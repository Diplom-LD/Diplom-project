using OrderService.Repositories.Warehouses;
using OrderService.Models.Enums;
using OrderService.DTO.Warehouses;
using OrderService.Models.Warehouses;

namespace OrderService.Services.Orders
{
    public class WarehouseAvailabilityService(
        IStockRepository<Warehouse> warehouseRepository,
        IStockRepository<EquipmentStock> equipmentRepository,
        IStockRepository<MaterialsStock> materialsRepository,
        IStockRepository<ToolsStock> toolsRepository,
        ILogger<WarehouseAvailabilityService> logger)
    {
        private readonly IStockRepository<Warehouse> _warehouseRepository = warehouseRepository;
        private readonly IStockRepository<EquipmentStock> _equipmentRepository = equipmentRepository;
        private readonly IStockRepository<MaterialsStock> _materialsRepository = materialsRepository;
        private readonly IStockRepository<ToolsStock> _toolsRepository = toolsRepository;
        private readonly ILogger<WarehouseAvailabilityService> _logger = logger;

        /// <summary>
        /// 🔍 Получает все склады без проверки наличия ресурсов.
        /// </summary>
        public async Task<List<WarehouseDTO>> GetAllWarehousesAsync()
        {
            _logger.LogInformation("📦 Получение всех складов...");
            var warehouses = await _warehouseRepository.GetAllAsync();
            return [.. warehouses.Select(ConvertToWarehouseDTO)];
        }


        /// <summary>
        /// 🔍 Проверяет, есть ли на складе все необходимые ресурсы.
        /// </summary>
        public async Task<bool> CheckWarehouseHasAllResourcesAsync(string warehouseId, OrderType orderType, string? requiredModelName)
        {
            _logger.LogInformation("🔍 Проверка склада {WarehouseId} на наличие всех ресурсов для {OrderType}", warehouseId, orderType);

            bool hasEquipment = string.IsNullOrEmpty(requiredModelName) || await CheckEquipmentAvailabilityByModelAsync(warehouseId, requiredModelName);
            bool hasMaterials = await CheckMaterialsAvailabilityAsync(warehouseId, orderType);
            bool hasTools = await CheckToolsAvailabilityAsync(warehouseId, orderType);

            return hasEquipment && hasMaterials && hasTools;
        }

        /// <summary>
        /// 🔍 Возвращает доступное оборудование на складе, ограничивая по количеству.
        /// </summary>
        public async Task<List<EquipmentStock>> GetAvailableEquipmentAsync(string warehouseId, string? modelName = null, int requiredQuantity = 1)
        {
            _logger.LogInformation("📦 Получение оборудования {ModelName} (нужно {RequiredQuantity}) на складе {WarehouseId}...", modelName, requiredQuantity, warehouseId);

            var equipment = await _equipmentRepository.GetByWarehouseIdAsync(warehouseId);

            if (!string.IsNullOrEmpty(modelName))
            {
                equipment = [.. equipment.Where(e => e.ModelName == modelName)];
            }

            var availableEquipment = equipment.Where(e => e.Quantity > 0).ToList();

            // Если запрошенное количество больше доступного, возвращаем максимум возможного
            foreach (var eq in availableEquipment)
            {
                eq.Quantity = Math.Min(eq.Quantity, requiredQuantity);
            }

            return availableEquipment;
        }

        /// <summary>
        /// 🔍 Возвращает список доступных материалов (по одной штуке каждого типа).
        /// </summary>
        public async Task<List<MaterialsStock>> GetAvailableMaterialsAsync(string warehouseId, OrderType orderType, int requiredQuantity = 1)
        {
            _logger.LogInformation("📦 Получение материалов (по {RequiredQuantity} шт.) на складе {WarehouseId} для {OrderType}...", requiredQuantity, warehouseId, orderType);

            var materials = await _materialsRepository.GetByWarehouseIdAsync(warehouseId);
            var requiredMaterials = RequiredMaterials.GetValueOrDefault(orderType, []);

            var availableMaterials = materials
                .Where(m => requiredMaterials.Contains(m.MaterialName) && m.Quantity > 0)
                .GroupBy(m => m.MaterialName) 
                .Select(g => g.First())
                .ToList();

            foreach (var material in availableMaterials)
            {
                material.Quantity = Math.Min(material.Quantity, requiredQuantity); 
            }

            return availableMaterials;
        }

        /// <summary>
        /// 🔍 Возвращает список доступных инструментов (по одной штуке каждого типа).
        /// </summary>
        public async Task<List<ToolsStock>> GetAvailableToolsAsync(string warehouseId, OrderType orderType, int requiredQuantity = 1)
        {
            _logger.LogInformation("🔧 Получение инструментов (по {RequiredQuantity} шт.) на складе {WarehouseId} для {OrderType}...", requiredQuantity, warehouseId, orderType);

            var tools = await _toolsRepository.GetByWarehouseIdAsync(warehouseId);
            var requiredTools = RequiredTools.GetValueOrDefault(orderType, []);

            var availableTools = tools
                .Where(t => requiredTools.Contains(t.ToolName) && t.Quantity > 0)
                .GroupBy(t => t.ToolName) // Группируем по названию
                .Select(g => g.First()) // Берем первый инструмент каждого типа
                .ToList();

            foreach (var tool in availableTools)
            {
                tool.Quantity = Math.Min(tool.Quantity, requiredQuantity); // Берем запрашиваемое количество или минимум
            }

            return availableTools;
        }


        /// <summary>
        /// 🔍 Проверяет наличие оборудования на складе.
        /// </summary>
        private async Task<bool> CheckEquipmentAvailabilityByModelAsync(string warehouseId, string modelName)
        {
            var equipment = await _equipmentRepository.GetByWarehouseIdAsync(warehouseId);
            return equipment.Any(eq => eq.ModelName == modelName && eq.Quantity > 0);
        }

        /// <summary>
        /// 🔍 Проверяет наличие материалов на складе.
        /// </summary>
        private async Task<bool> CheckMaterialsAvailabilityAsync(string warehouseId, OrderType orderType)
        {
            var materials = (await _materialsRepository.GetByWarehouseIdAsync(warehouseId))
                .Where(m => m.Quantity > 0)
                .Select(m => m.MaterialName)
                .Distinct()
                .ToList();

            return RequiredMaterials.TryGetValue(orderType, out var requiredMaterials) &&
                   requiredMaterials.All(m => materials.Contains(m));
        }

        /// <summary>
        /// 🔍 Проверяет наличие инструментов на складе.
        /// </summary>
        private async Task<bool> CheckToolsAvailabilityAsync(string warehouseId, OrderType orderType)
        {
            var tools = (await _toolsRepository.GetByWarehouseIdAsync(warehouseId))
                .Where(t => t.Quantity > 0)
                .Select(t => t.ToolName)
                .Distinct()
                .ToList();

            return RequiredTools.TryGetValue(orderType, out var requiredTools) &&
                   requiredTools.All(t => tools.Contains(t));
        }

        /// <summary>
        /// 🔄 Преобразует `Warehouse` в `WarehouseDTO`.
        /// </summary>
        private static WarehouseDTO ConvertToWarehouseDTO(Warehouse warehouse)
        {
            return new WarehouseDTO
            {
                Id = warehouse.ID,
                Name = warehouse.Name,
                Latitude = warehouse.Latitude,
                Longitude = warehouse.Longitude
            };
        }

        private static readonly Dictionary<OrderType, string[]> RequiredMaterials = new()
        {
            { OrderType.Installation, new[] { "Медная трубка 1/4 дюйма", "Фреон R410A", "Крепежные анкера" } },
            { OrderType.Maintenance, new[] { "Фреон R410A", "Антисептический раствор", "Герметик" } }
        };

        private static readonly Dictionary<OrderType, string[]> RequiredTools = new()
        {
            { OrderType.Installation, new[] { "Вакуумный насос", "Манометрический коллектор", "Перфоратор" } },
            { OrderType.Maintenance, new[] { "Газовый паяльник", "Мультиметр", "Ключ-трещотка" } }
        };
    }
}
