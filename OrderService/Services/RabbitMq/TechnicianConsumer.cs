using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using OrderService.Models.Users;
using OrderService.Repositories.Users;
using OrderService.DTO.Users;

namespace OrderService.Services.RabbitMq
{
    public class TechnicianConsumer(IServiceScopeFactory serviceScopeFactory, ILogger<TechnicianConsumer> logger, IConnection connection)
    {
        private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
        private readonly ILogger<TechnicianConsumer> _logger = logger;
        private IChannel? _channel;
        private AsyncEventingBasicConsumer? _consumer;

        public async Task InitializeAsync()
        {
            _channel = await connection.CreateChannelAsync();
            await _channel.BasicQosAsync(0, 1, false);

            _consumer = new AsyncEventingBasicConsumer(_channel);
            _consumer.ReceivedAsync += HandleBasicDeliverAsync;

            await _channel.BasicConsumeAsync("technicians_updated", autoAck: false, consumer: _consumer);
            _logger.LogInformation("📡 TechnicianConsumer subscribed to 'technicians_updated' queue.");
        }

        private async Task HandleBasicDeliverAsync(object sender, BasicDeliverEventArgs ea)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var redisRepo = scope.ServiceProvider.GetRequiredService<UserRedisRepository>();
            var postgreRepo = scope.ServiceProvider.GetRequiredService<UserPostgreRepository>();

            try
            {
                var message = Encoding.UTF8.GetString(ea.Body.Span);
                var technician = JsonSerializer.Deserialize<Technician>(message);

                if (technician != null)
                {
                    var existingTechnician = await postgreRepo.GetUserByIdAsync(technician.Id);
                    if (existingTechnician is Technician existing)
                    {
                        existing.FullName = technician.FullName;
                        existing.Address = technician.Address;
                        existing.PhoneNumber = technician.PhoneNumber;
                        existing.Latitude = technician.Latitude;
                        existing.Longitude = technician.Longitude;
                        existing.IsAvailable = technician.IsAvailable;
                        existing.CurrentOrderId = technician.CurrentOrderId;
                        await postgreRepo.UpdateUserAsync(existing);
                    }
                    else
                    {
                        await postgreRepo.SaveUserAsync(technician);
                    }

                    await redisRepo.SaveTechnicianAsync(technician);
                    _logger.LogInformation("✅ Technician {Id} updated in DB and Redis.", technician.Id);
                }

                if (_channel != null)
                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error processing Technician update");
                if (_channel != null)
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, true);
            }
        }

        /// <summary>
        /// Вызывается `RabbitMqConsumerService` для обработки данных.
        /// </summary>
        public async Task ProcessAsync(TechnicianDTO technicianData)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var postgreRepo = scope.ServiceProvider.GetRequiredService<UserPostgreRepository>();

            var technician = new Technician
            {
                Id = technicianData.Id,
                FullName = technicianData.FullName,
                Address = technicianData.Address,
                PhoneNumber = technicianData.PhoneNumber,
                Latitude = technicianData.Latitude,
                Longitude = technicianData.Longitude,
                IsAvailable = technicianData.IsAvailable,
                CurrentOrderId = technicianData.CurrentOrderId
            };

            await postgreRepo.SaveUserAsync(technician);
            _logger.LogInformation("✅ Technician {Id} processed and saved.", technician.Id);
        }
    }
}
