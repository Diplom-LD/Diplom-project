using Microsoft.AspNetCore.Mvc;
using OrderService.Models.Warehouses;
using OrderService.Services.Warehouses;

namespace OrderService.Controllers.Warehouses
{
    [ApiController]
    [Route("materials-stock")]
    public class MaterialsStockController(MaterialsStockService stockService, ILogger<MaterialsStockController> logger) : ControllerBase
    {
        private readonly MaterialsStockService _stockService = stockService;
        private readonly ILogger<MaterialsStockController> _logger = logger;

        [HttpGet]
        public async Task<ActionResult<List<MaterialsStock>>> GetAll(CancellationToken cancellationToken)
        {
            var items = await _stockService.GetAllAsync(cancellationToken);
            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<MaterialsStock>> GetById(string id, CancellationToken cancellationToken)
        {
            var item = await _stockService.GetByIdAsync(id, cancellationToken);
            return item is null ? NotFound($"Материал с ID {id} не найден.") : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<string>> Add([FromBody] MaterialsStock item, CancellationToken cancellationToken)
        {
            var id = await _stockService.AddAsync(item, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id }, item);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] MaterialsStock item, CancellationToken cancellationToken)
        {
            var updated = await _stockService.UpdateAsync(item, cancellationToken);
            return updated ? NoContent() : NotFound($"Материал с ID {id} не найден.");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
        {
            var deleted = await _stockService.DeleteAsync(id, cancellationToken);
            return deleted ? NoContent() : NotFound($"Материал с ID {id} не найден.");
        }
    }
}
