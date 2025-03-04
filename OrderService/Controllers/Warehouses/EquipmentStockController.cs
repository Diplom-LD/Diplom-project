using Microsoft.AspNetCore.Mvc;
using OrderService.Models.Warehouses;
using OrderService.Services.Warehouses;
using System.Threading;
using System.Threading.Tasks;

namespace OrderService.Controllers.Warehouses
{
    [ApiController]
    [Route("equipment")]
    public class EquipmentStockController(IEquipmentStockService equipmentStockService) : ControllerBase
    {
        private readonly IEquipmentStockService _equipmentStockService = equipmentStockService;

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
            await HandleRequestAsync(() => _equipmentStockService.GetAllAsync(cancellationToken));

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken) =>
            await HandleRequestAsync(() => _equipmentStockService.GetByIdAsync(id, cancellationToken));

        [HttpPost("add")]
        public async Task<IActionResult> Add([FromBody] EquipmentStock equipment, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return await HandleRequestAsync(() => _equipmentStockService.AddAsync(equipment, cancellationToken), equipment.ID);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] EquipmentStock equipment, CancellationToken cancellationToken)
        {
            if (id != equipment.ID)
                return BadRequest("ID в параметре и объекте не совпадают.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return await HandleRequestAsync(() => _equipmentStockService.UpdateAsync(equipment, cancellationToken));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken) =>
            await HandleRequestAsync(() => _equipmentStockService.DeleteAsync(id, cancellationToken));

        private async Task<IActionResult> HandleRequestAsync(Func<Task> action, string? id = null)
        {
            try
            {
                await action();
                if (id != null)
                {
                    return CreatedAtAction(nameof(GetById), new { id }, new { ID = id });
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при выполнении запроса: {ex.Message}");
            }
        }
    }
}
