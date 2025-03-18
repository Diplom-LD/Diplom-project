using StackExchange.Redis;
using OrderService.Models.Users;
using System.Globalization;
using System.Text.Json;
using OrderService.DTO.Orders.TechnicianLocation;

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

        /// <summary>
        /// Сохранить список техников в Redis.
        /// </summary>
        public async Task SaveTechniciansAsync(List<Technician> technicians)
        {
            if (technicians == null || technicians.Count == 0)
            {
                _logger.LogWarning("⚠️ [Redis] Пустой список техников, сохранение пропущено.");
                return;
            }

            try
            {
                foreach (var technician in technicians)
                {
                    await _cache.ListRightPushAsync("technicians", technician.Id.ToString());
                    await _cache.HashSetAsync($"technician:{technician.Id}", ConvertToHash(technician));
                }

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

        /// <summary>
        /// 📌 Получить всех техников из Redis.
        /// </summary>
        public async Task<List<Technician>> GetAllTechniciansAsync()
        {
            try
            {
                var technicianIds = await _cache.ListRangeAsync("technicians");
                var technicians = new List<Technician>();

                foreach (var id in technicianIds)
                {
                    var data = await _cache.HashGetAllAsync($"technician:{id}");
                    if (data.Length > 0)
                    {
                        var technician = ConvertFromHash(data, id);
                        technicians.Add(technician);
                    }
                }

                _logger.LogInformation("📥 [Redis] Загружено {Count} техников.", technicians.Count);
                return technicians;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ошибка при получении данных из Redis.");
                return [];
            }
        }

        /// <summary>
        /// 📌 Сохранить техника в Redis.
        /// </summary>
        public async Task SaveTechnicianAsync(Technician technician)
        {
            try
            {
                await _cache.ListRemoveAsync("technicians", technician.Id.ToString());
                await _cache.ListRightPushAsync("technicians", technician.Id.ToString());
                await _cache.HashSetAsync($"technician:{technician.Id}", ConvertToHash(technician));

                _logger.LogInformation("✅ [Redis] Техник сохранён с ID: {TechnicianId}", technician.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ошибка при сохранении техника в Redis.");
            }
        }

        /// <summary>
        /// 📌 Обновить данные техника в Redis.
        /// </summary>
        public async Task UpdateTechnicianAsync(Technician technician)
        {
            try
            {
                var key = $"technician:{technician.Id}";

                // Проверяем, есть ли уже техник в списке
                var exists = await _cache.HashExistsAsync(key, "FullName");
                if (!exists)
                {
                    await _cache.ListRemoveAsync("technicians", technician.Id.ToString());
                    await _cache.ListRightPushAsync("technicians", technician.Id.ToString());
                }

                await _cache.HashSetAsync(key, ConvertToHash(technician));

                _logger.LogInformation("✅ [Redis] Техник обновлён с ID: {TechnicianId}", technician.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ошибка при обновлении техника в Redis.");
            }
        }

        /// <summary>
        /// 📌 Удалить техника из Redis.
        /// </summary>
        public async Task DeleteTechnicianAsync(Guid technicianId)
        {
            try
            {
                var key = $"technician:{technicianId}";
                var removed = await _cache.KeyDeleteAsync(key);

                if (removed)
                {
                    await _cache.ListRemoveAsync("technicians", technicianId.ToString());
                    _logger.LogInformation("🗑️ [Redis] Техник удалён с ID: {TechnicianId}", technicianId);
                }
                else
                {
                    _logger.LogWarning("⚠️ [Redis] Техник с ID {TechnicianId} не найден.", technicianId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ошибка при удалении техника из Redis.");
            }
        }

        /// <summary>
        /// 📌 Устанавливает местоположение техника в Redis.
        /// </summary>
        public async Task<bool> SetTechnicianLocationAsync(Guid technicianId, double latitude, double longitude, Guid orderId)
        {
            try
            {
                var key = $"technician:{technicianId}";

                if (!await _cache.KeyExistsAsync(key))
                {
                    _logger.LogWarning("⚠️ [Redis] Попытка обновить локацию несуществующего техника: {TechnicianId}", technicianId);
                    return false;
                }

                _logger.LogInformation("📡 Обновление локации техника {TechnicianId}: {Latitude}, {Longitude}, OrderId: {OrderId}",
                    technicianId, latitude, longitude, orderId);

                await _cache.HashSetAsync(key,
                [
                    new HashEntry("Latitude", latitude.ToString(CultureInfo.InvariantCulture)),
                    new HashEntry("Longitude", longitude.ToString(CultureInfo.InvariantCulture)),
                    new HashEntry("OrderId", orderId.ToString()) 
                ]);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ошибка при обновлении локации техника в Redis.");
                return false;
            }
        }



        /// <summary>
        /// 🔍 Получает местоположение техника из Redis.
        /// </summary>
        public async Task<TechnicianLocationDTO?> GetTechnicianLocationAsync(Guid technicianId)
        {
            try
            {
                var key = $"technician:{technicianId}";

                var hashEntries = await _cache.HashGetAllAsync(key);
                if (hashEntries.Length == 0)
                {
                    _logger.LogWarning("⚠️ Локация для техника {TechnicianId} не найдена в Redis", technicianId);
                    return null;
                }

                var latitude = double.TryParse(hashEntries.FirstOrDefault(x => x.Name == "Latitude").Value, out var lat) ? lat : 0;
                var longitude = double.TryParse(hashEntries.FirstOrDefault(x => x.Name == "Longitude").Value, out var lon) ? lon : 0;

                var orderIdStr = hashEntries.FirstOrDefault(x => x.Name == "OrderId").Value;
                var orderId = !string.IsNullOrEmpty(orderIdStr) && Guid.TryParse(orderIdStr, out var parsedOrderId) ? parsedOrderId : Guid.Empty;

                _logger.LogInformation("📡 Получена локация техника {TechnicianId}: {Latitude}, {Longitude}, OrderId: {OrderId}",
                    technicianId, latitude, longitude, orderId);

                return new TechnicianLocationDTO(technicianId, latitude, longitude, orderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ошибка при получении локации техника из Redis.");
                return null;
            }
        }


        /// <summary>
        /// 🔄 Конвертирует `Technician` в `HashEntry[]` для Redis.
        /// </summary>
        private static HashEntry[] ConvertToHash(Technician technician)
        {
            return
            [
                new("FullName", technician.FullName),
                new("Email", technician.Email),
                new("Address", technician.Address),
                new("PhoneNumber", technician.PhoneNumber),
                new("Latitude", technician.Latitude.ToString(CultureInfo.InvariantCulture)),
                new("Longitude", technician.Longitude.ToString(CultureInfo.InvariantCulture)),
                new("IsAvailable", technician.IsAvailable.ToString()),
                new("CurrentOrderId", technician.CurrentOrderId?.ToString() ?? "null")
            ];
        }

        /// <summary>
        /// 🔄 Конвертирует `HashEntry[]` из Redis в `Technician`.
        /// </summary>
        private static Technician ConvertFromHash(HashEntry[] hashEntries, RedisValue id)
        {
            var dict = hashEntries.ToDictionary(x => x.Name.ToString(), x => x.Value.ToString());

            return new Technician
            {
                Id = Guid.Parse(id.ToString()),
                FullName = dict["FullName"],
                Email = dict["Email"],
                Address = dict["Address"],
                PhoneNumber = dict["PhoneNumber"],
                Latitude = double.Parse(dict["Latitude"], CultureInfo.InvariantCulture),
                Longitude = double.Parse(dict["Longitude"], CultureInfo.InvariantCulture),
                IsAvailable = bool.Parse(dict["IsAvailable"]),
                CurrentOrderId = dict["CurrentOrderId"] != "null" ? Guid.Parse(dict["CurrentOrderId"]) : null
            };
        }

        /// <summary>
        /// 🔍 Получает техника из Redis по его ID.
        /// </summary>
        public async Task<Technician?> GetTechnicianByIdAsync(Guid technicianId)
        {
            var key = $"technician:{technicianId}";
            var hashEntries = await _cache.HashGetAllAsync(key);

            if (hashEntries.Length == 0)
            {
                _logger.LogWarning("⚠️ [Redis] Техник {TechnicianId} не найден в кэше.", technicianId);
                return null;
            }

            return ConvertFromHash(hashEntries, technicianId.ToString());
        }

        public async Task RemoveTechnicianLocationAsync(Guid technicianId)
        {
            var key = $"technician:{technicianId}";

            if (await _cache.KeyExistsAsync(key))
            {
                await _cache.KeyDeleteAsync(key);
                _logger.LogInformation("🗑️ Локация техника {TechnicianId} удалена из Redis.", technicianId);
            }
        }

    }
}
