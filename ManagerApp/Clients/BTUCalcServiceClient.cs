using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ManagerApp.Models.BTU;
using System.Net;

namespace ManagerApp.Clients
{
    public class BTUCalcServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<BTUCalcServiceClient> _logger;
        private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
        private readonly string _apiUrl;

        public BTUCalcServiceClient(HttpClient httpClient, IConfiguration configuration, ILogger<BTUCalcServiceClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            _apiUrl = configuration["BTUCalcService:BaseUrl"]
                ?? throw new InvalidOperationException("BTUCalcService:BaseUrl is not configured in appsettings.json");

            _httpClient.BaseAddress = new Uri(_apiUrl);
            _logger.LogInformation("BTUCalcServiceClient configured with base URL: {BaseUrl}", _apiUrl);
        }

        public async Task<string?> CalculateBTUAsync(BTURequestModel model)
        {
            var endpoint = "/BTUCalcService/calculate_btu";
            _logger.LogInformation("Sending BTU calculation request to {Endpoint}", endpoint);

            var jsonContent = JsonSerializer.Serialize(model);
            _logger.LogInformation("Request Payload: {JsonContent}", jsonContent);

            try
            {
                var response = await _httpClient.PostAsJsonAsync(endpoint, model);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("BTU calculation failed. Status: {StatusCode}, Response: {Error}", response.StatusCode, errorContent);
                    return null;
                }

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BTU calculation request failed due to an exception.");
                return null;
            }
        }

        public async Task<string?> GetProductsByBTURangeAsync(int btuMin, int btuMax)
        {
            var endpoint = $"/BTUCalcService/products/range/?btu_min={btuMin}&btu_max={btuMax}";
            _logger.LogInformation("Fetching products in BTU range: {Min} - {Max}", btuMin, btuMax);

            try
            {
                var response = await _httpClient.GetAsync(endpoint);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to fetch products by BTU range, status: {StatusCode}", response.StatusCode);
                    return null;
                }

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching products by BTU range.");
                return null;
            }
        }

        public async Task<string?> GetProductsByExactBTUAsync(int btu)
        {
            var endpoint = $"/BTUCalcService/products/btu/{btu}";
            _logger.LogInformation("Fetching products with exact BTU: {BTU}", btu);

            try
            {
                var response = await _httpClient.GetAsync(endpoint);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to fetch products by exact BTU, status: {StatusCode}", response.StatusCode);
                    return null;
                }

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching products by exact BTU.");
                return null;
            }
        }

        public async Task<string?> GetExtremeBTUProductsAsync()
        {
            var endpoint = "/BTUCalcService/products/extremes/";
            _logger.LogInformation("Fetching products with extreme BTU values");

            try
            {
                var response = await _httpClient.GetAsync(endpoint).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    _logger.LogWarning("Failed to fetch products with extreme BTU values, status: {StatusCode}, Response: {Error}",
                        response.StatusCode, errorMessage);

                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        return "{\"error\":\"Extreme BTU products not found\"}";
                    }

                    return null;
                }

                return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "Network error while fetching extreme BTU products.");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching extreme BTU products.");
                return null;
            }
        }

        public async Task<string?> GetStoresAsync()
        {
            var endpoint = "/BTUCalcService/products/stores/";
            _logger.LogInformation("Fetching list of stores");

            try
            {
                var response = await _httpClient.GetAsync(endpoint);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to fetch stores, status: {StatusCode}", response.StatusCode);
                    return null;
                }

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching stores.");
                return null;
            }
        }

        public async Task<string?> GetProductsByStoreAsync(string storeName)
        {
            var endpoint = $"/BTUCalcService/products/store/{storeName}";
            _logger.LogInformation("Fetching products from store: {StoreName}", storeName);

            try
            {
                var response = await _httpClient.GetAsync(endpoint);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to fetch products by store, status: {StatusCode}", response.StatusCode);
                    return null;
                }

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching products by store.");
                return null;
            }
        }

        public async Task<string?> GetProductsByServiceAreaAsync(int area)
        {
            var endpoint = $"/BTUCalcService/products/service_area/{area}";
            _logger.LogInformation("Fetching products for service area: {Area}", area);

            try
            {
                var response = await _httpClient.GetAsync(endpoint);

                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Failed to fetch products by service area, status: {StatusCode}, error: {Error}",
                        response.StatusCode, errorMessage);

                    if (response.StatusCode == HttpStatusCode.NotFound)
                        return "[]";  

                    return null; 
                }

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching products by service area.");
                return null;
            }
        }

        public async Task<string?> GetProductsByExactPriceAsync(int price)
        {
            var endpoint = $"/BTUCalcService/products/price/{price}";
            _logger.LogInformation("Fetching products with exact price: {Price}", price);

            try
            {
                var response = await _httpClient.GetAsync(endpoint);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to fetch products by exact price, status: {StatusCode}", response.StatusCode);
                    return null;
                }

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching products by exact price.");
                return null;
            }
        }

        public async Task<string?> GetProductsByPriceRangeAsync(int priceMin, int priceMax)
        {
            var endpoint = $"/BTUCalcService/products/price/?price_min={priceMin}&price_max={priceMax}";
            _logger.LogInformation("Fetching products in price range: {Min} - {Max}", priceMin, priceMax);

            try
            {
                var response = await _httpClient.GetAsync(endpoint);

                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Failed to fetch products by price range, status: {StatusCode}, error: {Error}",
                        response.StatusCode, errorMessage);

                    if (response.StatusCode == HttpStatusCode.NotFound)
                        return "[]"; 

                    return null; 
                }

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching products by price range.");
                return null;
            }
        }


    }
}
