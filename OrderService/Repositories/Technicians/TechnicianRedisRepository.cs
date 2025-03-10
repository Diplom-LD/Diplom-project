using StackExchange.Redis;
using System.Text.Json;
using OrderService.Models.Technicians;

namespace OrderService.Repositories.Technicians
{
    public class TechnicianRedisRepository(IDatabase cache, ILogger<TechnicianRedisRepository> logger)
    {
        private readonly IDatabase _cache = cache;
        private readonly ILogger<TechnicianRedisRepository> _logger = logger;
        private readonly string _redisKey = "technicians";

        public TechnicianRedisRepository(IConnectionMultiplexer redis, ILogger<TechnicianRedisRepository> logger)
            : this(redis.GetDatabase(), logger)
        {
        }

        public async Task<List<Technician>> GetAllAsync()
        {
            try
            {
                var json = await _cache.StringGetAsync(_redisKey);
                if (!json.HasValue)
                {
                    _logger.LogWarning("⚠️ В Redis нет сохранённых техников.");
                    return [];
                }

                var technicians = JsonSerializer.Deserialize<List<Technician>>(json.ToString());
                if (technicians == null)
                {
                    _logger.LogError("❌ Ошибка десериализации данных из Redis.");
                    return [];
                }

                _logger.LogInformation("📥 [Redis] Загружено {Count} техников.", technicians.Count);
                return technicians;
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogError(ex, "❌ Ошибка соединения с Redis.");
                return [];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Неизвестная ошибка при получении данных из Redis.");
                return [];
            }
        }


        public async Task SaveAsync(List<Technician> technicians)
        {
            try
            {
                var json = JsonSerializer.Serialize(technicians);
                await _cache.StringSetAsync(_redisKey, json);
                _logger.LogInformation("✅ [Redis] Сохранено {Count} техников.", technicians.Count);
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogError(ex, "❌ Ошибка соединения с Redis при сохранении данных.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Неизвестная ошибка при сохранении данных в Redis.");
            }
        }
    }
}
