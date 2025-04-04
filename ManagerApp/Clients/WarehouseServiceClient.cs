using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using ManagerApp.DTO.Warehouses;

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

        private void SetAuthorizationHeader(string accessToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Access token is missing.", nameof(accessToken));

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        public async Task<List<AggregatedEquipmentDTO>> GetAllEquipmentFromWarehousesAsync(string accessToken)
        {
            try
            {
                SetAuthorizationHeader(accessToken);
                using var request = new HttpRequestMessage(HttpMethod.Get, "equipment-stock/aggregated");
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    _logger.LogWarning("⚠️ [GetAllEquipment] Status: {StatusCode}, Body: {Content}", response.StatusCode, content);

                return JsonSerializer.Deserialize<List<AggregatedEquipmentDTO>>(content, _jsonOptions) ?? [];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ошибка при получении оборудования");
                return [];
            }
        }

        public async Task<List<WarehouseDTO>> GetAllAsync(string accessToken)
        {
            try
            {
                SetAuthorizationHeader(accessToken);
                var response = await _httpClient.GetAsync("warehouses");
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    _logger.LogWarning("⚠️ [GetAll] Status: {StatusCode}, Body: {Content}", response.StatusCode, content);

                return JsonSerializer.Deserialize<List<WarehouseDTO>>(content, _jsonOptions) ?? [];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ошибка при получении всех складов");
                return [];
            }
        }

        public async Task<WarehouseDTO?> GetByIdAsync(string id, string accessToken)
        {
            try
            {
                SetAuthorizationHeader(accessToken);
                var response = await _httpClient.GetAsync($"warehouses/{id}");
                var content = await response.Content.ReadAsStringAsync();

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("⚠️ Склад с ID {Id} не найден. Response: {Content}", id, content);
                    return null;
                }

                if (!response.IsSuccessStatusCode)
                    _logger.LogWarning("⚠️ [GetById] Status: {StatusCode}, Body: {Content}", response.StatusCode, content);

                return JsonSerializer.Deserialize<WarehouseDTO>(content, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ошибка при получении склада по ID");
                return null;
            }
        }

        public async Task<HttpResponseMessage> AddAsync(WarehouseDTO warehouse, string accessToken)
        {
            SetAuthorizationHeader(accessToken);
            var jsonContent = new StringContent(JsonSerializer.Serialize(warehouse), System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("warehouses", jsonContent);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                _logger.LogWarning("⚠️ [Add] Status: {StatusCode}, Body: {Content}", response.StatusCode, content);

            return response;
        }

        public async Task<HttpResponseMessage> UpdateAsync(string id, WarehouseDTO warehouse, string accessToken)
        {
            SetAuthorizationHeader(accessToken);
            var jsonContent = new StringContent(JsonSerializer.Serialize(warehouse), System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"warehouses/{id}", jsonContent);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                _logger.LogWarning("⚠️ [Update] Status: {StatusCode}, Body: {Content}", response.StatusCode, content);

            return response;
        }

        public async Task<HttpResponseMessage> DeleteAsync(string id, string accessToken)
        {
            SetAuthorizationHeader(accessToken);
            var response = await _httpClient.DeleteAsync($"warehouses/{id}");
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                _logger.LogWarning("⚠️ [Delete] Status: {StatusCode}, Body: {Content}", response.StatusCode, content);

            return response;
        }

        // ---------- ОБОРУДОВАНИЕ ----------
        public async Task<List<EquipmentStockDTO>> GetAllEquipmentAsync(string accessToken)
        {
            SetAuthorizationHeader(accessToken);
            var response = await _httpClient.GetAsync("equipment-stock");
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                _logger.LogWarning("⚠️ [GetAllEquipment] Status: {StatusCode}, Body: {Content}", response.StatusCode, content);

            return JsonSerializer.Deserialize<List<EquipmentStockDTO>>(content, _jsonOptions) ?? [];
        }

        public async Task<HttpResponseMessage> AddEquipmentAsync(EquipmentStockDTO equipment, string accessToken)
        {
            SetAuthorizationHeader(accessToken);
            var json = new StringContent(JsonSerializer.Serialize(equipment), System.Text.Encoding.UTF8, "application/json");
            return await _httpClient.PostAsync("equipment-stock", json);
        }

        public async Task<HttpResponseMessage> UpdateEquipmentAsync(string id, EquipmentStockDTO equipment, string accessToken)
        {
            SetAuthorizationHeader(accessToken);
            var json = new StringContent(JsonSerializer.Serialize(equipment), System.Text.Encoding.UTF8, "application/json");
            return await _httpClient.PutAsync($"equipment-stock/{id}", json);
        }

        public async Task<HttpResponseMessage> DeleteEquipmentAsync(string id, string accessToken)
        {
            SetAuthorizationHeader(accessToken);
            return await _httpClient.DeleteAsync($"equipment-stock/{id}");
        }

        // ---------- МАТЕРИАЛЫ ----------
        public async Task<List<MaterialsStockDTO>> GetAllMaterialsAsync(string accessToken)
        {
            SetAuthorizationHeader(accessToken);
            var response = await _httpClient.GetAsync("materials-stock");
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                _logger.LogWarning("⚠️ [GetAllMaterials] Status: {StatusCode}, Body: {Content}", response.StatusCode, content);

            return JsonSerializer.Deserialize<List<MaterialsStockDTO>>(content, _jsonOptions) ?? [];
        }

        public async Task<HttpResponseMessage> AddMaterialAsync(MaterialsStockDTO material, string accessToken)
        {
            SetAuthorizationHeader(accessToken);
            var json = new StringContent(JsonSerializer.Serialize(material), System.Text.Encoding.UTF8, "application/json");
            return await _httpClient.PostAsync("materials-stock", json);
        }

        public async Task<HttpResponseMessage> UpdateMaterialAsync(string id, MaterialsStockDTO material, string accessToken)
        {
            SetAuthorizationHeader(accessToken);
            var json = new StringContent(JsonSerializer.Serialize(material), System.Text.Encoding.UTF8, "application/json");
            return await _httpClient.PutAsync($"materials-stock/{id}", json);
        }

        public async Task<HttpResponseMessage> DeleteMaterialAsync(string id, string accessToken)
        {
            SetAuthorizationHeader(accessToken);
            return await _httpClient.DeleteAsync($"materials-stock/{id}");
        }

        // ---------- ИНСТРУМЕНТЫ ----------
        public async Task<List<ToolsStockDTO>> GetAllToolsAsync(string accessToken)
        {
            SetAuthorizationHeader(accessToken);
            var response = await _httpClient.GetAsync("tools-stock");
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                _logger.LogWarning("⚠️ [GetAllTools] Status: {StatusCode}, Body: {Content}", response.StatusCode, content);

            return JsonSerializer.Deserialize<List<ToolsStockDTO>>(content, _jsonOptions) ?? [];
        }

        public async Task<HttpResponseMessage> AddToolAsync(ToolsStockDTO tool, string accessToken)
        {
            SetAuthorizationHeader(accessToken);
            var json = new StringContent(JsonSerializer.Serialize(tool), System.Text.Encoding.UTF8, "application/json");
            return await _httpClient.PostAsync("tools-stock", json);
        }

        public async Task<HttpResponseMessage> UpdateToolAsync(string id, ToolsStockDTO tool, string accessToken)
        {
            SetAuthorizationHeader(accessToken);
            var json = new StringContent(JsonSerializer.Serialize(tool), System.Text.Encoding.UTF8, "application/json");
            return await _httpClient.PutAsync($"tools-stock/{id}", json);
        }

        public async Task<HttpResponseMessage> DeleteToolAsync(string id, string accessToken)
        {
            SetAuthorizationHeader(accessToken);
            return await _httpClient.DeleteAsync($"tools-stock/{id}");
        }

    }
}
