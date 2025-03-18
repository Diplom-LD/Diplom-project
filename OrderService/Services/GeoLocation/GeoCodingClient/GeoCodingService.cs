using OrderService.DTO.GeoLocation;
using System.Text.Json;

namespace OrderService.Services.GeoLocation.GeoCodingClient
{
    public class GeoCodingService : IGeoCodingService
    {
        private const string ApiUrl = "https://nominatim.openstreetmap.org/search";
        private static readonly List<string> Languages = ["ro", "ru", "en"];

        private readonly HttpClient _httpClient;
        private readonly ILogger<GeoCodingService> _logger;
        private static readonly SemaphoreSlim Semaphore = new(1, 1);
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        public GeoCodingService(HttpClient httpClient, ILogger<GeoCodingService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("BTU-OrderService/1.0 (contact: support@btu-orders.com)");
            _httpClient.DefaultRequestHeaders.Referrer = new Uri("https://btu-orders.com");
        }

        public async Task<(double Latitude, double Longitude, string DisplayName)?> GetCoordinatesAsync(string address)
        {
            foreach (var lang in Languages)
            {
                var coordinates = await FetchCoordinatesAsync(address, lang);
                if (coordinates != null) return coordinates;
                await Task.Delay(1000);
            }
            return null;
        }

        public async Task<(double Latitude, double Longitude, string DisplayName)?> GetBestCoordinateAsync(string address)
        {
            foreach (var lang in Languages)
            {
                var results = await FetchAllCoordinatesAsync(address, lang);
                if (results.Count > 0)
                {
                    var bestMatch = results.OrderByDescending(r => r.Importance).FirstOrDefault();
                    if (bestMatch != default) return (bestMatch.Latitude, bestMatch.Longitude, bestMatch.DisplayName);
                }
                await Task.Delay(500);
            }
            return null;
        }

        private async Task<(double Latitude, double Longitude, string DisplayName)?> FetchCoordinatesAsync(string address, string language)
        {
            var url = $"{ApiUrl}?q={Uri.EscapeDataString(address)}&format=json&accept-language={language}&countrycodes=MD";
            try
            {
                await Semaphore.WaitAsync();
                var response = await _httpClient.GetStringAsync(url);
                _logger.LogInformation("📍 JSON-ответ от GeoCoding API ({Language}): {Json}", language, response);

                var data = JsonSerializer.Deserialize<List<GeoLocationResponse>>(response, JsonOptions);
                if (data == null || data.Count == 0)
                {
                    _logger.LogWarning("⚠️ Нет данных от GeoCoding API ({Language}) для адреса: {Address}", language, address);
                    return null;
                }

                var bestMatch = data.OrderByDescending(d => d.Importance).FirstOrDefault();
                return bestMatch != null ? (bestMatch.Latitude, bestMatch.Longitude, bestMatch.DisplayName) : null;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "❌ Ошибка запроса к GeoCoding API ({Language}): {Address}", language, address);
                return null;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "❌ Ошибка обработки JSON от GeoCoding API ({Language}): {Address}", language, address);
                return null;
            }
            finally
            {
                Semaphore.Release();
            }
        }

        private async Task<List<(double Latitude, double Longitude, string DisplayName, double Importance)>> FetchAllCoordinatesAsync(string address, string language)
        {
            var url = $"{ApiUrl}?q={Uri.EscapeDataString(address)}&format=json&accept-language={language}&countrycodes=MD";

            try
            {
                await Semaphore.WaitAsync();
                var response = await _httpClient.GetStringAsync(url);
                _logger.LogInformation("📍 JSON-ответ от GeoCoding API ({Language}): {Json}", language, response);

                var data = JsonSerializer.Deserialize<List<GeoLocationResponse>>(response, JsonOptions);
                if (data == null || data.Count == 0)
                {
                    _logger.LogWarning("⚠️ Нет данных от GeoCoding API ({Language}) для адреса: {Address}", language, address);
                    return [];
                }

                return [.. data.Select(d => (d.Latitude, d.Longitude, d.DisplayName ?? "Unknown", d.Importance))];
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "❌ Ошибка запроса к GeoCoding API ({Language}): {Address}", language, address);
                return [];
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "❌ Ошибка обработки JSON от GeoCoding API ({Language}): {Address}", language, address);
                return [];
            }
            finally
            {
                Semaphore.Release();
            }
        }
    }
}
