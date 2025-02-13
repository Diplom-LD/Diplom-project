namespace ManagerApp.Clients
{
    using System;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;

    public class AuthServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AuthServiceClient> _logger;
        private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        public AuthServiceClient(HttpClient httpClient, IConfiguration configuration, ILogger<AuthServiceClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            var baseUrl = configuration["AuthService:BaseUrl"];
            if (string.IsNullOrEmpty(baseUrl))
            {
                throw new InvalidOperationException("AuthService:BaseUrl is not configured in appsettings.json");
            }

            _httpClient.BaseAddress = new Uri(baseUrl);
            _logger.LogInformation("AuthServiceClient configured with base URL: {BaseUrl}", baseUrl);
        }

        public async Task<HttpResponseMessage> PostAsync(string endpoint, object data)
        {
            if (string.IsNullOrEmpty(endpoint))
            {
                _logger.LogError("PostAsync called with empty endpoint");
                throw new ArgumentException("Endpoint cannot be null or empty", nameof(endpoint));
            }

            _logger.LogInformation("Sending POST request to {Endpoint}", endpoint);

            try
            {
                var jsonContent = new StringContent(JsonSerializer.Serialize(data, _jsonOptions), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(endpoint, jsonContent);

                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("POST request to {Endpoint} failed with status {StatusCode}: {ErrorMessage}", endpoint, response.StatusCode, errorMessage);
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending POST request to {Endpoint}", endpoint);
                throw;
            }
        }

        public async Task<HttpResponseMessage> GetAsync(string endpoint, string? accessToken = null)
        {
            if (string.IsNullOrEmpty(endpoint))
            {
                _logger.LogError("GetAsync called with empty endpoint");
                throw new ArgumentException("Endpoint cannot be null or empty", nameof(endpoint));
            }

            _logger.LogInformation("Sending GET request to {Endpoint}", endpoint);

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, endpoint);

                if (!string.IsNullOrEmpty(accessToken))
                {
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                }

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("GET request to {Endpoint} failed with status {StatusCode}: {ErrorMessage}", endpoint, response.StatusCode, errorMessage);
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending GET request to {Endpoint}", endpoint);
                throw;
            }
        }

        public async Task<HttpResponseMessage> PostWithCookiesAsync(string endpoint, object data, string cookieHeader)
        {
            if (string.IsNullOrEmpty(endpoint))
            {
                _logger.LogError("PostWithCookiesAsync called with empty endpoint");
                throw new ArgumentException("Endpoint cannot be null or empty", nameof(endpoint));
            }

            _logger.LogInformation("Sending POST request with cookies to {Endpoint}", endpoint);

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = new StringContent(JsonSerializer.Serialize(data, _jsonOptions), Encoding.UTF8, "application/json")
                };
                request.Headers.Add("Cookie", cookieHeader);

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("POST request with cookies to {Endpoint} failed with status {StatusCode}: {ErrorMessage}", endpoint, response.StatusCode, errorMessage);
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending POST request with cookies to {Endpoint}", endpoint);
                throw;
            }
        }

    }

}
