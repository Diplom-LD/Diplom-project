using ManagerApp.Models.Orders;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using System.Net;

namespace ManagerApp.Clients
{
    public class OrderServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OrderServiceClient> _logger;
        private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        public OrderServiceClient(HttpClient httpClient, ILogger<OrderServiceClient> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;

            // Устанавливаем базовый URL из конфигурации
            _httpClient.BaseAddress = new Uri(configuration["OrderService:BaseUrl"] ?? throw new InvalidOperationException("OrderService BaseUrl is missing!"));
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        /// <summary>
        /// Устанавливает Bearer-токен перед выполнением запроса.
        /// </summary>
        private void SetAuthorizationHeader(string accessToken)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        /// <summary>
        /// Получение списка всех заявок.
        /// </summary>
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


        /// <summary>
        /// Получение заявки по ID.
        /// </summary>
        public async Task<OrderResponse?> GetOrderByIdAsync(Guid orderId, string accessToken)
        {
            try
            {
                SetAuthorizationHeader(accessToken);
                var response = await _httpClient.GetAsync($"orders/{orderId}");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("⚠️ [OrderService] Ошибка получения заявки {OrderId}: {StatusCode}", orderId, response.StatusCode);
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<OrderResponse>(content, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [OrderServiceClient] Ошибка при получении заявки {OrderId}.", orderId);
                return null;
            }
        }

        /// <summary>
        /// Создание новой заявки.
        /// </summary>
        public async Task<CreatedOrderResponseDTO?> CreateOrderAsync(OrderRequest newOrder, string accessToken)
        {
            try
            {
                SetAuthorizationHeader(accessToken);
                var jsonContent = JsonSerializer.Serialize(newOrder);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("orders/create", content);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("⚠️ [OrderService] Ошибка при создании заявки: {StatusCode}", response.StatusCode);
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
    }
}
