using OrderService.Models.Warehouses;
using OrderService.Repositories.Warehouses;
using OrderService.DTO.Warehouses;  

namespace OrderService.Services.Warehouses
{
    public class EquipmentStockService(IStockRepository<EquipmentStock> repository, ILogger<EquipmentStockService> logger)
            : BaseStockService<EquipmentStock>(repository, logger)
    {
        /// <summary>
        /// Валидация данных об оборудовании перед добавлением/обновлением.
        /// </summary>
        protected override void ValidateStockItem(EquipmentStock equipment)
        {
            if (string.IsNullOrWhiteSpace(equipment.ModelName))
                throw new ArgumentException("Название модели не может быть пустым.", nameof(equipment));

            if (equipment.ModelName.Length > 50)
                throw new ArgumentException("Название модели не может превышать 50 символов.", nameof(equipment));

            if (equipment.Price <= 0)
                throw new ArgumentException("Цена должна быть больше 0.", nameof(equipment));

            if (equipment.Quantity < 0)
                throw new ArgumentException("Количество не может быть отрицательным.", nameof(equipment));

            if (equipment.BTU < 1000 || equipment.BTU > 300000)
                throw new ArgumentException("BTU должен быть в диапазоне от 1000 до 300000.", nameof(equipment));

            if (equipment.ServiceArea <= 0)
                throw new ArgumentException("Площадь обслуживания должна быть больше 0.", nameof(equipment));
        }

        /// <summary>
        /// Логирование действий с оборудованием.
        /// </summary>
        protected override void LogAction(string action, EquipmentStock? item, string id)
        {
            _logger.LogInformation("{Action}: {ModelName} (ID: {Id})", action,
                item?.ModelName ?? "Неизвестное оборудование", id);
        }

        public async Task<List<AggregatedEquipmentDTO>> GetAllEquipmentFromWarehousesAsync(CancellationToken cancellationToken = default)
        {
            var allEquipment = await GetAllAsync(cancellationToken);

            var aggregatedEquipment = allEquipment
                .GroupBy(e => new { e.ModelName, e.BTU, e.ServiceArea, e.Price }) 
                .Select(group => new AggregatedEquipmentDTO
                {
                    ModelName = group.Key.ModelName,
                    BTU = group.Key.BTU, 
                    ServiceArea = group.Key.ServiceArea,
                    Price = group.Key.Price, 
                    TotalQuantity = group.Sum(e => e.Quantity)
                })
                .OrderByDescending(e => e.TotalQuantity)
                .ToList();

            _logger.LogInformation("✅ Найдено {Count} уникальных моделей оборудования со всех складов.", aggregatedEquipment.Count);
            return aggregatedEquipment;
        }


    }
}
