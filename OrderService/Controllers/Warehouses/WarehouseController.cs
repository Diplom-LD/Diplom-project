using Microsoft.AspNetCore.Mvc;
using OrderService.Models.Warehouses;
using OrderService.Services.Warehouses;

namespace OrderService.Controllers.Warehouses
{
    [ApiController]
    [Route("warehouses")]
    public class WarehouseController(WarehouseService warehouseService, ILogger<WarehouseController> logger) : ControllerBase
    {
        private readonly WarehouseService _warehouseService = warehouseService;
        private readonly ILogger<WarehouseController> _logger = logger;

        [HttpGet]
        public async Task<ActionResult<List<Warehouse>>> GetAll(CancellationToken cancellationToken)
        {
            try
            {
                var warehouses = await _warehouseService.GetAllAsync(cancellationToken);
                return Ok(warehouses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка складов");
                return StatusCode(500, new { message = "Ошибка сервера при получении складов." });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Warehouse>> GetById(string id, CancellationToken cancellationToken)
        {
            try
            {
                var warehouse = await _warehouseService.GetByIdAsync(id, cancellationToken);
                return warehouse is null
                    ? NotFound(new { message = $"Склад с ID {id} не найден." })
                    : Ok(warehouse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении склада по ID: {Id}", id);
                return StatusCode(500, new { message = "Ошибка сервера при получении склада." });
            }
        }

        [HttpPost]
        public async Task<ActionResult<string>> Add([FromBody] Warehouse warehouse, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Невалидные данные склада", errors = ModelState });
            }

            try
            {
                var id = await _warehouseService.AddAsync(warehouse, cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id }, warehouse);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Ошибка валидации при добавлении склада");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при добавлении нового склада");
                return StatusCode(500, new { message = "Ошибка сервера при добавлении склада." });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] Warehouse warehouse, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Невалидные данные для обновления", errors = ModelState });
            }

            try
            {
                var updated = await _warehouseService.UpdateAsync(warehouse, cancellationToken);
                return updated
                    ? NoContent()
                    : NotFound(new { message = $"Склад с ID {id} не найден." });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Ошибка валидации при обновлении склада с ID {Id}", id);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении склада");
                return StatusCode(500, new { message = "Ошибка сервера при обновлении склада." });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
        {
            try
            {
                var deleted = await _warehouseService.DeleteAsync(id, cancellationToken);
                return deleted
                    ? NoContent()
                    : NotFound(new { message = $"Склад с ID {id} не найден." });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Ошибка валидации при удалении склада с ID {Id}", id);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении склада");
                return StatusCode(500, new { message = "Ошибка сервера при удалении склада." });
            }
        }
    }
}
