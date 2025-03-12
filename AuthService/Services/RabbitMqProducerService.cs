using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using AuthService.Models.User;

namespace AuthService.Services
{
    public class RabbitMqProducerService : IAsyncDisposable
    {
        private readonly IConnection _connection;
        private readonly IChannel _channel;
        private readonly ILogger<RabbitMqProducerService> _logger;

        public RabbitMqProducerService(ILogger<RabbitMqProducerService> logger)
        {
            _logger = logger;

            var factory = new ConnectionFactory
            {
                HostName = "rabbitmq",
                Port = 5672,
                UserName = "guest",
                Password = "guest"
            };

            _logger.LogInformation("⏳ Подключение к RabbitMQ...");

            try
            {
                _connection = Task.Run(() => factory.CreateConnectionAsync()).GetAwaiter().GetResult();
                _channel = Task.Run(() => _connection.CreateChannelAsync()).GetAwaiter().GetResult();

                _channel.QueueDeclareAsync(queue: "users_registered",
                                           durable: true,
                                           exclusive: false,
                                           autoDelete: false,
                                           arguments: null).GetAwaiter().GetResult();

                _channel.QueueDeclareAsync(queue: "users_updated",
                                           durable: true,
                                           exclusive: false,
                                           autoDelete: false,
                                           arguments: null).GetAwaiter().GetResult();

                _logger.LogInformation("✅ [RabbitMQ] Подключение успешно установлено.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [RabbitMQ] Ошибка подключения.");
                throw;
            }
        }

        public async Task PublishUserRegisteredAsync(User user, CancellationToken cancellationToken = default)
        {
            if (user == null)
            {
                _logger.LogWarning("⚠️ [RabbitMQ] Попытка отправки null-пользователя.");
                return;
            }

            try
            {
                var message = JsonSerializer.Serialize(new
                {
                    user.Id,
                    user.Role,
                    FullName = $"{user.FirstName} {user.LastName}".Trim(),
                    user.Address,
                    user.Latitude,
                    user.Longitude
                });

                await PublishMessageAsync("users_registered", message, cancellationToken);
                _logger.LogInformation("📤 [RabbitMQ] Отправлен новый пользователь: {Id}", user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [RabbitMQ] Ошибка при отправке нового пользователя.");
            }
        }

        public async Task PublishUserUpdatedAsync(User user, CancellationToken cancellationToken = default)
        {
            if (user == null)
            {
                _logger.LogWarning("⚠️ [RabbitMQ] Попытка отправки null-пользователя на обновление.");
                return;
            }

            try
            {
                var message = JsonSerializer.Serialize(new
                {
                    user.Id,
                    user.Role,
                    FullName = $"{user.FirstName} {user.LastName}".Trim(),
                    user.Address,
                    user.Latitude,
                    user.Longitude
                });

                await PublishMessageAsync("users_updated", message, cancellationToken);
                _logger.LogInformation("📤 [RabbitMQ] Обновлены данные пользователя: {Id}", user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [RabbitMQ] Ошибка при отправке обновления пользователя.");
            }
        }

        private async Task PublishMessageAsync(string queue, string message, CancellationToken cancellationToken)
        {
            var body = Encoding.UTF8.GetBytes(message);

            var properties = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json"
            };

            await _channel.BasicPublishAsync(exchange: "",
                                             routingKey: queue,
                                             mandatory: false,
                                             basicProperties: properties,
                                             body: body,
                                             cancellationToken: cancellationToken);
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                if (_channel.IsOpen)
                {
                    await _channel.CloseAsync(200, "Closing channel", false, CancellationToken.None);
                    _logger.LogInformation("⚠️ [RabbitMQ] Канал закрыт.");
                }

                if (_connection.IsOpen)
                {
                    await _connection.CloseAsync(200, "Closing connection", TimeSpan.FromSeconds(5), false, CancellationToken.None);
                    _logger.LogInformation("⚠️ [RabbitMQ] Соединение закрыто.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [RabbitMQ] Ошибка при закрытии соединения.");
            }
            finally
            {
                GC.SuppressFinalize(this);
            }
        }
    }
}