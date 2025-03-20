using ManagerApp.Models.Warehouses;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Net;

namespace ManagerApp.Clients
{
    public class WarehouseServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<WarehouseServiceClient> _logger;
        private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        public WarehouseServiceClient(HttpClient httpClient, ILogger<WarehouseServiceClient> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;

            _httpClient.BaseAddress = new Uri(configuration["WarehouseService:BaseUrl"]
                ?? throw new InvalidOperationException("WarehouseService BaseUrl is missing!"));
        }

        /// <summary>
        /// Устанавливает Bearer-токен перед выполнением запроса.
        /// </summary>
        private void SetAuthorizationHeader(string accessToken)
        {
            if (_httpClient.DefaultRequestHeaders.Authorization == null)  
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }
        }

        /// <summary>
        /// 📌 Получение агрегированного списка оборудования со всех складов.
        /// </summary>
        public async Task<List<AggregatedEquipmentDTO>> GetAllEquipmentFromWarehousesAsync(string accessToken)
        {
            try
            {
                SetAuthorizationHeader(accessToken);
                using var request = new HttpRequestMessage(HttpMethod.Get, "equipment-stock/aggregated");
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));  

                var response = await _httpClient.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("❌ [WarehouseService] Ошибка авторизации (401 Unauthorized).");
                    return [];
                }

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("⚠️ [WarehouseService] Нет доступного оборудования на складах.");
                    return [];
                }

                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<AggregatedEquipmentDTO>>(content, _jsonOptions) ?? [];
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "❌ [WarehouseServiceClient] Ошибка HTTP-запроса: {Message}", ex.Message);
                return [];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [WarehouseServiceClient] Непредвиденная ошибка.");
                return [];
            }
        }
    }
}
