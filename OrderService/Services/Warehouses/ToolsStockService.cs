using Microsoft.Extensions.Logging;
using OrderService.Models.Warehouses;
using OrderService.Repositories.Warehouses;

namespace OrderService.Services.Warehouses
{
    public class ToolsStockService(IStockRepository<ToolsStock> repository, ILogger<ToolsStockService> logger)
            : BaseStockService<ToolsStock>(repository, logger)
    {
        /// <summary>
        /// Валидация данных об инструменте перед добавлением/обновлением.
        /// </summary>
        protected override void ValidateStockItem(ToolsStock tool)
        {
            if (string.IsNullOrWhiteSpace(tool.ToolName))
                throw new ArgumentException("Название инструмента не может быть пустым.", nameof(tool));

            if (tool.ToolName.Length > 50)
                throw new ArgumentException("Название инструмента не может превышать 50 символов.", nameof(tool));

            if (tool.Quantity < 0)
                throw new ArgumentException("Количество не может быть отрицательным.", nameof(tool));
        }

        /// <summary>
        /// Логирование действий с инструментами.
        /// </summary>
        protected override void LogAction(string action, ToolsStock? item, string id)
        {
            _logger.LogInformation("{Action}: {ToolName} (ID: {Id})", action,
                item?.ToolName ?? "Неизвестный инструмент", id);
        }
    }
}
