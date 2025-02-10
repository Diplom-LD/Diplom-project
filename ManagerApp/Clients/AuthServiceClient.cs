namespace ManagerApp.Clients
{
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
            var url = $"{_authServiceUrl}{endpoint}";
            _logger.LogInformation("Sending request to {Url}", url);

            var jsonContent = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");

            return await _httpClient.PostAsync(url, jsonContent);
        }
    }
}
