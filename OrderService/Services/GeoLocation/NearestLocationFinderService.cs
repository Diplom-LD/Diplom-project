using OrderService.DTO.GeoLocation;
using OrderService.DTO.Technicians;
using OrderService.Models.Technicians;
using OrderService.Services.Warehouses;
using OrderService.Models.Enums;
using OrderService.Models.Warehouses;
using OrderService.Services.Orders;
using OrderService.Repositories.Users;
using OrderService.DTO.Users;
using OrderService.Models.Users;

namespace OrderService.Services.GeoLocation
{
    public class NearestLocationFinderService(
        WarehouseService warehouseService,
        UserPostgreRepository userPostgreRepository,
        UserRedisRepository userRedisRepository,
        OptimizedRouteService optimizedRouteService,
        EquipmentService equipmentService,
        ILogger<NearestLocationFinderService> logger)
    {
        private readonly WarehouseService _warehouseService = warehouseService;
        private readonly UserPostgreRepository _userPostgreRepository = userPostgreRepository;
        private readonly UserRedisRepository _userRedisRepository = userRedisRepository;
        private readonly OptimizedRouteService _optimizedRouteService = optimizedRouteService;
        private readonly EquipmentService _equipmentService = equipmentService;
        private readonly ILogger<NearestLocationFinderService> _logger = logger;

        /// <summary>
        /// 📍 Определяет ближайший склад, проверяет наличие ресурсов и строит маршруты.
        /// </summary>
        public async Task<NearestLocationResultDTO> FindNearestLocationWithRoutesAsync(
            double latitude, double longitude, OrderType orderType, int? requiredBTU,
            List<string>? requestedTechnicianIds = null, int technicianCount = 2)
        {
            _logger.LogInformation("🔍 Поиск склада с необходимыми ресурсами и ближайших {TechnicianCount} техников...", technicianCount);

            // 🏪 1️⃣ Находим подходящий склад
            var (primaryWarehouse, secondaryWarehouse) = await FindBestWarehousesAsync(latitude, longitude, orderType, requiredBTU);

            if (primaryWarehouse == null)
            {
                _logger.LogWarning("⚠️ Не найден подходящий склад!");
                return new NearestLocationResultDTO
                {
                    NearestWarehouse = null,
                    SecondaryWarehouse = null,
                    SelectedTechnicians = [],
                    Routes = []
                };
            }

            // 👷 2️⃣ Поиск ближайших доступных техников
            var technicians = await FindTechniciansAsync(latitude, longitude, requestedTechnicianIds, technicianCount);
            if (technicians.Count == 0)
            {
                _logger.LogWarning("⚠️ Нет доступных техников, маршруты не строятся!");
                return new NearestLocationResultDTO
                {
                    NearestWarehouse = primaryWarehouse,
                    SecondaryWarehouse = secondaryWarehouse,
                    SelectedTechnicians = [],
                    Routes = []
                };
            }

            // 🛣️ 3️⃣ Строим маршруты
            var routes = await _optimizedRouteService.BuildOptimizedRouteAsync(
                latitude, longitude, primaryWarehouse, secondaryWarehouse, technicians);

            return new NearestLocationResultDTO
            {
                NearestWarehouse = primaryWarehouse,
                SecondaryWarehouse = secondaryWarehouse,
                SelectedTechnicians = technicians,
                Routes = routes
            };
        }

        /// <summary>
        /// 🏪 Поиск складов с необходимыми ресурсами
        /// </summary>
        private async Task<(WarehouseDTO? primary, WarehouseDTO? secondary)> FindBestWarehousesAsync(
            double latitude, double longitude, OrderType orderType, int? requiredBTU)
        {
            var warehouses = await _warehouseService.GetAllAsync();
            if (warehouses.Count == 0)
            {
                _logger.LogWarning("⚠️ В системе нет доступных складов!");
                return (null, null);
            }

            _logger.LogInformation("📍 Поиск ближайших складов для координат: {Latitude}, {Longitude}", latitude, longitude);

            var suitableWarehouses = new List<Warehouse>();

            foreach (var warehouse in warehouses)
            {
                bool hasEquipment = !requiredBTU.HasValue || await _equipmentService.CheckEquipmentAvailabilityAsync(warehouse.ID.ToString(), requiredBTU.Value);
                bool hasMaterials = await _equipmentService.CheckMaterialsAvailabilityAsync(warehouse.ID.ToString());
                bool hasTools = await _equipmentService.CheckToolsAvailabilityAsync(warehouse.ID.ToString(), orderType);

                if (hasEquipment && hasMaterials && hasTools)
                {
                    _logger.LogInformation("✅ Склад {WarehouseName} содержит ВСЁ необходимое", warehouse.Name);
                    return (ConvertToWarehouseDTO(warehouse), null);
                }
                else if (hasMaterials && hasTools)
                {
                    suitableWarehouses.Add(warehouse);
                }
            }

            if (suitableWarehouses.Count > 0)
            {
                var primaryWarehouse = suitableWarehouses.FirstOrDefault();
                var secondaryWarehouse = suitableWarehouses.Skip(1).FirstOrDefault();

                return (ConvertToWarehouseDTO(primaryWarehouse), ConvertToWarehouseDTO(secondaryWarehouse));
            }

            _logger.LogWarning("⚠️ Не удалось найти подходящие склады!");
            return (null, null);
        }

        /// <summary>
        /// 👷 Поиск ближайших доступных техников (из Redis и PostgreSQL)
        /// </summary>
        public async Task<List<TechnicianDTO>> FindTechniciansAsync(
    double latitude, double longitude, List<string>? requestedTechnicianIds, int technicianCount)
        {
            _logger.LogInformation("👷 Поиск ближайших техников (или по ID)");

            // 1️⃣ Загружаем всех техников из Redis
            var technicians = await _userRedisRepository.GetAllTechniciansAsync();

            // 2️⃣ Если техник отсутствует в Redis, загружаем из PostgreSQL
            if (technicians.Count == 0)
            {
                var techniciansFromDb = await _userPostgreRepository.GetTechniciansAsync();
                technicians = techniciansFromDb;

                if (technicians.Count > 0)
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

            List<Technician> selectedTechnicians;

            if (requestedTechnicianIds != null && requestedTechnicianIds.Count > 0)
            {
                selectedTechnicians = [.. availableTechnicians.Where(t => requestedTechnicianIds.Contains(t.Id.ToString()))];

                _logger.LogInformation("✅ Выбраны конкретные техники по ID: {Technicians}",
                    string.Join(", ", selectedTechnicians.Select(t => t.Id)));
            }
            else
            {
                selectedTechnicians = [.. availableTechnicians
                    .OrderBy(t => DistanceCalculator.CalculateDistance(latitude, longitude, t.Latitude, t.Longitude))
                    .Take(technicianCount)];

                _logger.LogInformation("✅ Выбраны {TechnicianCount} ближайших доступных техников", selectedTechnicians.Count);
            }

            return [.. selectedTechnicians.Select(t => new TechnicianDTO
            {
                Id = t.Id,
                FullName = t.FullName,
                Address = t.Address,
                PhoneNumber = t.PhoneNumber,
                Latitude = t.Latitude,
                Longitude = t.Longitude,
                IsAvailable = t.IsAvailable,
                CurrentOrderId = t.CurrentOrderId
            })];
        }


        /// <summary>
        /// 🔄 Конвертирует Warehouse в WarehouseDTO
        /// </summary>
        private static WarehouseDTO ConvertToWarehouseDTO(Warehouse? warehouse)
        {
            if (warehouse == null) return null!;

            return new WarehouseDTO
            {
                Id = warehouse.ID,
                Name = warehouse.Name,
                Latitude = warehouse.Latitude,
                Longitude = warehouse.Longitude
            };
        }

        /// <summary>
        /// 👷 Поиск ближайшего склада
        /// </summary>
        public async Task<WarehouseDTO?> FindNearestWarehouseAsync(double latitude, double longitude)
        {
            var warehouses = await _warehouseService.GetAllAsync();
            if (warehouses.Count == 0)
            {
                _logger.LogWarning("⚠️ В системе нет доступных складов!");
                return null;
            }

            _logger.LogInformation("📍 Поиск ближайшего склада для координат: {Latitude}, {Longitude}", latitude, longitude);

            var nearestWarehouse = warehouses
                .Select(w => new
                {
                    Warehouse = w,
                    Distance = DistanceCalculator.CalculateDistance(latitude, longitude, w.Latitude, w.Longitude)
                })
                .OrderBy(w => w.Distance)
                .FirstOrDefault()?.Warehouse;

            if (nearestWarehouse == null)
            {
                _logger.LogWarning("⚠️ Ближайший склад не найден для координат: {Latitude}, {Longitude}", latitude, longitude);
                return null;
            }

            return ConvertToWarehouseDTO(nearestWarehouse);
        }


    }
}
