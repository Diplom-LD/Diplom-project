using Microsoft.AspNetCore.Mvc;
using OrderService.Models.Warehouses;
using OrderService.Services.Warehouses;
using Microsoft.Extensions.Logging;

namespace OrderService.Controllers.Warehouses
{
    [ApiController]
    [Route("warehouses")]
    public class WarehousesController(
        IWarehouseService warehouseService,
        ILogger<WarehousesController> logger) : ControllerBase
    {
        private readonly IWarehouseService _warehouseService = warehouseService;
        private readonly ILogger<WarehousesController> _logger = logger;

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var warehouses = await _warehouseService.GetAllAsync();
                return Ok(warehouses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка складов.");
                return StatusCode(500, "Ошибка при получении данных.");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            try
            {
                var warehouse = await _warehouseService.GetByIdAsync(id);
                if (warehouse == null) return NotFound($"Склад с ID {id} не найден.");
                return Ok(warehouse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении склада с ID {Id}.", id);
                return StatusCode(500, "Ошибка при получении данных.");
            }
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] Warehouse warehouse)
        {
            if (warehouse == null || !ModelState.IsValid)
                return BadRequest("Некорректные данные для создания склада.");

            try
            {
                await _warehouseService.CreateAsync(warehouse);
                return CreatedAtAction(nameof(GetById), new { id = warehouse.ID }, warehouse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании склада.");
                return StatusCode(500, "Ошибка при создании склада.");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] Warehouse warehouse)
        {
            if (id != warehouse.ID) return BadRequest("ID в параметре и объекте не совпадают.");
            if (warehouse == null || !ModelState.IsValid)
                return BadRequest("Некорректные данные для обновления склада.");

            try
            {
                var updated = await _warehouseService.UpdateAsync(warehouse);
                if (!updated) return NotFound($"Склад с ID {id} не найден.");
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении склада с ID {Id}.", id);
                return StatusCode(500, "Ошибка при обновлении склада.");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var deleted = await _warehouseService.DeleteAsync(id);
                if (!deleted) return NotFound($"Склад с ID {id} не найден.");
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении склада с ID {Id}.", id);
                return StatusCode(500, "Ошибка при удалении склада.");
            }
        }
    }
}
