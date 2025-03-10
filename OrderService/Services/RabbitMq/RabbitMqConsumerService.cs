using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using OrderService.Models.Technicians;
using OrderService.Repositories.Technicians;

namespace OrderService.Services.RabbitMq
{
    public class RabbitMqConsumerService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RabbitMqConsumerService> _logger;
        private IConnection? _connection;
        private IModel? _channel;

        public RabbitMqConsumerService(IServiceScopeFactory serviceScopeFactory, IConfiguration configuration, ILogger<RabbitMqConsumerService> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _configuration = configuration;
            _logger = logger;
            InitRabbitMQ();
        }

        private void InitRabbitMQ()
        {
            var host = _configuration["RabbitMQ:Host"];
            var portString = _configuration["RabbitMQ:Port"];
            var user = _configuration["RabbitMQ:User"];
            var password = _configuration["RabbitMQ:Password"];

            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(portString) || string.IsNullOrEmpty(user) || string.IsNullOrEmpty(password))
            {
                throw new ArgumentNullException("RabbitMQ settings are not properly configured.");
            }

            var port = int.Parse(portString);

            var factory = new ConnectionFactory()
            {
                HostName = host,
                Port = port,
                UserName = user,
                Password = password,
                DispatchConsumersAsync = true
            };

            int retryCount = 5;

            _logger.LogInformation("⏳ Waiting 5 seconds before attempting RabbitMQ connection...");
            Task.Delay(5000).Wait();

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

                    _logger.LogInformation("✅ RabbitMQ connection established successfully.");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ RabbitMQ connection error. Retrying...");
                    retryCount--;
                    if (retryCount == 0)
                    {
                        throw;
                    }
                    _logger.LogInformation("🔄 Retrying in 5 seconds... Attempts left: {RetryCount}", retryCount);
                    Thread.Sleep(5000);
                }
            }

            if (_connection == null || _channel == null)
            {
                throw new InvalidOperationException("❌ Failed to create RabbitMQ connection or channel.");
            }
        }


        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_channel == null)
            {
                _logger.LogError("RabbitMQ channel is not initialized, subscription is not possible!");
                return Task.CompletedTask;
            }

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var redisRepo = scope.ServiceProvider.GetRequiredService<TechnicianRedisRepository>();

                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var workers = JsonSerializer.Deserialize<List<Technician>>(message);

                    if (workers != null)
                    {
                        await redisRepo.SaveAsync(workers);
                        _logger.LogInformation("Received {Count} workers and saved to Redis.", workers.Count);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing RabbitMQ message");
                }
            };

            _channel.BasicConsume(queue: "technicians_update",
                                  autoAck: true,
                                  consumer: consumer);

            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            try
            {
                if (_channel?.IsOpen == true)
                {
                    _channel.Close();
                    _logger.LogInformation("RabbitMQ channel closed.");
                }

                if (_connection?.IsOpen == true)
                {
                    _connection.Close();
                    _logger.LogInformation("RabbitMQ connection closed.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing RabbitMQ connection");
            }
            finally
            {
                base.Dispose();
                GC.SuppressFinalize(this);
            }
        }
    }
}