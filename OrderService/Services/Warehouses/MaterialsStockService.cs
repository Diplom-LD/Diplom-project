using OrderService.Models.Warehouses;
using OrderService.Repositories.Warehouses;

namespace OrderService.Services.Warehouses
{
    public class MaterialsStockService(IStockRepository<MaterialsStock> repository, ILogger<MaterialsStockService> logger)
            : BaseStockService<MaterialsStock>(repository, logger)
    {
        /// <summary>
        /// Валидация данных о материале перед добавлением/обновлением.
        /// </summary>
        protected override void ValidateStockItem(MaterialsStock material)
        {
            if (string.IsNullOrWhiteSpace(material.MaterialName))
                throw new ArgumentException("Название материала не может быть пустым.", nameof(material));

            if (material.MaterialName.Length > 50)
                throw new ArgumentException("Название материала не может превышать 50 символов.", nameof(material));

            if (material.Price <= 0)
                throw new ArgumentException("Цена должна быть больше 0.", nameof(material));

            if (material.Quantity < 0)
                throw new ArgumentException("Количество не может быть отрицательным.", nameof(material));
        }

        /// <summary>
        /// Логирование действий с материалом.
        /// </summary>
        protected override void LogAction(string action, MaterialsStock? item, string id)
        {
            _logger.LogInformation("{Action}: {MaterialName} (ID: {Id})", action,
                item?.MaterialName ?? "Неизвестный материал", id);
        }
    }
}
