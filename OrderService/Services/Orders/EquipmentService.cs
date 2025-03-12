using OrderService.Models.Warehouses;
using OrderService.Repositories.Warehouses;
using OrderService.DTO.Orders;
using OrderService.Models.Enums;

namespace OrderService.Services.Orders
{
    public class EquipmentService(
        IStockRepository<EquipmentStock> equipmentRepository,
        IStockRepository<MaterialsStock> materialsRepository,
        IStockRepository<ToolsStock> toolsRepository)
    {
        private readonly IStockRepository<EquipmentStock> _equipmentRepository = equipmentRepository;
        private readonly IStockRepository<MaterialsStock> _materialsRepository = materialsRepository;
        private readonly IStockRepository<ToolsStock> _toolsRepository = toolsRepository;
        private static readonly string[] sourceArray =
            [
                "Медная трубка", "Теплоизоляция", "Фреон",
                "Крепежные анкера", "Дренажный шланг"
            ];

        /// <summary>
        /// ✅ Проверяет наличие оборудования на заданном складе.
        /// </summary>
        public async Task<bool> CheckEquipmentAvailabilityAsync(string warehouseId, int requiredBTU)
        {
            var equipment = await _equipmentRepository.GetByWarehouseIdAsync(warehouseId);
            return equipment.Any(e => e.BTU >= requiredBTU);
        }

        /// <summary>
        /// ✅ Подбирает оборудование на указанном складе.
        /// </summary>
        public async Task<List<EquipmentStock>> GetEquipmentForOrderAsync(CreateOrderRequest request, string warehouseId)
        {
            var equipment = await _equipmentRepository.GetByWarehouseIdAsync(warehouseId);
            return [.. equipment.Where(e => e.BTU >= request.RequiredBTU)];
        }

        /// <summary>
        /// ✅ Проверяет наличие всех необходимых материалов на складе.
        /// </summary>
        public async Task<bool> CheckMaterialsAvailabilityAsync(string warehouseId)
        {
            var materials = await _materialsRepository.GetByWarehouseIdAsync(warehouseId);
            return materials.Count > 0;
        }

        /// <summary>
        /// ✅ Проверяет наличие всех необходимых инструментов.
        /// </summary>
        public async Task<bool> CheckToolsAvailabilityAsync(string warehouseId, OrderType orderType)
        {
            var tools = await _toolsRepository.GetByWarehouseIdAsync(warehouseId);
            var requiredTools = GetRequiredToolsList(orderType);
            return requiredTools.All(tool => tools.Any(t => t.ToolName == tool));
        }

        /// <summary>
        /// ✅ Получает необходимые материалы для установки.
        /// </summary>
        public async Task<List<MaterialsStock>> GetRequiredMaterials(List<EquipmentStock> selectedEquipment, string warehouseId)
        {
            if (selectedEquipment is not null)
            {
                var materials = await _materialsRepository.GetByWarehouseIdAsync(warehouseId);
                return [.. materials.Where(m => sourceArray.Contains(m.MaterialName))];
            }

            throw new ArgumentNullException(nameof(selectedEquipment));
        }

        /// <summary>
        /// ✅ Возвращает список необходимых инструментов в зависимости от типа заявки.
        /// </summary>
        private static List<string> GetRequiredToolsList(OrderType orderType)
        {
            return orderType switch
            {
                OrderType.Installation =>
                [
                    "Вакуумный насос", "Манометрический коллектор", "Электродрель",
                    "Трубогиб", "Перфоратор", "Фрезер", "Газовый паяльник",
                    "Клещи для развальцовки", "Набор отверток", "Рулетка"
                ],
                OrderType.Maintenance =>
                [
                    "Манометрический коллектор", "Электродрель", "Газовый паяльник",
                    "Набор отверток", "Рулетка", "Мультиметр", "Токовые клещи",
                    "Тепловизор", "Уровень", "Ключ-трещотка"
                ],
                _ => []
            };
        }

        /// <summary>
        /// ✅ Проверяет наличие заданного оборудования, инструментов и материалов на складе и автоматически собирает пакет инструментов для заявки.
        /// </summary>
        public async Task<(bool hasAllResources, List<EquipmentStock> selectedEquipment, List<MaterialsStock> selectedMaterials, List<string> selectedTools)>
            CheckAndCollectResourcesAsync(CreateOrderRequest request, string warehouseId)
        {
            var hasEquipment = await CheckEquipmentAvailabilityAsync(warehouseId, request.RequiredBTU ?? 0);
            var hasMaterials = await CheckMaterialsAvailabilityAsync(warehouseId);
            var hasTools = await CheckToolsAvailabilityAsync(warehouseId, request.OrderType);

            if (hasEquipment && hasMaterials && hasTools)
            {
                var selectedEquipment = await GetEquipmentForOrderAsync(request, warehouseId);
                var selectedMaterials = await GetRequiredMaterials(selectedEquipment, warehouseId);
                var selectedTools = GetRequiredToolsList(request.OrderType);
                return (true, selectedEquipment, selectedMaterials, selectedTools);
            }

            return (false, new List<EquipmentStock>(), new List<MaterialsStock>(), new List<string>());
        }
    }
}