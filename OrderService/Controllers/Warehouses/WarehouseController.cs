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
            var warehouses = await _warehouseService.GetAllAsync(cancellationToken);
            return Ok(warehouses);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Warehouse>> GetById(string id, CancellationToken cancellationToken)
        {
            var warehouse = await _warehouseService.GetByIdAsync(id, cancellationToken);
            return warehouse is null ? NotFound($"Склад с ID {id} не найден.") : Ok(warehouse);
        }

        [HttpPost]
        public async Task<ActionResult<string>> Add([FromBody] Warehouse warehouse, CancellationToken cancellationToken)
        {
            var id = await _warehouseService.AddAsync(warehouse, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id }, warehouse);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] Warehouse warehouse, CancellationToken cancellationToken)
        {
            var updated = await _warehouseService.UpdateAsync(warehouse, cancellationToken);
            return updated ? NoContent() : NotFound($"Склад с ID {id} не найден.");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
        {
            var deleted = await _warehouseService.DeleteAsync(id, cancellationToken);
            return deleted ? NoContent() : NotFound($"Склад с ID {id} не найден.");
        }
    }
}
