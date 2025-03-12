using OrderService.Models.Users;
using OrderService.Repositories.Users;

namespace OrderService.Services.Technicians
{
    public class TechnicianAvailabilityService(
        UserPostgreRepository userPostgreRepository,
        UserRedisRepository userRedisRepository,
        ILogger<TechnicianAvailabilityService> logger)
    {
        private readonly UserPostgreRepository _userPostgreRepository = userPostgreRepository;
        private readonly UserRedisRepository _userRedisRepository = userRedisRepository;
        private readonly ILogger<TechnicianAvailabilityService> _logger = logger;


        /// <summary>
        /// Получение всех техников (из Redis или PostgreSQL)
        /// </summary>
        private async Task<List<Technician>> GetAllTechniciansAsync()
        {
            var technicians = await _userRedisRepository.GetAllTechniciansAsync();

            if (technicians == null || technicians.Count == 0)
            {
                _logger.LogWarning("⚠️ Redis пуст или недоступен. Загружаем техников из PostgreSQL...");

                // Загружаем всех пользователей, но выбираем только техников
                var users = await _userPostgreRepository.GetAllUsersAsync();
                technicians = [.. users.OfType<Technician>()];

                if (technicians.Count > 0)
                {
                    _logger.LogInformation("✅ Загружено {Count} техников из PostgreSQL. Кэшируем в Redis...", technicians.Count);
                    await _userRedisRepository.SaveTechniciansAsync(technicians); 
                }
                else
                {
                    _logger.LogWarning("⚠️ В PostgreSQL нет данных о техниках!");
                }
            }

            return technicians;
        }

        /// <summary>
        /// Проверка занятости техника на конкретную дату
        /// </summary>
        private async Task<bool> IsTechnicianAvailableAsync(string technicianId, DateTime date)
        {
            var user = await _userPostgreRepository.GetUserByIdAsync(Guid.Parse(technicianId));

            if (user is not Technician technician)
            {
                _logger.LogWarning("❌ Техник с ID: {TechnicianId} не найден", technicianId);
                return false;
            }

            return !technician.Appointments.Any(a => a.Date.Date == date.Date);
        }


        /// <summary>
        /// Получение доступных техников на конкретную дату
        /// </summary>
        public async Task<List<Technician>> GetAvailableTechniciansAsync(DateTime date)
        {
            var allTechnicians = await GetAllTechniciansAsync();
            var availableTechnicians = allTechnicians
                .Where(t => t.IsAvailable && !t.Appointments.Any(a => a.Date.Date == date.Date))
                .ToList();

            _logger.LogInformation("✅ Найдено {Count} доступных техников на {Date}", availableTechnicians.Count, date.ToShortDateString());
            return availableTechnicians;
        }
    }
}
