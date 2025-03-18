using Microsoft.AspNetCore.Mvc;
using OrderService.Services.Orders;
using OrderService.DTO.Orders.CreateOrders;

namespace OrderService.Controllers.Orders
{
    [ApiController]
    [Route("orders/client")]
    public class OrderClientController(OrderServiceClient orderServiceClient, ILogger<OrderClientController> logger) : ControllerBase
    {
        private readonly OrderServiceClient _orderServiceClient = orderServiceClient;
        private readonly ILogger<OrderClientController> _logger = logger;

        /// <summary>
        /// 📌 Клиент создаёт заявку (⚡ Без склада, оборудования, материалов, техников).
        /// </summary>
        [HttpPost("create")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequestForClient request)
        {
            _logger.LogInformation("📌 Клиент {ClientId} создаёт заявку", request.ClientId);

            var result = await _orderServiceClient.CreateOrderByClientAsync(request);
            if (result == null)
            {
                return BadRequest("❌ Ошибка при создании заявки.");
            }

            return Ok(result);
        }
    }
}
