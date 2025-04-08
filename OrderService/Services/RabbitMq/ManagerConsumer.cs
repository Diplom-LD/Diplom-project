using OrderService.Models.Users;
using OrderService.Repositories.Users;
using OrderService.DTO.Users;

namespace OrderService.Services.RabbitMq
{
    public class ManagerConsumer(IServiceScopeFactory serviceScopeFactory, ILogger<ManagerConsumer> logger)
    {
        private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
        private readonly ILogger<ManagerConsumer> _logger = logger;

        /// <summary>
        /// Обрабатывает данные менеджера, переданные из RabbitMqConsumerService.
        /// </summary>
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
                Longitude = userData.Longitude,
                Email = userData.Email
            };

            var existingManager = await postgreRepo.GetUserByIdAsync(userData.Id);

            if (existingManager is Manager existing)
            {
                existing.FullName = manager.FullName;
                existing.Address = manager.Address;
                existing.Latitude = manager.Latitude;
                existing.Longitude = manager.Longitude;
                existing.PhoneNumber = manager.PhoneNumber;
                existing.Email = manager.Email;

                await postgreRepo.UpdateUserAsync(existing);
                _logger.LogInformation("✅ Manager {Id} updated in DB, Email: {Email}", existing.Id, existing.Email);
            }
            else
            {
                await postgreRepo.SaveUserAsync(manager);
                _logger.LogInformation("✅ New Manager {Id} saved in DB, Email: {Email}", manager.Id, manager.Email);
            }
        }
    }
}
