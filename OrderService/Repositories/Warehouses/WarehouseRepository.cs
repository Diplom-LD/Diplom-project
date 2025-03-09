using MongoDB.Driver;
using OrderService.Data.Warehouses;
using OrderService.Models.Warehouses;

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
    }
}
