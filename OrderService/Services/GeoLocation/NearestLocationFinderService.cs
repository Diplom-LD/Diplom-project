using OrderService.DTO.GeoLocation;
using OrderService.Models.Enums;
using OrderService.Repositories.Users;
using OrderService.DTO.Users;
using OrderService.Models.Users;
using OrderService.DTO.Orders;
using OrderService.Services.Orders;
using OrderService.DTO.Warehouses;
using OrderService.Repositories.Warehouses;

namespace OrderService.Services.GeoLocation
{
    public class NearestLocationFinderService(
        WarehouseAvailabilityService warehouseAvailabilityService,
        UserPostgreRepository userPostgreRepository,
        UserRedisRepository userRedisRepository,
        WarehouseRepository warehouseRepository,
        IOptimizedRouteService optimizedRouteService,
        ILogger<NearestLocationFinderService> logger)
    {
        private readonly WarehouseAvailabilityService _warehouseAvailabilityService = warehouseAvailabilityService;
        private readonly UserPostgreRepository _userPostgreRepository = userPostgreRepository;
        private readonly UserRedisRepository _userRedisRepository = userRedisRepository;
        private readonly WarehouseRepository _warehouseRepository = warehouseRepository;
        private readonly IOptimizedRouteService _optimizedRouteService = optimizedRouteService;
        private readonly ILogger<NearestLocationFinderService> _logger = logger;

        /// <summary>
        /// 🔍 Определяет ближайшие склады, техников и строит маршруты.
        /// </summary>
        public async Task<NearestLocationResultDTO> FindNearestLocationsAsync(
        double latitude,
        double longitude,
        OrderType orderType,
        string? requiredModelName,
        List<string>? requestedTechnicianIds = null)
        {
            _logger.LogInformation("🔍 Поиск ближайшего подходящего склада...");

            // 1️ Получаем **все** склады, а не только те, что полностью соответствуют
            var allWarehouses = await _warehouseAvailabilityService.GetAllWarehousesAsync();
            if (allWarehouses.Count == 0)
            {
                _logger.LogWarning("⚠️ В системе нет складов!");
                return new NearestLocationResultDTO();
            }

            // 2️ Сортируем склады по расстоянию до точки установки
            var sortedWarehouses = allWarehouses.OrderBy(w =>
                DistanceCalculator.CalculateDistance(latitude, longitude, w.Latitude, w.Longitude)).ToList();

            // 3️ Проверяем склады по порядку, пока не найдём тот, где есть всё
            WarehouseDTO? nearestWarehouse = null;
            foreach (var warehouse in sortedWarehouses)
            {
                bool hasAllResources = await _warehouseAvailabilityService.CheckWarehouseHasAllResourcesAsync(
                    warehouse.Id.ToString(), orderType, requiredModelName);

                if (hasAllResources)
                {
                    nearestWarehouse = warehouse;
                    _logger.LogInformation("✅ Найден ближайший подходящий склад: {WarehouseId}", warehouse.Id);
                    break;
                }
            }

            if (nearestWarehouse == null)
            {
                _logger.LogWarning("⚠️ Нет складов, содержащих все необходимые ресурсы!");
                return new NearestLocationResultDTO();
            }

            // 4️ Получение ресурсов с выбранного склада
            var equipment = (await _warehouseAvailabilityService.GetAvailableEquipmentAsync(nearestWarehouse.Id.ToString(), requiredModelName))
                .Select(e => new OrderEquipmentDTO
                {
                    ModelName = e.ModelName,
                    ModelPrice = e.Price,
                    ModelBTU = e.BTU,
                    ServiceArea = e.ServiceArea,
                    ModelSource = "Warehouse",
                    Quantity = e.Quantity
                }).ToList();

            var materials = (await _warehouseAvailabilityService.GetAvailableMaterialsAsync(nearestWarehouse.Id.ToString(), orderType))
                .Select(m => new OrderMaterialDTO { MaterialName = m.MaterialName, Quantity = m.Quantity, MaterialPrice = m.Price }).ToList();

            var tools = (await _warehouseAvailabilityService.GetAvailableToolsAsync(nearestWarehouse.Id.ToString(), orderType))
                .Select(t => new OrderToolDTO { ToolName = t.ToolName, Quantity = t.Quantity }).ToList();

            _logger.LogInformation("📦 Получено: {EquipmentCount} оборудования, {MaterialCount} материалов, {ToolCount} инструментов",
                equipment.Count, materials.Count, tools.Count);

            // 5️ Поиск ближайших техников
            var technicians = await FindTechniciansAsync(latitude, longitude, requestedTechnicianIds);

            if (technicians.Count == 0)
            {
                _logger.LogWarning("⚠️ Нет доступных техников!");
                return new NearestLocationResultDTO
                {
                    NearestWarehouses = [nearestWarehouse],
                    AvailableEquipment = equipment,
                    AvailableMaterials = materials,
                    AvailableTools = tools
                };
            }

            _logger.LogInformation("✅ Найдено {TechnicianCount} доступных техников.", technicians.Count);

            // 6️ Построение маршрутов
            var routes = await _optimizedRouteService.BuildOptimizedRouteAsync(latitude, longitude, [nearestWarehouse], technicians);
            _logger.LogInformation("✅ Построено {RouteCount} маршрутов.", routes.Count);

            return new NearestLocationResultDTO
            {
                NearestWarehouses = [nearestWarehouse],
                SelectedTechnicians = technicians,
                AvailableEquipment = equipment,
                AvailableMaterials = materials,
                AvailableTools = tools,
                Routes = routes
            };
        }


        /// <summary>
        /// 🔍 Поиск доступных техников (из Redis и PostgreSQL)
        /// </summary>
        public async Task<List<TechnicianDTO>> FindTechniciansAsync(double latitude, double longitude, List<string>? requestedTechnicianIds)
        {
            _logger.LogInformation("🔍 Поиск доступных техников...");

            var technicians = await _userRedisRepository.GetAllTechniciansAsync();
            if (technicians.Count == 0)
            {
                technicians = await _userPostgreRepository.GetTechniciansAsync();
                if (technicians.Count != 0)
                {
                    await _userRedisRepository.SaveTechniciansAsync(technicians);
                }
            }

            var availableTechnicians = technicians.Where(t => t.IsAvailable).ToList();
            if (availableTechnicians.Count == 0)
            {
                _logger.LogWarning("⚠️ Нет доступных техников!");
                return [];
            }

            List<TechnicianDTO> selectedTechnicians = requestedTechnicianIds != null && requestedTechnicianIds.Count > 0
                ? [.. availableTechnicians.Where(t => requestedTechnicianIds.Contains(t.Id.ToString())).Select(ConvertToTechnicianDTO)]
                : [.. availableTechnicians.OrderBy(t => DistanceCalculator.CalculateDistance(latitude, longitude, t.Latitude, t.Longitude)).Take(2).Select(ConvertToTechnicianDTO)];

            _logger.LogInformation("✅ Выбрано {TechnicianCount} техников.", selectedTechnicians.Count);
            return selectedTechnicians;
        }

        /// <summary>
        /// 🔄 Конвертация `Technician` в `TechnicianDTO`
        /// </summary>
        private static TechnicianDTO ConvertToTechnicianDTO(Technician technician)
        {
            return new TechnicianDTO
            {
                Id = technician.Id,
                FullName = technician.FullName,
                Address = technician.Address,
                PhoneNumber = technician.PhoneNumber,
                Latitude = technician.Latitude,
                Longitude = technician.Longitude,
                IsAvailable = technician.IsAvailable,
                CurrentOrderId = technician.CurrentOrderId
            };
        }

        /// <summary>
        /// 🔄 Получение данных про техников и складов.
        /// </summary>
        public async Task<List<TechnicianCoordinateDTO>> GetAllTechnicianHomeLocationsAsync()
        {
            return await _userPostgreRepository.GetTechnicianCoordinatesAsync();
        }
        public async Task<List<WarehouseCoordinateDTO>> GetAllWarehouseLocationsAsync()
        {
            return await _warehouseRepository.GetWarehouseCoordinatesAsync();
        }

    }
}
