using System.Text.Json;
using AuthService.Models.GeoCodingServices;

namespace AuthService.Services
{
    public class GeoCodingService
    {
        private const string ApiUrl = "https://nominatim.openstreetmap.org/search";
        private static readonly List<string> Languages = ["ro", "ru", "en"]; 

        private readonly HttpClient httpClient;
        private const int MaxRequestsPerSecond = 1;
        private static readonly SemaphoreSlim Semaphore = new(MaxRequestsPerSecond, MaxRequestsPerSecond);

        public GeoCodingService(HttpClient httpClient)
        {
            this.httpClient = httpClient;

            // Уникальный User-Agent в соответствии с требованиями Nominatim
            this.httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("BTU-OrderService/1.0 (contact: support@btu-orders.com)");
            this.httpClient.DefaultRequestHeaders.Referrer = new System.Uri("https://btu-orders.com");
        }

        public async Task<(double Latitude, double Longitude, string DisplayName)?> GetCoordinatesAsync(string address)
        {
            foreach (var lang in Languages)
            {
                var coordinates = await FetchCoordinatesAsync(address, lang);
                if (coordinates.HasValue) return coordinates;
            }
            return null;
        }

        //public async Task<List<(double Latitude, double Longitude, string DisplayName)>> GetAllCoordinatesAsync(string address)
        //{
        //    var allResults = new List<(double Latitude, double Longitude, string DisplayName)>();
        //    foreach (var lang in Languages)
        //    {
        //        var results = await FetchAllCoordinatesAsync(address, lang);
        //        if (results.Count > 0) allResults.AddRange(results);
        //    }
        //    return allResults;
        //}

        public async Task<(double Latitude, double Longitude, string DisplayName)?> GetBestCoordinateAsync(string address)
        {
            foreach (var lang in Languages)
            {
                var results = await FetchAllCoordinatesAsync(address, lang);
                if (results.Count > 0) return results.First();
            }
            return null;
        }

        private async Task<(double Latitude, double Longitude, string DisplayName)?> FetchCoordinatesAsync(string address, string language)
        {
            var url = $"{ApiUrl}?q={Uri.EscapeDataString(address)}&format=json&accept-language={language}";

            try
            {
                await Semaphore.WaitAsync();
                var response = await httpClient.GetStringAsync(url);
                var data = JsonSerializer.Deserialize<List<GeoLocationResponse>>(response);

                if (data == null || data.Count == 0) 
                {
                    Console.WriteLine($"⚠️ Нет данных от GeoCoding API ({language}) для адреса: {address}");
                    return null;
                }

                return (data[0].Latitude, data[0].Longitude, data[0].DisplayName);
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"❌ Ошибка запроса к GeoCoding API ({language}): {ex.Message}");
                return null;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"❌ Ошибка обработки JSON от GeoCoding API ({language}): {ex.Message}");
                return null;
            }
            finally
            {
                await Task.Delay(1100); 
                Semaphore.Release();
            }
        }

        private async Task<List<(double Latitude, double Longitude, string DisplayName)>> FetchAllCoordinatesAsync(string address, string language)
        {
            var url = $"{ApiUrl}?q={Uri.EscapeDataString(address)}&format=json&accept-language={language}";

            try
            {
                await Semaphore.WaitAsync();
                var response = await httpClient.GetStringAsync(url);
                var data = JsonSerializer.Deserialize<List<GeoLocationResponse>>(response);

                if (data == null || data.Count == 0)
                {
                    Console.WriteLine($"⚠️ Нет данных от GeoCoding API ({language}) для адреса: {address}");
                    return []; 
                }

                return [.. data.Select(d => (d.Latitude, d.Longitude, d.DisplayName))];
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"❌ Ошибка запроса к GeoCoding API ({language}): {ex.Message}");
                return [];
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"❌ Ошибка обработки JSON от GeoCoding API ({language}): {ex.Message}");
                return []; 
            }
            finally
            {
                await Task.Delay(1100);
                Semaphore.Release();
            }
        }


    }
}
