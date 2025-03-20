using ManagerApp.Clients;
using ManagerApp.Models.Warehouses;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ManagerApp.Controllers.Warehouses
{
    [Route("equipment")]
    [ApiController]
    public class EquipmentController(WarehouseServiceClient warehouseServiceClient, ILogger<EquipmentController> logger) : ControllerBase
    {
        private readonly WarehouseServiceClient _warehouseServiceClient = warehouseServiceClient;
        private readonly ILogger<EquipmentController> _logger = logger;
        private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

        /// <summary>
        /// 📌 Получение списка доступного оборудования со всех складов.
        /// </summary>
        [HttpGet("all-warehouses")]
        public async Task<IActionResult> GetEquipmentFromAllWarehouses()
        {
            try
            {
                string? accessToken = HttpContext.Request.Cookies["accessToken"];

                if (string.IsNullOrEmpty(accessToken))
                {
                    _logger.LogWarning("⚠️ Access token is missing in cookies.");
                    return Unauthorized(new { message = "Access token is missing" });
                }

                _logger.LogInformation("📡 Отправка запроса в WarehouseService с токеном.");

                var equipmentList = await _warehouseServiceClient.GetAllEquipmentFromWarehousesAsync(accessToken);

                if (equipmentList == null || equipmentList.Count == 0)
                {
                    _logger.LogWarning("⚠️ Нет доступного оборудования на складах.");
                    return Ok(new List<AggregatedEquipmentDTO>());
                }

                _logger.LogInformation("✅ Получен список оборудования: {EquipmentData}",
                    JsonSerializer.Serialize(equipmentList, _jsonOptions));

                return Ok(equipmentList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ошибка при получении оборудования со всех складов.");
                return StatusCode(500, new { message = "Ошибка сервера." });
            }
        }
    }
}
