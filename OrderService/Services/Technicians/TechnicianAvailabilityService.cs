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
        /// 🔍 Получение всех техников (из Redis, а при отсутствии - из PostgreSQL).
        /// </summary>
        private async Task<List<Technician>> GetAllTechniciansAsync()
        {
            var technicians = await _userRedisRepository.GetAllTechniciansAsync();

            if (technicians is { Count: > 0 })
            {
                _logger.LogInformation("✅ [Redis] Загружено {Count} техников.", technicians.Count);
                return technicians;
            }

            _logger.LogWarning("⚠️ [Redis] Пусто или недоступен. Загружаем из PostgreSQL...");

            var users = await _userPostgreRepository.GetAllUsersAsync();
            technicians = [.. users.OfType<Technician>()];

            if (technicians.Count > 0)
            {
                _logger.LogInformation("✅ [PostgreSQL] Загружено {Count} техников. Кэшируем в Redis...", technicians.Count);
                await _userRedisRepository.SaveTechniciansAsync(technicians);
            }
            else
            {
                _logger.LogWarning("⚠️ [PostgreSQL] Нет данных о техниках!");
            }

            return technicians;
        }

        /// <summary>
        /// 🔍 Проверка занятости техника на конкретную дату.
        /// </summary>
        private async Task<bool> IsTechnicianAvailableAsync(Guid technicianId, DateTime date)
        {
            // Попробуем загрузить техников из Redis
            var technician = await _userRedisRepository.GetTechnicianByIdAsync(technicianId);

            if (technician == null)
            {
                _logger.LogWarning("⚠️ [Redis] Техник {TechnicianId} не найден. Загружаем из PostgreSQL...", technicianId);
                var user = await _userPostgreRepository.GetUserByIdAsync(technicianId);
                technician = user as Technician;
            }

            if (technician == null)
            {
                _logger.LogWarning("❌ Техник с ID {TechnicianId} не найден!", technicianId);
                return false;
            }

            return !technician.Appointments.Any(a => a.Date.Date == date.Date);
        }


        /// <summary>
        /// 🔍 Получение доступных техников на конкретную дату.
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
