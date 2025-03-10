using System.Text.Json;
using OrderService.DTO.GeoLocation;
using OrderService.DTO.Technicians;
using System.Text;
using Microsoft.Extensions.Logging;

namespace OrderService.Services.GeoLocation
{
    public class OptimizedRouteService(HttpClient httpClient, ILogger<OptimizedRouteService> logger, IConfiguration configuration)
    {
        private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        private readonly ILogger<OptimizedRouteService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly string _apiKey = configuration["OpenRouteService:ApiKey"] ?? throw new ArgumentNullException(nameof(configuration));
        private const string _baseUrl = "https://api.openrouteservice.org/v2/directions/driving-car";
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new() { WriteIndented = true };

        public async Task<List<RouteDTO>> BuildOptimizedRouteAsync(double jobLatitude, double jobLongitude, double warehouseLatitude, double warehouseLongitude, List<TechnicianDTO> technicians)
        {
            var routes = new List<RouteDTO>();

            // 1. Определяем техника, который ближе всего к складу
            var nearestTechnician = technicians
                .OrderBy(t => DistanceCalculator.CalculateDistance(t.Latitude, t.Longitude, warehouseLatitude, warehouseLongitude))
                .FirstOrDefault();

            if (nearestTechnician != null)
            {
                _logger.LogInformation("📍 Ближайший техник к складу: {TechnicianName}", nearestTechnician.FullName);

                // 🚀 Строим маршрут техника до склада, а затем на заявку
                var routeToWarehouse = await GetRouteAsync(nearestTechnician.Latitude, nearestTechnician.Longitude, warehouseLatitude, warehouseLongitude);
                if (routeToWarehouse != null)
                {
                    var routeToJob = await GetRouteAsync(warehouseLatitude, warehouseLongitude, jobLatitude, jobLongitude);
                    if (routeToJob != null)
                    {
                        var combinedRoutePoints = routeToWarehouse.RoutePoints
                            .Concat([new(warehouseLatitude, warehouseLongitude, true)]) 
                            .Concat(routeToJob.RoutePoints)
                            .ToList();

                        routes.Add(new RouteDTO
                        {
                            TechnicianId = nearestTechnician.Id,
                            TechnicianName = nearestTechnician.FullName,
                            Distance = Math.Round(routeToWarehouse.Distance + routeToJob.Distance, 2),
                            Duration = Math.Round(routeToWarehouse.Duration + routeToJob.Duration, 2),
                            RoutePoints = combinedRoutePoints,
                            IsViaWarehouse = true
                        });

                        _logger.LogInformation("✅ Оптимизированный маршрут через склад для {TechnicianName} добавлен!", nearestTechnician.FullName);
                    }
                }
            }

            // 2. Остальные техники едут напрямую на заявку
            foreach (var technician in technicians)
            {
                if (technician.Id == nearestTechnician?.Id)
                    continue;

                var directRoute = await GetRouteAsync(technician.Latitude, technician.Longitude, jobLatitude, jobLongitude);
                if (directRoute != null)
                {
                    routes.Add(new RouteDTO
                    {
                        TechnicianId = technician.Id,
                        TechnicianName = technician.FullName,
                        Distance = directRoute.Distance,
                        Duration = directRoute.Duration,
                        RoutePoints = directRoute.RoutePoints,
                        IsViaWarehouse = false
                    });

                    _logger.LogInformation("✅ Прямой маршрут для {TechnicianName} добавлен!", technician.FullName);
                }
            }

            _logger.LogInformation("✅ Обработка маршрутов завершена. Всего найдено {RouteCount} маршрутов.", routes.Count);
            return routes;
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

                var request = new HttpRequestMessage(HttpMethod.Post, _baseUrl)
                {
                    Content = new StringContent(jsonRequest, Encoding.UTF8, "application/json")
                };
                request.Headers.Add("Authorization", $"Bearer {_apiKey}");

                var response = await _httpClient.SendAsync(request);
                var responseString = await response.Content.ReadAsStringAsync();

                _logger.LogDebug("📥 Получен полный ответ OpenRouteService (Status: {StatusCode}): {ResponseJson}",
                    response.StatusCode, responseString);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("❌ Ошибка {StatusCode} в OpenRouteService: {ErrorResponse}", response.StatusCode, responseString);
                    return null;
                }

                var routeData = JsonSerializer.Deserialize<ORSRouteResponse>(responseString, _jsonSerializerOptions);
                if (routeData?.Routes == null || routeData.Routes.Count == 0)
                {
                    _logger.LogWarning("❌ Ошибка! routeData.Routes пуст");
                    return null;
                }

                var route = routeData.Routes[0];
                var distance = Math.Round(route.Summary.Distance / 1000, 2);
                var duration = Math.Round(route.Summary.Duration / 60, 2);

                _logger.LogInformation("✅ Найден маршрут: {Distance} км, {Duration} мин", distance, duration);

                if (string.IsNullOrEmpty(route.EncodedPolyline))
                {
                    _logger.LogWarning("⚠️ Полилиния отсутствует");
                    return null;
                }

                var routePoints = route.GetCoordinates();
                if (routePoints.Count == 0)
                {
                    _logger.LogWarning("⚠️ Декодирование полилинии не вернуло координаты");
                    return null;
                }

                return new RouteDTO
                {
                    Distance = distance,
                    Duration = duration,
                    RoutePoints = routePoints
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ошибка при запросе маршрута");
                return null;
            }
        }
    }
}
