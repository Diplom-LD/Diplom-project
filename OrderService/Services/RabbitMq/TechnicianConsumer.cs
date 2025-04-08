using OrderService.Repositories.Users;
using OrderService.DTO.Users;
using OrderService.Models.Users;

namespace OrderService.Services.RabbitMq
{
    public class TechnicianConsumer(IServiceScopeFactory serviceScopeFactory, ILogger<TechnicianConsumer> logger)
    {
        private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
        private readonly ILogger<TechnicianConsumer> _logger = logger;

        /// <summary>
        /// Обрабатывает данные техника, переданные из RabbitMqConsumerService.
        /// </summary>
        public async Task ProcessAsync(TechnicianDTO technicianData)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var postgreRepo = scope.ServiceProvider.GetRequiredService<UserPostgreRepository>();
            var redisRepo = scope.ServiceProvider.GetRequiredService<UserRedisRepository>();

            var technician = new Technician
            {
                Id = technicianData.Id,
                FullName = technicianData.FullName,
                Address = technicianData.Address,
                PhoneNumber = technicianData.PhoneNumber,
                Email = technicianData.Email,
                Latitude = technicianData.Latitude,
                Longitude = technicianData.Longitude,
                IsAvailable = technicianData.IsAvailable,
                CurrentOrderId = technicianData.CurrentOrderId
            };

            var existingTechnician = await postgreRepo.GetUserByIdAsync(technicianData.Id);

            if (existingTechnician is Technician existing)
            {
                existing.FullName = technician.FullName;
                existing.Address = technician.Address;
                existing.Latitude = technician.Latitude;
                existing.Longitude = technician.Longitude;
                existing.PhoneNumber = technician.PhoneNumber;
                existing.Email = technician.Email;
                existing.IsAvailable = technician.IsAvailable;
                existing.CurrentOrderId = technician.CurrentOrderId;

                await postgreRepo.UpdateUserAsync(existing);
                await redisRepo.UpdateTechnicianAsync(existing);
                _logger.LogInformation("✅ Technician {Id} updated in DB and Redis, Email: {Email}", existing.Id, existing.Email);
            }
            else
            {
                await postgreRepo.SaveUserAsync(technician);
                await redisRepo.SaveTechnicianAsync(technician);
                _logger.LogInformation("✅ New Technician {Id} saved in DB and Redis, Email: {Email}", technician.Id, technician.Email);
            }
        }
    }
}
