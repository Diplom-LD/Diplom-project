using MongoDB.Driver;
using OrderService.Data.Warehouses;
using OrderService.Models.Warehouses;

namespace OrderService.Repositories.Warehouses
{
    public class ToolsStockRepository(WarehouseMongoContext context, ILogger<ToolsStockRepository> logger)
        : BaseMongoRepository<ToolsStock>(context.ToolsStock, logger)
    {
        public async Task<ToolsStock?> GetByToolNameAsync(string toolName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(toolName))
                throw new ArgumentException("Название инструмента не может быть пустым.", nameof(toolName));

            try
            {
                var result = await _collection.Find(t => t.ToolName == toolName)
                                              .FirstOrDefaultAsync(cancellationToken);

                if (result == null)
                {
                    _logger.LogWarning("Инструмент с названием '{ToolName}' не найден.", toolName);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при поиске инструмента по названию '{ToolName}'", toolName);
                throw;
            }
        }
    }
}
