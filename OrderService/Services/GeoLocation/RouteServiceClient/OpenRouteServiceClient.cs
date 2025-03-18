using System.Text.Json;
using System.Text;
using OrderService.DTO.GeoLocation;

namespace OrderService.Services.GeoLocation.RouteServiceClient
{
    public class OpenRouteServiceClient(HttpClient httpClient, ILogger<OpenRouteServiceClient> logger, IConfiguration configuration)
        : IRouteServiceClient
    {
        private readonly HttpClient _httpClient = httpClient;
        private readonly ILogger<OpenRouteServiceClient> _logger = logger;
        private readonly string _apiKey = configuration["OpenRouteService:ApiKey"] ?? throw new ArgumentNullException(nameof(configuration));
        private const string OpenRouteServiceBaseUrl = "https://api.openrouteservice.org/v2/directions/driving-car";
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new() { WriteIndented = true };

        public async Task<RouteDTO?> GetRouteAsync(double startLat, double startLon, double endLat, double endLon)
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
                    Duration = Math.Round((route.Summary.Duration * 1.2) / 60, 2),
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
