using MongoDB.Driver;
using OrderService.Data.Warehouses;
using OrderService.Models.Warehouses;

namespace OrderService.Repositories.Warehouses
{
    public class EquipmentStockRepository(WarehouseMongoContext context, ILogger<EquipmentStockRepository> logger)
        : BaseMongoRepository<EquipmentStock>(context.EquipmentStock, logger)
    {
        public WarehouseMongoContext Context { get; } = context;
        public ILogger<EquipmentStockRepository> Logger { get; } = logger;

        public async Task<EquipmentStock?> GetByModelNameAsync(string modelName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(modelName))
                throw new ArgumentException("Название модели не может быть пустым.", nameof(modelName));

            try
            {
                var result = await _collection.Find(e => e.ModelName == modelName)
                                              .FirstOrDefaultAsync(cancellationToken);

                if (result == null)
                {
                    Logger.LogWarning("Оборудование с названием '{ModelName}' не найдено.", modelName);
                }

                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Ошибка при поиске оборудования по названию '{ModelName}'", modelName);
                throw;
            }
        }
    }
}
