using ManagerApp.Clients;
using ManagerApp.DTO.Warehouses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ManagerApp.Controllers.Warehouses
{
    [Authorize]
    public class WarehousesController(
        WarehouseServiceClient warehouseClient,
        ILogger<WarehousesController> logger) : Controller
    {
        private readonly WarehouseServiceClient _warehouseClient = warehouseClient;
        private readonly ILogger<WarehousesController> _logger = logger;

        [HttpGet]
        public IActionResult Warehouse() => View();

        [HttpGet]
        public async Task<IActionResult> WarehouseDetails(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || !Guid.TryParse(id, out Guid warehouseId))
            {
                return BadRequest("Некорректный ID склада.");
            }

            var token = GetAccessToken();
            var warehouse = await _warehouseClient.GetByIdAsync(id, token);

            if (warehouse == null)
            {
                return NotFound("Склад не найден.");
            }

            ViewBag.WarehouseId = warehouseId;
            ViewBag.WarehouseName = warehouse.Name;

            return View("WarehouseDetails");
        }


        [HttpGet]
        public async Task<IActionResult> GetAllWarehouses()
        {
            var token = Request.Cookies["accessToken"];
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("Access token missing in cookie");
                return Unauthorized(new { message = "Access token is missing in cookies." });
            }

            var warehouses = await _warehouseClient.GetAllAsync(token);
            return Json(warehouses);
        }

        [HttpPost]
        public async Task<IActionResult> AddWarehouse([FromBody] WarehouseDTO warehouse)
        {
            return await ForwardToService(() => _warehouseClient.AddAsync(warehouse, GetAccessToken()), "добавлении склада");
        }

        [HttpPut]
        public async Task<IActionResult> UpdateWarehouse(string id, [FromBody] WarehouseDTO warehouse)
        {
            if (string.IsNullOrWhiteSpace(id) || id == Guid.Empty.ToString())
            {
                return BadRequest(new { message = "Некорректный ID склада для обновления." });
            }

            if (!Guid.TryParse(id, out Guid warehouseId))
            {
                return BadRequest(new { message = "Некорректный формат ID склада." });
            }

            warehouse.Id = warehouseId;

            return await ForwardToService(() => _warehouseClient.UpdateAsync(id, warehouse, GetAccessToken()), "обновлении склада");
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteWarehouse(string id)
        {
            return await ForwardToService(() => _warehouseClient.DeleteAsync(id, GetAccessToken()), "удалении склада");
        }

        // ------------------ ОБОРУДОВАНИЕ ------------------

        [HttpPost]
        public async Task<IActionResult> AddEquipment([FromBody] EquipmentStockDTO equipment)
        {
            return await ForwardToService(() => _warehouseClient.AddEquipmentAsync(equipment, GetAccessToken()), "добавлении оборудования");
        }

        [HttpPut]
        public async Task<IActionResult> UpdateEquipment(string id, [FromBody] EquipmentStockDTO equipment)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest(new { message = "Некорректный ID оборудования." });
            equipment.Id = id;
            return await ForwardToService(() => _warehouseClient.UpdateEquipmentAsync(id, equipment, GetAccessToken()), "обновлении оборудования");
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteEquipment(string id)
        {
            return await ForwardToService(() => _warehouseClient.DeleteEquipmentAsync(id, GetAccessToken()), "удалении оборудования");
        }

        // ------------------ МАТЕРИАЛЫ ------------------

        [HttpPost]
        public async Task<IActionResult> AddMaterial([FromBody] MaterialsStockDTO material)
        {
            return await ForwardToService(() => _warehouseClient.AddMaterialAsync(material, GetAccessToken()), "добавлении материала");
        }

        [HttpPut]
        public async Task<IActionResult> UpdateMaterial(string id, [FromBody] MaterialsStockDTO material)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest(new { message = "Некорректный ID материала." });
            material.Id = id;
            return await ForwardToService(() => _warehouseClient.UpdateMaterialAsync(id, material, GetAccessToken()), "обновлении материала");
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteMaterial(string id)
        {
            return await ForwardToService(() => _warehouseClient.DeleteMaterialAsync(id, GetAccessToken()), "удалении материала");
        }

        // ------------------ ИНСТРУМЕНТЫ ------------------

        [HttpPost]
        public async Task<IActionResult> AddTool([FromBody] ToolsStockDTO tool)
        {
            return await ForwardToService(() => _warehouseClient.AddToolAsync(tool, GetAccessToken()), "добавлении инструмента");
        }

        [HttpPut]
        public async Task<IActionResult> UpdateTool(string id, [FromBody] ToolsStockDTO tool)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest(new { message = "Некорректный ID инструмента." });
            tool.Id = id;
            return await ForwardToService(() => _warehouseClient.UpdateToolAsync(id, tool, GetAccessToken()), "обновлении инструмента");
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteTool(string id)
        {
            return await ForwardToService(() => _warehouseClient.DeleteToolAsync(id, GetAccessToken()), "удалении инструмента");
        }


        [HttpGet]
        public async Task<IActionResult> GetEquipment(Guid warehouseId)
        {
            var all = await _warehouseClient.GetAllEquipmentAsync(GetAccessToken());
            return Json(all.Where(e => e.WarehouseId == warehouseId.ToString()));
        }

        [HttpGet]
        public async Task<IActionResult> GetMaterials(Guid warehouseId)
        {
            var all = await _warehouseClient.GetAllMaterialsAsync(GetAccessToken());
            return Json(all.Where(m => m.WarehouseId == warehouseId.ToString()));
        }

        [HttpGet]
        public async Task<IActionResult> GetTools(Guid warehouseId)
        {
            var all = await _warehouseClient.GetAllToolsAsync(GetAccessToken());
            return Json(all.Where(t => t.WarehouseId == warehouseId.ToString()));
        }

        private string GetAccessToken()
        {
            var token = Request.Cookies["accessToken"];
            if (string.IsNullOrEmpty(token))
                throw new UnauthorizedAccessException("Access token is missing in cookies.");
            return token;
        }

        private async Task<IActionResult> ForwardToService(
            Func<Task<HttpResponseMessage>> serviceCall,
            string actionContext)
        {
            try
            {
                var response = await serviceCall();
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                    return Ok();

                _logger.LogWarning("❌ Ошибка при {Action}. Status: {StatusCode}, Body: {Body}",
                    actionContext, response.StatusCode, content);

                try
                {
                    using var doc = JsonDocument.Parse(content);
                    return StatusCode((int)response.StatusCode, doc.RootElement.Clone());
                }
                catch (JsonException)
                {
                    return StatusCode((int)response.StatusCode, new { message = content });
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("❌ Unauthorized: {Message}", ex.Message);
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Необработанная ошибка при {Action}", actionContext);
                return StatusCode(500, new { message = "Внутренняя ошибка сервера." });
            }
        }
    }
}
