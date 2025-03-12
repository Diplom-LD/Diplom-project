using StackExchange.Redis;
using System.Text.Json;
using OrderService.Models.Users;

namespace OrderService.Repositories.Users
{
    public class UserRedisRepository(IDatabase cache, ILogger<UserRedisRepository> logger)
    {
        private readonly IDatabase _cache = cache;
        private readonly ILogger<UserRedisRepository> _logger = logger;

        public UserRedisRepository(IConnectionMultiplexer redis, ILogger<UserRedisRepository> logger)
            : this(redis.GetDatabase(), logger)
        {
        }

        public async Task<List<Technician>> GetAllTechniciansAsync()
        {
            try
            {
                var server = _cache.Multiplexer.GetServer(_cache.Multiplexer.GetEndPoints()[0]);
                var keys = server.Keys(pattern: "technician:*").ToList();

                var technicians = new List<Technician>();

                foreach (var key in keys)
                {
                    var json = await _cache.StringGetAsync(key);
                    if (json.HasValue)
                    {
                        var technician = JsonSerializer.Deserialize<Technician>(json.ToString());
                        if (technician != null)
                        {
                            technicians.Add(technician);
                        }
                    }
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

        public async Task SaveTechnicianAsync(Technician technician)
        {
            try
            {
                var json = JsonSerializer.Serialize(technician);
                await _cache.StringSetAsync($"technician:{technician.Id}", json);
                _logger.LogInformation("✅ [Redis] Техник сохранён с ID: {TechnicianId}", technician.Id);
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

        public async Task SaveTechniciansAsync(List<Technician> technicians)
        {
            if (technicians == null || technicians.Count == 0)
            {
                _logger.LogWarning("⚠️ [Redis] Пустой список техников, сохранение пропущено.");
                return;
            }

            try
            {
                var json = JsonSerializer.Serialize(technicians);
                await _cache.StringSetAsync("technicians", json);
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


        public async Task DeleteTechnicianAsync(Guid technicianId)
        {
            try
            {
                var key = $"technician:{technicianId}";
                var result = await _cache.KeyDeleteAsync(key);

                if (result)
                {
                    _logger.LogInformation("🗑️ [Redis] Техник удалён с ID: {TechnicianId}", technicianId);
                }
                else
                {
                    _logger.LogWarning("⚠️ [Redis] Техник с ID: {TechnicianId} не найден.", technicianId);
                }
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogError(ex, "❌ Ошибка соединения с Redis при удалении данных.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Неизвестная ошибка при удалении данных из Redis.");
            }
        }
    }
}
