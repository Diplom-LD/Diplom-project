using OrderService.Services.GeoLocation.RouteServiceClient;
using OrderService.DTO.GeoLocation;
using OrderService.DTO.Users;
using OrderService.DTO.Warehouses;

namespace OrderService.Services.GeoLocation
{
    public class OptimizedRouteService(IRouteServiceClient routeServiceClient, ILogger<OptimizedRouteService> logger) : IOptimizedRouteService
    {
        private readonly IRouteServiceClient _routeServiceClient = routeServiceClient ?? throw new ArgumentNullException(nameof(routeServiceClient));
        private readonly ILogger<OptimizedRouteService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        /// <summary>
        /// ✅ Строит маршрут техника с учетом всех ресурсов (оборудование, материалы, инструменты)
        /// </summary>
        public async Task<List<RouteDTO>> BuildOptimizedRouteAsync(
    double jobLatitude, double jobLongitude,
    List<WarehouseDTO> warehouses,
    List<TechnicianDTO> technicians)
        {
            var routes = new List<RouteDTO>();

            if (technicians.Count == 0 || warehouses.Count == 0)
            {
                _logger.LogWarning("⚠️ Нет доступных техников или складов.");
                return routes;
            }

            var validTechnicians = technicians.Where(t => t.Latitude != 0 && t.Longitude != 0).ToList();
            if (validTechnicians.Count == 0)
            {
                _logger.LogWarning("⚠️ Все техники имеют нулевые координаты!");
                return routes;
            }

            _logger.LogInformation("📍 Доступно техников: {Count}", validTechnicians.Count);

            //  Находим ближайший склад 
            var nearestWarehouse = warehouses.OrderBy(w =>
                validTechnicians.Min(t => DistanceCalculator.CalculateDistance(t.Latitude, t.Longitude, w.Latitude, w.Longitude))
            ).First();

            // Теперь выбираем техника, который ближе всего к ближайшему складу
            var assignedTechnician = validTechnicians
                .OrderBy(t => DistanceCalculator.CalculateDistance(t.Latitude, t.Longitude, nearestWarehouse.Latitude, nearestWarehouse.Longitude))
                .FirstOrDefault();

            if (assignedTechnician == null)
            {
                _logger.LogWarning("⚠️ Не удалось назначить техника для маршрута через склад.");
                return routes;
            }

            _logger.LogInformation("📦 Техник {TechnicianName} (📍 {Lat}, {Lon}) назначен для маршрута через склады.",
                assignedTechnician.FullName, assignedTechnician.Latitude, assignedTechnician.Longitude);

            //  Добавляем маршрут через склады
            await AddMultiWarehouseRouteAsync(assignedTechnician, warehouses, jobLatitude, jobLongitude, routes);

            //  Остальные техники — прямой маршрут
            var otherTechnicianTasks = validTechnicians
                .Where(t => t.Id != assignedTechnician.Id)
                .Select(t => AddDirectRouteAsync(t, jobLatitude, jobLongitude, routes));

            await Task.WhenAll(otherTechnicianTasks);

            return routes;
        }


        /// <summary>
        /// ✅ Добавляет маршрут техника через несколько складов
        /// </summary>
        private async Task AddMultiWarehouseRouteAsync(TechnicianDTO technician, List<WarehouseDTO> warehouses, double jobLat, double jobLon, List<RouteDTO> routes)
        {
            var routePoints = new List<RoutePoint>();
            double totalDistance = 0;
            double totalDuration = 0;

            double currentLat = technician.Latitude;
            double currentLon = technician.Longitude;

            _logger.LogInformation("📦 Техник {TechnicianName} заедет на {WarehouseCount} складов перед работой.", technician.FullName, warehouses.Count);

            foreach (var warehouse in warehouses)
            {
                var routeToWarehouse = await _routeServiceClient.GetRouteAsync(currentLat, currentLon, warehouse.Latitude, warehouse.Longitude);
                if (routeToWarehouse != null)
                {
                    routePoints.AddRange(routeToWarehouse.RoutePoints);
                    routePoints.Add(new RoutePoint(warehouse.Latitude, warehouse.Longitude, true)); // Точка склада
                    totalDistance += routeToWarehouse.Distance;
                    totalDuration += routeToWarehouse.Duration;
                    currentLat = warehouse.Latitude;
                    currentLon = warehouse.Longitude;
                }
                else
                {
                    _logger.LogWarning("❌ Не удалось построить маршрут к складу {WarehouseId}", warehouse.Id);
                }
            }

            // 📌 Добавляем финальный маршрут от последнего склада до заказа
            var routeToJob = await _routeServiceClient.GetRouteAsync(currentLat, currentLon, jobLat, jobLon);
            if (routeToJob != null)
            {
                routePoints.AddRange(routeToJob.RoutePoints);
                totalDistance += routeToJob.Distance;
                totalDuration += routeToJob.Duration;
            }
            else
            {
                _logger.LogWarning("❌ Не удалось построить маршрут от склада до заказа.");
            }

            routes.Add(new RouteDTO
            {
                TechnicianId = technician.Id,
                TechnicianName = technician.FullName,
                PhoneNumber = technician.PhoneNumber,
                Distance = Math.Round(totalDistance, 2),
                Duration = Math.Round(totalDuration, 2),
                RoutePoints = routePoints,
                IsViaWarehouse = true
            });

            _logger.LogInformation("✅ Оптимизированный маршрут через {WarehouseCount} складов добавлен для {TechnicianName}.", warehouses.Count, technician.FullName);
        }

        /// <summary>
        /// ✅ Добавляет прямой маршрут техника до заказа без складов
        /// </summary>
        private async Task AddDirectRouteAsync(TechnicianDTO technician, double jobLat, double jobLon, List<RouteDTO> routes)
        {
            var directRoute = await _routeServiceClient.GetRouteAsync(technician.Latitude, technician.Longitude, jobLat, jobLon);
            if (directRoute != null)
            {
                routes.Add(new RouteDTO
                {
                    TechnicianId = technician.Id,
                    TechnicianName = technician.FullName,
                    PhoneNumber = technician.PhoneNumber,
                    Distance = directRoute.Distance,
                    Duration = directRoute.Duration,
                    RoutePoints = directRoute.RoutePoints,
                    IsViaWarehouse = false
                });

                _logger.LogInformation("✅ Прямой маршрут для {TechnicianName} добавлен!", technician.FullName);
            }
            else
            {
                _logger.LogWarning("❌ Не удалось построить маршрут для техника {TechnicianName}.", technician.FullName);
            }
        }
    }
}
