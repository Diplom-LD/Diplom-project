using StackExchange.Redis;
using System.Text.Json;
using OrderService.Models.Technicians;

namespace OrderService.Repositories.Technicians
{
    public class TechnicianRedisRepository(IDatabase cache, ILogger<TechnicianRedisRepository> logger)
    {
        private readonly IDatabase _cache = cache;
        private readonly ILogger<TechnicianRedisRepository> _logger = logger;

        public TechnicianRedisRepository(IConnectionMultiplexer redis, ILogger<TechnicianRedisRepository> logger)
            : this(redis.GetDatabase(), logger)
        {
        }

        public async Task<List<Technician>> GetAllAsync()
        {
            var json = await _cache.StringGetAsync("technicians");
            if (!json.HasValue)
            {
                _logger.LogWarning("⚠️ В Redis нет сохранённых техников.");
                return [];
            }

            var technicians = JsonSerializer.Deserialize<List<Technician>>(json!) ?? [];
            _logger.LogInformation("📥 [Redis] Загружено {Count} техников.", technicians.Count);
            return technicians;
        }

        public async Task SaveAsync(List<Technician> technicians)
        {
            var json = JsonSerializer.Serialize(technicians);
            await _cache.StringSetAsync("technicians", json, TimeSpan.FromHours(2));
            _logger.LogInformation("✅ [Redis] Сохранено {Count} техников.", technicians.Count);
        }
    }
}
