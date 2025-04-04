using System.Text;
using System.Text.Json;
using ManagerApp.Models.Home;
using ManagerApp.Models.AuthRequest;

namespace ManagerApp.Clients
{
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

        public async Task<List<ClientModel>> GetClientsAsync(string accessToken)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "/auth/account/get-all-clients");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                _logger.LogInformation("Sending request to AuthService: {Url}", request.RequestUri);

                var response = await _httpClient.SendAsync(request);

                _logger.LogInformation("Response received. Status: {StatusCode}", response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Failed to get clients. Status: {StatusCode}, Error: {ErrorMessage}", response.StatusCode, errorMessage);
                    return [];
                }

                var json = await response.Content.ReadAsStringAsync();
                var clients = JsonSerializer.Deserialize<List<ClientModel>>(json, _jsonOptions);

                _logger.LogInformation("Clients received: {Count}", clients?.Count ?? 0);

                return clients ?? [];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching clients from AuthService");
                return [];
            }
        }

        public async Task<UserProfile?> GetProfileAsync(string loginOrEmail, string accessToken)
        {
            _logger.LogInformation("Fetching profile for {LoginOrEmail}", loginOrEmail);
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"/auth/account/get-profile/{loginOrEmail}");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<UserProfile>(_jsonOptions);
                }

                var errorMessage = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("GetProfileAsync failed: {StatusCode} - {ErrorMessage}", response.StatusCode, errorMessage);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching profile from AuthService.");
                return null;
            }
        }

        public async Task<UserProfile?> GetMyProfileAsync(string accessToken)
        {
            _logger.LogInformation("Fetching current manager profile.");
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "/auth/account/my-profile");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<UserProfile>(_jsonOptions);
                }

                var errorMessage = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("GetMyProfileAsync failed: {StatusCode} - {ErrorMessage}", response.StatusCode, errorMessage);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching current manager profile.");
                return null;
            }
        }

        public async Task<bool> UpdateProfileAsync(UpdateProfileRequest request, string accessToken)
        {
            _logger.LogInformation("Updating user profile for: {Name}", request.FirstName);

            try
            {
                var httpRequest = new HttpRequestMessage(HttpMethod.Put, "/auth/account/update-profile")
                {
                    Content = new StringContent(JsonSerializer.Serialize(request, _jsonOptions), Encoding.UTF8, "application/json")
                };
                httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var response = await _httpClient.SendAsync(httpRequest);

                if (!response.IsSuccessStatusCode)
                {
                    var errorMsg = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("UpdateProfileAsync failed: {StatusCode} - {Error}", response.StatusCode, errorMsg);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in UpdateProfileAsync");
                return false;
            }
        }

    }
}

