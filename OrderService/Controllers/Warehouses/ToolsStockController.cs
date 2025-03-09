using Microsoft.AspNetCore.Mvc;
using OrderService.Models.Warehouses;
using OrderService.Services.Warehouses;

namespace OrderService.Controllers.Warehouses
{
    [ApiController]
    [Route("tools-stock")]
    public class ToolsStockController(ToolsStockService stockService, ILogger<ToolsStockController> logger) : ControllerBase
    {
        private readonly ToolsStockService _stockService = stockService;
        private readonly ILogger<ToolsStockController> _logger = logger;

        [HttpGet]
        public async Task<ActionResult<List<ToolsStock>>> GetAll(CancellationToken cancellationToken)
        {
            var items = await _stockService.GetAllAsync(cancellationToken);
            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ToolsStock>> GetById(string id, CancellationToken cancellationToken)
        {
            var item = await _stockService.GetByIdAsync(id, cancellationToken);
            return item is null ? NotFound($"Инструмент с ID {id} не найден.") : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<string>> Add([FromBody] ToolsStock item, CancellationToken cancellationToken)
        {
            var id = await _stockService.AddAsync(item, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id }, item);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] ToolsStock item, CancellationToken cancellationToken)
        {
            var updated = await _stockService.UpdateAsync(item, cancellationToken);
            return updated ? NoContent() : NotFound($"Инструмент с ID {id} не найден.");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
        {
            var deleted = await _stockService.DeleteAsync(id, cancellationToken);
            return deleted ? NoContent() : NotFound($"Инструмент с ID {id} не найден.");
        }
    }
}
