using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using OrderService.Models.Users;
using OrderService.Repositories.Users;
using OrderService.DTO.Users;

namespace OrderService.Services.RabbitMq
{
    public class ClientConsumer(IServiceScopeFactory serviceScopeFactory, ILogger<ClientConsumer> logger, IConnection connection)
    {
        private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
        private readonly ILogger<ClientConsumer> _logger = logger;
        private IChannel? _channel;
        private AsyncEventingBasicConsumer? _consumer;

        /// <summary>
        /// Инициализация подписки на очередь RabbitMQ.
        /// </summary>
        public async Task InitializeAsync()
        {
            _channel = await connection.CreateChannelAsync();
            await _channel.BasicQosAsync(0, 1, false);

            _consumer = new AsyncEventingBasicConsumer(_channel);
            _consumer.ReceivedAsync += HandleBasicDeliverAsync; 

            await _channel.BasicConsumeAsync(queue: "clients_updated",
                                             autoAck: false,
                                             consumer: _consumer);

            _logger.LogInformation("📡 ClientConsumer subscribed to 'clients_updated' queue.");
        }

        /// <summary>
        /// Обработчик полученных сообщений.
        /// </summary>
        private async Task HandleBasicDeliverAsync(object sender, BasicDeliverEventArgs ea)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var userRepository = scope.ServiceProvider.GetRequiredService<UserPostgreRepository>();

            try
            {
                var message = Encoding.UTF8.GetString(ea.Body.Span);
                var client = JsonSerializer.Deserialize<Client>(message);

                if (client != null)
                {
                    var existingClient = await userRepository.GetUserByIdAsync(client.Id);

                    if (existingClient is Client existing)
                    {
                        existing.FullName = client.FullName;
                        existing.Address = client.Address;
                        existing.Latitude = client.Latitude;
                        existing.Longitude = client.Longitude;
                        existing.PhoneNumber = client.PhoneNumber;
                        await userRepository.UpdateUserAsync(existing);
                    }
                    else
                    {
                        await userRepository.SaveUserAsync(client);
                    }

                    _logger.LogInformation("✅ Client {Id} updated in DB.", client.Id);
                }

                if (_channel != null)
                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error processing client update");
                if (_channel != null)
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, true);
            }
        }

        public async Task ProcessAsync(UserDTO userData)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var postgreRepo = scope.ServiceProvider.GetRequiredService<UserPostgreRepository>();

            var client = new Client
            {
                Id = userData.Id,
                FullName = userData.FullName,
                Address = userData.Address,
                PhoneNumber = userData.PhoneNumber,
                Latitude = userData.Latitude,
                Longitude = userData.Longitude
            };

            await postgreRepo.SaveUserAsync(client);
            _logger.LogInformation("✅ Client {Id} processed and saved.", client.Id);
        }

    }
}
