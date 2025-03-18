using System.Text;
using System.Text.Json;
using OrderService.DTO.Users;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OrderService.Services.RabbitMq
{
    public class RabbitMqConsumerService(IServiceScopeFactory serviceScopeFactory, IConfiguration configuration, ILogger<RabbitMqConsumerService> logger) : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
        private readonly IConfiguration _configuration = configuration;
        private readonly ILogger<RabbitMqConsumerService> _logger = logger;
        private IConnection? _connection;
        private IChannel? _channel;
        private AsyncEventingBasicConsumer? _consumer;

        /// <summary>
        /// Инициализация соединения с RabbitMQ.
        /// </summary>
        private async Task InitRabbitMQ()
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _configuration["RabbitMQ:Host"] ?? throw new ArgumentNullException("RabbitMQ:Host"),
                    Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
                    UserName = _configuration["RabbitMQ:User"] ?? throw new ArgumentNullException("RabbitMQ:User"),
                    Password = _configuration["RabbitMQ:Password"] ?? throw new ArgumentNullException("RabbitMQ:Password"),
                    AutomaticRecoveryEnabled = true,
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
                };

                _logger.LogInformation("⏳ Connecting to RabbitMQ...");

                _connection = await factory.CreateConnectionAsync();
                _channel = await _connection.CreateChannelAsync();

                await _channel.QueueDeclareAsync(
                    queue: "users_updated",
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                _consumer = new AsyncEventingBasicConsumer(_channel);
                _consumer.ReceivedAsync += HandleMessageAsync;

                await _channel.BasicConsumeAsync(queue: "users_updated", autoAck: false, consumer: _consumer);
                _logger.LogInformation("✅ RabbitMQ connection established.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to connect to RabbitMQ.");
            }
        }

        /// <summary>
        /// Основной метод выполнения фонового сервиса.
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await InitRabbitMQ();

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken); 
            }
        }

        /// <summary>
        /// Обрабатывает полученное сообщение из RabbitMQ.
        /// </summary>
        private async Task HandleMessageAsync(object sender, BasicDeliverEventArgs ea)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            _logger.LogInformation("📩 Received message: {Message}", message);

            try
            {
                var userData = JsonSerializer.Deserialize<UserDTO>(message);
                if (userData == null)
                {
                    _logger.LogWarning("⚠️ Received invalid user data.");
                    return;
                }

                await ProcessUserUpdateAsync(scope, userData);

                if (_channel != null)
                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error processing user update");
                if (_channel != null)
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, true);
            }
        }

        /// <summary>
        /// Обрабатывает обновление пользователя.
        /// </summary>
        private async Task ProcessUserUpdateAsync(IServiceScope scope, UserDTO userData)
        {
            switch (userData.Role.ToLower())
            {
                case "worker":
                case "technician":
                    var technicianConsumer = scope.ServiceProvider.GetRequiredService<TechnicianConsumer>();
                    var technicianData = new TechnicianDTO
                    {
                        Id = userData.Id,
                        FullName = userData.FullName,
                        Address = userData.Address,
                        Latitude = userData.Latitude,
                        Longitude = userData.Longitude,
                        PhoneNumber = userData.PhoneNumber,
                        Email = userData.Email,
                        IsAvailable = true,
                        CurrentOrderId = null
                    };
                    await technicianConsumer.ProcessAsync(technicianData);
                    break;

                case "manager":
                    var managerConsumer = scope.ServiceProvider.GetRequiredService<ManagerConsumer>();
                    await managerConsumer.ProcessAsync(userData);
                    break;

                case "client":
                    var clientConsumer = scope.ServiceProvider.GetRequiredService<ClientConsumer>();
                    await clientConsumer.ProcessAsync(userData);
                    break;

                default:
                    _logger.LogWarning("⚠️ Unknown user role: {Role}", userData.Role);
                    break;
            }
        }

        /// <summary>
        /// Останавливает сервис и закрывает соединение с RabbitMQ.
        /// </summary>
        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            await DisposeAsync();
            await base.StopAsync(stoppingToken);
        }

        /// <summary>
        /// Освобождает ресурсы RabbitMQ.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            try
            {
                if (_channel != null)
                {
                    await _channel.DisposeAsync();
                    _logger.LogInformation("⚠️ [RabbitMQ] Channel closed.");
                }

                if (_connection != null)
                {
                    await _connection.DisposeAsync();
                    _logger.LogInformation("⚠️ [RabbitMQ] Connection closed.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [RabbitMQ] Error closing connection.");
            }
        }
    }
}
