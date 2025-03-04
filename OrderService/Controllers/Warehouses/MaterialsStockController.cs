using Microsoft.AspNetCore.Mvc;
using OrderService.Models.Warehouses;
using OrderService.Services.Warehouses;
using Microsoft.Extensions.Logging;

namespace OrderService.Controllers.Warehouses
{
    [ApiController]
    [Route("materials")]
    public class MaterialsStockController(IMaterialsStockService materialsStockService, ILogger<MaterialsStockController> logger) : ControllerBase
    {
        private readonly IMaterialsStockService _materialsStockService = materialsStockService;
        private readonly ILogger<MaterialsStockController> _logger = logger;

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var materials = await _materialsStockService.GetAllAsync();
                return Ok(materials);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка материалов.");
                return StatusCode(500, "Внутренняя ошибка сервера.");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            try
            {
                var material = await _materialsStockService.GetByIdAsync(id);
                if (material == null) return NotFound($"Материал с ID {id} не найден.");
                return Ok(material);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении материала с ID {Id}.", id);
                return StatusCode(500, "Внутренняя ошибка сервера.");
            }
        }

        [HttpPost("add")]
        public async Task<IActionResult> Add([FromBody] MaterialsStock material)
        {
            if (material == null || !ModelState.IsValid)
                return BadRequest("Некорректные данные для добавления материала.");

            try
            {
                await _materialsStockService.AddAsync(material);
                return CreatedAtAction(nameof(GetById), new { id = material.ID }, material);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при добавлении материала.");
                return StatusCode(500, "Ошибка при добавлении материала.");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] MaterialsStock material)
        {
            if (id != material.ID) return BadRequest("ID в параметре и объекте не совпадают.");
            if (material == null || !ModelState.IsValid)
                return BadRequest("Некорректные данные для обновления материала.");

            try
            {
                var updated = await _materialsStockService.UpdateAsync(material);
                if (!updated) return NotFound($"Материал с ID {id} не найден.");
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении материала с ID {Id}.", id);
                return StatusCode(500, "Ошибка при обновлении материала.");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var deleted = await _materialsStockService.DeleteAsync(id);
                if (!deleted) return NotFound($"Материал с ID {id} не найден.");
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении материала с ID {Id}.", id);
                return StatusCode(500, "Ошибка при удалении материала.");
            }
        }
    }
}
