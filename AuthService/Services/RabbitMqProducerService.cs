using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using AuthService.Models.User;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuthService.Services
{
    public class RabbitMqProducerService : IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly ILogger<RabbitMqProducerService> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public RabbitMqProducerService(IServiceScopeFactory serviceScopeFactory, ILogger<RabbitMqProducerService> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;

            var factory = new ConnectionFactory()
            {
                HostName = "rabbitmq",
                Port = 5672,
                UserName = "guest",  
                Password = "guest",  
                DispatchConsumersAsync = true
            };

            int retryCount = 5;
            while (retryCount > 0)
            {
                try
                {
                    _connection = factory.CreateConnection();
                    _channel = _connection.CreateModel();

                    _channel.QueueDeclare(queue: "technicians_update",
                                          durable: true,
                                          exclusive: false,
                                          autoDelete: false,
                                          arguments: null);

                    _logger.LogInformation("✅ [RabbitMQ] Подключение успешно установлено");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ [RabbitMQ] Ошибка подключения. Retrying...");
                    retryCount--;
                    if (retryCount == 0)
                    {
                        throw;
                    }
                    Thread.Sleep(5000);
                }
            }

            if (_connection == null || _channel == null)
            {
                throw new InvalidOperationException("Failed to create RabbitMQ connection or channel.");
            }
        }

        public void PublishTechnicianUpdate(List<User> workers)
        {
            if (workers == null || workers.Count == 0) return;

            try
            {
                foreach (var user in workers)
                {
                    _logger.LogInformation("🔍 Пользователь: Id={Id}, FirstName='{FirstName}', LastName='{LastName}'",
                                           user.Id, user.FirstName, user.LastName);
                }

                var message = JsonSerializer.Serialize(workers.Select(user => new
                {
                    user.Id,
                    FullName = $"{user.FirstName} {user.LastName}",
                    user.PhoneNumber,
                    user.Address,
                    user.Latitude,
                    user.Longitude
                }).ToList());

                var body = Encoding.UTF8.GetBytes(message);

                _channel.BasicPublish(exchange: "", routingKey: "technicians_update",
                                      basicProperties: null, body: body);

                _logger.LogInformation("📤 [RabbitMQ] Отправлен обновленный список рабочих ({Count} человек)", workers.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ [RabbitMQ] Ошибка при отправке сообщения");
            }
        }


        public void Dispose()
        {
            try
            {
                if (_channel.IsOpen)
                {
                    _channel.Close();
                    _logger.LogInformation("⚠️ [RabbitMQ] Канал закрыт");
                }

                if (_connection.IsOpen)
                {
                    _connection.Close();
                    _logger.LogInformation("⚠️ [RabbitMQ] Соединение закрыто");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ [RabbitMQ] Ошибка при закрытии соединения");
            }
            finally
            {
                GC.SuppressFinalize(this);
            }
        }
    }
}