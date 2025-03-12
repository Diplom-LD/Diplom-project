using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using OrderService.Models.Users;
using OrderService.Repositories.Users;
using OrderService.DTO.Users;

namespace OrderService.Services.RabbitMq
{
    public class ManagerConsumer(IServiceScopeFactory serviceScopeFactory, ILogger<ManagerConsumer> logger, IConnection connection)
    {
        private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
        private readonly ILogger<ManagerConsumer> _logger = logger;
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

            await _channel.BasicConsumeAsync(queue: "managers_updated",
                                             autoAck: false,
                                             consumer: _consumer);

            _logger.LogInformation("📡 ManagerConsumer subscribed to 'managers_updated' queue.");
        }

        /// <summary>
        /// Обработчик полученных сообщений из очереди.
        /// </summary>
        private async Task HandleBasicDeliverAsync(object sender, BasicDeliverEventArgs ea)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var userRepository = scope.ServiceProvider.GetRequiredService<UserPostgreRepository>();

            try
            {
                var message = Encoding.UTF8.GetString(ea.Body.Span);
                var manager = JsonSerializer.Deserialize<Manager>(message);

                if (manager != null)
                {
                    var existingUser = await userRepository.GetUserByIdAsync(manager.Id);

                    if (existingUser is Manager existingManager)
                    {
                        existingManager.FullName = manager.FullName;
                        existingManager.Address = manager.Address;
                        existingManager.Latitude = manager.Latitude;
                        existingManager.Longitude = manager.Longitude;
                        existingManager.PhoneNumber = manager.PhoneNumber;
                        await userRepository.UpdateUserAsync(existingManager);
                    }
                    else
                    {
                        await userRepository.SaveUserAsync(manager);
                    }

                    _logger.LogInformation("✅ Manager {Id} updated in DB.", manager.Id);
                }

                if (_channel != null)
                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error processing manager update");
                if (_channel != null)
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, true);
            }
        }

        public async Task ProcessAsync(UserDTO userData)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var postgreRepo = scope.ServiceProvider.GetRequiredService<UserPostgreRepository>();

            var manager = new Manager
            {
                Id = userData.Id,
                FullName = userData.FullName,
                Address = userData.Address,
                PhoneNumber = userData.PhoneNumber,
                Latitude = userData.Latitude,
                Longitude = userData.Longitude
            };

            await postgreRepo.SaveUserAsync(manager);
            _logger.LogInformation("✅ Manager {Id} processed and saved.", manager.Id);
        }



    }
}
