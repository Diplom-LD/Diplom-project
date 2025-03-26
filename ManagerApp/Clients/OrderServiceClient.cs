using ManagerApp.Models.Orders;
using ManagerApp.DTO.Technicians;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using System.Net;
using ManagerApp.DTO.Orders;
using System.Text.Json.Serialization;

namespace ManagerApp.Clients
{
    public class OrderServiceClient : IOrderServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OrderServiceClient> _logger;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };


        public OrderServiceClient(HttpClient httpClient, ILogger<OrderServiceClient> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;

            _httpClient.BaseAddress = new Uri(configuration["OrderService:BaseUrl"] ?? throw new InvalidOperationException("OrderService BaseUrl is missing!"));
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        private void SetAuthorizationHeader(string accessToken)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        public async Task<List<OrderResponse>> GetAllOrdersAsync(string accessToken)
        {
            try
            {
                SetAuthorizationHeader(accessToken);
                var response = await _httpClient.GetAsync("manager/orders/get/all");

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("⚠️ [OrderService] Нет доступных заявок. Возвращаем пустой список.");
                    return [];
                }

                var content = await response.Content.ReadAsStringAsync();

                if (content.Contains("\"message\""))
                {
                    _logger.LogWarning("⚠️ [OrderService] Получен ответ с сообщением: {Message}", content);
                    return [];
                }

                return JsonSerializer.Deserialize<List<OrderResponse>>(content, _jsonOptions) ?? [];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [OrderServiceClient] Ошибка при запросе заявок.");
                return [];
            }
        }

        public async Task<List<TechnicianDTO>> GetAvailableTechniciansTodayAsync(string accessToken)
        {
            try
            {
                SetAuthorizationHeader(accessToken);

                var response = await _httpClient.GetAsync("technicians/availability/today");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("⚠️ [OrderService] Ошибка получения доступных техников: {StatusCode}", response.StatusCode);
                    return [];
                }

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<TechnicianDTO>>(content, _jsonOptions) ?? [];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [OrderServiceClient] Ошибка при получении доступных техников.");
                return [];
            }
        }

        public async Task<CreatedOrderResponseDTO?> CreateOrderAsync(OrderRequest newOrder, string accessToken)
        {
            try
            {
                SetAuthorizationHeader(accessToken);

                var jsonContent = JsonSerializer.Serialize(newOrder);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("manager/orders/create", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("⚠️ [OrderService] Ошибка при создании заявки: {StatusCode}, Body: {Body}", response.StatusCode, errorBody);
                    return null;
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<CreatedOrderResponseDTO>(responseBody, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [OrderServiceClient] Ошибка при создании заявки.");
                return null;
            }
        }

        public async Task<OrderResponse?> GetOrderByIdAsync(Guid orderId, string accessToken)
        {
            try
            {
                SetAuthorizationHeader(accessToken);

                var response = await _httpClient.GetAsync($"manager/orders/get/{orderId}");

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("⚠️ [OrderService] Ошибка при получении заявки {OrderId}: {StatusCode}, Body: {Body}",
                        orderId, response.StatusCode, error);
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("📦 [OrderService] Получен JSON для заявки {OrderId}:\n{JsonContent}", orderId, content);

                return JsonSerializer.Deserialize<OrderResponse>(content, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [OrderServiceClient] Исключение при получении заявки {OrderId}.", orderId);
                return null;
            }
        }

        public async Task<bool> UpdateOrderFieldsAsync(OrderUpdateRequestDTO dto, string accessToken)
        {
            try
            {
                SetAuthorizationHeader(accessToken);

                var json = JsonSerializer.Serialize(dto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"manager/orders/update/{dto.OrderId}", content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("⚠️ [OrderService] Не удалось обновить заявку {OrderId}: {StatusCode}, Body: {Body}", dto.OrderId, response.StatusCode, error);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ошибка при обновлении заявки {OrderId}.", dto.OrderId);
                return false;
            }
        }

        public async Task<bool> UpdateOrderStatusAsync(Guid orderId, FulfillmentStatus newStatus, string accessToken)
        {
            try
            {
                SetAuthorizationHeader(accessToken);

                var payload = new
                {
                    OrderId = orderId,
                    NewStatus = newStatus
                };

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync("manager/orders/update-status", content);


                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("⚠️ [OrderService] Не удалось обновить статус заявки {OrderId}: {StatusCode}, Body: {Body}", orderId, response.StatusCode, error);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [OrderServiceClient] Ошибка при обновлении статуса заявки {OrderId}.", orderId);
                return false;
            }
        }


        public async Task<bool> CheckTechniciansArrivalAsync(Guid orderId, string accessToken)
        {
            try
            {
                SetAuthorizationHeader(accessToken);

                var response = await _httpClient.GetAsync($"technicians/orders/{orderId}/check-arrival");

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("⚠️ [OrderService] Ошибка при проверке прибытия техников по заявке {OrderId}: {StatusCode}, {Body}", orderId, response.StatusCode, error);
                    return false;
                }

                var result = await response.Content.ReadAsStringAsync();
                return result.Contains("Все техники прибыли");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ошибка при проверке прибытия техников для заявки {OrderId}", orderId);
                return false;
            }
        }

    }
}
