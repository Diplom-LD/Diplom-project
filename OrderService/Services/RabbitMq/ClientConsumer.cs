using OrderService.Models.Users;
using OrderService.Repositories.Users;
using OrderService.DTO.Users;

namespace OrderService.Services.RabbitMq
{
    public class ClientConsumer(IServiceScopeFactory serviceScopeFactory, ILogger<ClientConsumer> logger)
    {
        private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
        private readonly ILogger<ClientConsumer> _logger = logger;

        /// <summary>
        /// Обрабатывает данные клиента, переданные из RabbitMqConsumerService.
        /// </summary>
        public async Task ProcessAsync(UserDTO userData)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var postgreRepo = scope.ServiceProvider.GetRequiredService<UserPostgreRepository>();

            var client = new Client
            {
                Id = userData.Id,
                FullName = userData.FullName,
                Address = userData.Address,
                Latitude = userData.Latitude,
                Longitude = userData.Longitude,
                PhoneNumber = userData.PhoneNumber,
                Email = userData.Email
            };

            var existingClient = await postgreRepo.GetUserByIdAsync(userData.Id);

            if (existingClient is Client existing)
            {
                existing.FullName = client.FullName;
                existing.Address = client.Address;
                existing.Latitude = client.Latitude;
                existing.Longitude = client.Longitude;
                existing.PhoneNumber = client.PhoneNumber;
                existing.Email = client.Email;

                await postgreRepo.UpdateUserAsync(existing);
                _logger.LogInformation("✅ Client {Id} updated in DB, Email: {Email}", existing.Id, existing.Email);
            }
            else
            {
                await postgreRepo.SaveUserAsync(client);
                _logger.LogInformation("✅ New Client {Id} saved in DB, Email: {Email}", client.Id, client.Email);
            }
        }
    }
}
