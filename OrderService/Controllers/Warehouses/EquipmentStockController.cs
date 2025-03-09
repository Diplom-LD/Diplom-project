using Microsoft.AspNetCore.Mvc;
using OrderService.Models.Warehouses;
using OrderService.Services.Warehouses;

namespace OrderService.Controllers.Warehouses
{
    [ApiController]
    [Route("equipment-stock")]
    public class EquipmentStockController(EquipmentStockService stockService, ILogger<EquipmentStockController> logger) : ControllerBase
    {
        private readonly EquipmentStockService _stockService = stockService;
        private readonly ILogger<EquipmentStockController> _logger = logger;

        [HttpGet]
        public async Task<ActionResult<List<EquipmentStock>>> GetAll(CancellationToken cancellationToken)
        {
            var items = await _stockService.GetAllAsync(cancellationToken);
            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<EquipmentStock>> GetById(string id, CancellationToken cancellationToken)
        {
            var item = await _stockService.GetByIdAsync(id, cancellationToken);
            return item is null ? NotFound($"Оборудование с ID {id} не найдено.") : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<string>> Add([FromBody] EquipmentStock item, CancellationToken cancellationToken)
        {
            var id = await _stockService.AddAsync(item, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id }, item);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] EquipmentStock item, CancellationToken cancellationToken)
        {
            var updated = await _stockService.UpdateAsync(item, cancellationToken);
            return updated ? NoContent() : NotFound($"Оборудование с ID {id} не найдено.");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
        {
            var deleted = await _stockService.DeleteAsync(id, cancellationToken);
            return deleted ? NoContent() : NotFound($"Оборудование с ID {id} не найдено.");
        }
    }
}
