using System.Text.Json;
using OrderService.DTO.GeoLocation;
using OrderService.DTO.Users;
using System.Text;
using OrderService.Services.GeoLocation.HTTPClient;

namespace OrderService.Services.GeoLocation
{
    public class OptimizedRouteService(IHttpClient httpClient, ILogger<OptimizedRouteService> logger, IConfiguration configuration) : IOptimizedRouteService
    {
        private readonly IHttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        private readonly ILogger<OptimizedRouteService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly string _apiKey = configuration["OpenRouteService:ApiKey"] ?? throw new ArgumentNullException(nameof(configuration));
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new() { WriteIndented = true };
        private const string OpenRouteServiceBaseUrl = "https://api.openrouteservice.org/v2/directions/driving-car";

        public async Task<List<RouteDTO>> BuildOptimizedRouteAsync(
            double jobLatitude, double jobLongitude,
            WarehouseDTO? primaryWarehouse, WarehouseDTO? secondaryWarehouse,
            List<TechnicianDTO> technicians)
        {
            var routes = new List<RouteDTO>();
            var nearestTechnician = technicians
                .OrderBy(t => DistanceCalculator.CalculateDistance(t.Latitude, t.Longitude, jobLatitude, jobLongitude))
                .FirstOrDefault();

            if (nearestTechnician != null)
            {
                _logger.LogInformation("📍 Ближайший техник к заявке: {TechnicianName}", nearestTechnician.FullName);

                if (primaryWarehouse != null && secondaryWarehouse == null)
                {
                    await AddWarehouseRouteAsync(nearestTechnician, primaryWarehouse, jobLatitude, jobLongitude, routes);
                }
                else if (primaryWarehouse != null && secondaryWarehouse != null)
                {
                    await AddTwoWarehouseRouteAsync(nearestTechnician, primaryWarehouse, secondaryWarehouse, jobLatitude, jobLongitude, routes);
                }
                else
                {
                    await AddDirectRouteAsync(nearestTechnician, jobLatitude, jobLongitude, routes);
                }
            }

            var otherTechnicianTasks = technicians
                .Where(t => t.Id != nearestTechnician?.Id)
                .Select(t => AddDirectRouteAsync(t, jobLatitude, jobLongitude, routes));

            await Task.WhenAll(otherTechnicianTasks);

            _logger.LogInformation("✅ Обработка маршрутов завершена. Всего найдено {RouteCount} маршрутов.", routes.Count);
            return routes;
        }

        private async Task AddWarehouseRouteAsync(TechnicianDTO technician, WarehouseDTO warehouse, double jobLat, double jobLon, List<RouteDTO> routes)
        {
            var routeToWarehouse = await GetRouteAsync(technician.Latitude, technician.Longitude, warehouse.Latitude, warehouse.Longitude);
            var routeToJob = await GetRouteAsync(warehouse.Latitude, warehouse.Longitude, jobLat, jobLon);

            if (routeToWarehouse != null && routeToJob != null)
            {
                var combinedRoutePoints = routeToWarehouse.RoutePoints
                    .Concat([new RoutePoint(warehouse.Latitude, warehouse.Longitude, true)])
                    .Concat(routeToJob.RoutePoints)
                    .ToList();

                routes.Add(new RouteDTO
                {
                    TechnicianId = technician.Id,
                    TechnicianName = technician.FullName,
                    PhoneNumber = technician.PhoneNumber,
                    Distance = Math.Round(routeToWarehouse.Distance + routeToJob.Distance, 2),
                    Duration = Math.Round(routeToWarehouse.Duration + routeToJob.Duration, 2),
                    RoutePoints = combinedRoutePoints,
                    IsViaWarehouse = true
                });

                _logger.LogInformation("✅ Оптимизированный маршрут через склад {WarehouseName} добавлен!", warehouse.Name);
            }
        }

        private async Task AddTwoWarehouseRouteAsync(TechnicianDTO technician, WarehouseDTO warehouse1, WarehouseDTO warehouse2, double jobLat, double jobLon, List<RouteDTO> routes)
        {
            var routeToFirstWarehouse = await GetRouteAsync(technician.Latitude, technician.Longitude, warehouse1.Latitude, warehouse1.Longitude);
            var routeBetweenWarehouses = await GetRouteAsync(warehouse1.Latitude, warehouse1.Longitude, warehouse2.Latitude, warehouse2.Longitude);
            var routeToJob = await GetRouteAsync(warehouse2.Latitude, warehouse2.Longitude, jobLat, jobLon);

            if (routeToFirstWarehouse != null && routeBetweenWarehouses != null && routeToJob != null)
            {
                var combinedRoutePoints = routeToFirstWarehouse.RoutePoints
                    .Concat([new RoutePoint(warehouse1.Latitude, warehouse1.Longitude, true)])
                    .Concat(routeBetweenWarehouses.RoutePoints)
                    .Concat([new RoutePoint(warehouse2.Latitude, warehouse2.Longitude, true)])
                    .Concat(routeToJob.RoutePoints)
                    .ToList();

                routes.Add(new RouteDTO
                {
                    TechnicianId = technician.Id,
                    TechnicianName = technician.FullName,
                    PhoneNumber = technician.PhoneNumber,
                    Distance = Math.Round(routeToFirstWarehouse.Distance + routeBetweenWarehouses.Distance + routeToJob.Distance, 2),
                    Duration = Math.Round(routeToFirstWarehouse.Duration + routeBetweenWarehouses.Duration + routeToJob.Duration, 2),
                    RoutePoints = combinedRoutePoints,
                    IsViaWarehouse = true
                });

                _logger.LogInformation("✅ Оптимизированный маршрут через два склада {Warehouse1Name} и {Warehouse2Name} добавлен!", warehouse1.Name, warehouse2.Name);
            }
        }

        private async Task AddDirectRouteAsync(TechnicianDTO technician, double jobLat, double jobLon, List<RouteDTO> routes)
        {
            var directRoute = await GetRouteAsync(technician.Latitude, technician.Longitude, jobLat, jobLon);
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
        }

        private async Task<RouteDTO?> GetRouteAsync(double startLat, double startLon, double endLat, double endLon)
        {
            try
            {
                var requestBody = new
                {
                    coordinates = new[] { [startLon, startLat], new[] { endLon, endLat } },
                    profile = "driving-car",
                    format = "json",
                    geometry = true,
                    options = new { avoid_features = new[] { "ferries", "tollways" } }
                };

                var jsonRequest = JsonSerializer.Serialize(requestBody, _jsonSerializerOptions);
                _logger.LogDebug("📤 Отправляем запрос в OpenRouteService: {RequestJson}", jsonRequest);

                var request = new HttpRequestMessage(HttpMethod.Post, OpenRouteServiceBaseUrl)
                {
                    Content = new StringContent(jsonRequest, Encoding.UTF8, "application/json")
                };
                request.Headers.Add("Authorization", $"Bearer {_apiKey}");

                var response = await _httpClient.SendAsync(request);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("❌ Ошибка {StatusCode} в OpenRouteService: {ErrorResponse}", response.StatusCode, responseString);
                    return null;
                }

                var routeData = JsonSerializer.Deserialize<ORSRouteResponse>(responseString, _jsonSerializerOptions);
                if (routeData?.Routes == null || routeData.Routes.Count == 0)
                {
                    _logger.LogWarning("⚠️ Данные маршрута отсутствуют.");
                    return null;
                }

                var route = routeData.Routes[0];
                return new RouteDTO
                {
                    Distance = Math.Round(route.Summary.Distance / 1000, 2),
                    Duration = Math.Round(route.Summary.Duration / 60, 2),
                    RoutePoints = route.GetCoordinates()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ошибка при запросе маршрута.");
                return null;
            }
        }
    }
}