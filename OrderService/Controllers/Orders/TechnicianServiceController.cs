using Microsoft.AspNetCore.Mvc;
using OrderService.Services.Orders;

namespace OrderService.Controllers.Orders
{
    [ApiController]
    [Route("orders/technician")]
    public class TechnicianServiceController(TechnicianServiceClient technicianServiceClient, ILogger<TechnicianServiceController> logger) : ControllerBase
    {
        private readonly TechnicianServiceClient _technicianServiceClient = technicianServiceClient;
        private readonly ILogger<TechnicianServiceController> _logger = logger;

        /// <summary>
        /// 📦 Получение всех заявок, назначенных технику
        /// </summary>
        [HttpGet("get/all")]
        public async Task<IActionResult> GetOrdersForTechnician([FromQuery] Guid technicianId)
        {
            _logger.LogInformation("📦 Получение всех заявок для техника {TechnicianId}", technicianId);

            var orders = await _technicianServiceClient.GetOrdersForTechnicianAsync(technicianId);

            if (orders == null || orders.Count == 0)
            {
                return NotFound(new { message = $"⚠️ Для техника {technicianId} нет активных заявок." });
            }

            return Ok(orders); 
        }
    }
}
