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
        private readonly string _authServiceUrl;
        private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        public AuthServiceClient(HttpClient httpClient, IConfiguration configuration, ILogger<AuthServiceClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _authServiceUrl = configuration["AuthService:BaseUrl"]?.TrimEnd('/') ?? string.Empty;

            if (string.IsNullOrEmpty(_authServiceUrl))
            {
                _logger.LogError("AuthService:BaseUrl is not configured in appsettings.json");
            }
        }

        public async Task<HttpResponseMessage> PostAsync(string endpoint, object data)
        {
            if (string.IsNullOrEmpty(endpoint))
            {
                _logger.LogError("PostAsync called with empty endpoint");
                throw new ArgumentException("Endpoint cannot be null or empty", nameof(endpoint));
            }

            var url = $"{_authServiceUrl}{endpoint}";
            _logger.LogInformation("Sending request to {Url}", url);

            try
            {
                var jsonContent = new StringContent(JsonSerializer.Serialize(data, _jsonOptions), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, jsonContent);

                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Request to {Url} failed with status {StatusCode}: {ErrorMessage}", url, response.StatusCode, errorMessage);
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending request to {Url}", url);
                throw;
            }
        }
    }
}
