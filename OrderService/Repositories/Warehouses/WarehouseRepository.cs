using MongoDB.Driver;
using OrderService.Data.Warehouses;
using OrderService.Models.Warehouses;
using OrderService.DTO.GeoLocation;

namespace OrderService.Repositories.Warehouses
{
    public class WarehouseRepository(WarehouseMongoContext context, ILogger<WarehouseRepository> logger)
        : BaseMongoRepository<Warehouse>(context.Warehouses, logger), IStockRepository<Warehouse>
    {
        public new async Task<Warehouse?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Название склада не может быть пустым.", nameof(name));

            try
            {
                var result = await _collection.Find(w => w.Name == name)
                                              .FirstOrDefaultAsync(cancellationToken);

                if (result == null)
                {
                    _logger.LogWarning("Склад с названием '{Name}' не найден.", name);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при поиске склада по названию '{Name}'", name);
                throw;
            }
        }

        public async Task<List<WarehouseCoordinateDTO>> GetWarehouseCoordinatesAsync()
        {
            try
            {
                var warehouses = await _collection.Find(_ => true).ToListAsync();

                var result = warehouses.Select(w => new WarehouseCoordinateDTO
                {
                    WarehouseId = w.ID,
                    Name = w.Name,
                    Address = w.Address,
                    ContactPerson = w.ContactPerson,
                    PhoneNumber = w.PhoneNumber,
                    Latitude = w.Latitude,
                    Longitude = w.Longitude
                }).ToList();

                _logger.LogInformation("📦 Получены координаты {Count} складов.", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ошибка при получении координат складов.");
                return [];
            }
        }

    }
}
