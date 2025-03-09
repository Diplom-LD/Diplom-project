using MongoDB.Driver;
using OrderService.Data.Warehouses;
using OrderService.Models.Warehouses;

namespace OrderService.Repositories.Warehouses
{
    public class MaterialsStockRepository(WarehouseMongoContext context, ILogger<MaterialsStockRepository> logger) : BaseMongoRepository<MaterialsStock>(context.MaterialsStock, logger)
    {
        public async Task<MaterialsStock?> GetByMaterialNameAsync(string materialName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(materialName))
                throw new ArgumentException("Название материала не может быть пустым.", nameof(materialName));

            try
            {
                var result = await _collection.Find(m => m.MaterialName == materialName)
                                              .FirstOrDefaultAsync(cancellationToken);

                if (result == null)
                {
                    _logger.LogWarning("Материал с названием '{MaterialName}' не найден.", materialName);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при поиске материала по названию '{MaterialName}'", materialName);
                throw;
            }
        }
    }
}
