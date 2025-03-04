using Microsoft.AspNetCore.Mvc;
using OrderService.Models.Warehouses;
using OrderService.Services.Warehouses;
using Microsoft.Extensions.Logging;

namespace OrderService.Controllers.Warehouses
{
    [ApiController]
    [Route("tools")]
    public class ToolsStockController(IToolsStockService toolsStockService, ILogger<ToolsStockController> logger) : ControllerBase
    {
        private readonly IToolsStockService _toolsStockService = toolsStockService;
        private readonly ILogger<ToolsStockController> _logger = logger;

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var tools = await _toolsStockService.GetAllAsync();
                return Ok(tools);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка инструментов.");
                return StatusCode(500, "Внутренняя ошибка сервера.");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            try
            {
                var tool = await _toolsStockService.GetByIdAsync(id);
                if (tool == null) return NotFound($"Инструмент с ID {id} не найден.");
                return Ok(tool);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении инструмента с ID {Id}.", id);
                return StatusCode(500, "Внутренняя ошибка сервера.");
            }
        }

        [HttpPost("add")]
        public async Task<IActionResult> Add([FromBody] ToolsStock tool)
        {
            if (tool == null || !ModelState.IsValid)
                return BadRequest("Некорректные данные для добавления инструмента.");

            try
            {
                await _toolsStockService.AddAsync(tool);
                return CreatedAtAction(nameof(GetById), new { id = tool.ID }, tool);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при добавлении инструмента.");
                return StatusCode(500, "Ошибка при добавлении инструмента.");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] ToolsStock tool)
        {
            if (id != tool.ID) return BadRequest("ID в параметре и объекте не совпадают.");
            if (!ModelState.IsValid)
                return BadRequest("Некорректные данные для обновления инструмента.");

            try
            {
                var updated = await _toolsStockService.UpdateAsync(tool);
                if (!updated) return NotFound($"Инструмент с ID {id} не найден.");
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении инструмента с ID {Id}.", id);
                return StatusCode(500, "Ошибка при обновлении инструмента.");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var deleted = await _toolsStockService.DeleteAsync(id);
                if (!deleted) return NotFound($"Инструмент с ID {id} не найден.");
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении инструмента с ID {Id}.", id);
                return StatusCode(500, "Ошибка при удалении инструмента.");
            }
        }
    }
}
