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

        /// <summary>
        /// 📜 Получение всех заявок клиента.
        /// </summary>
        [HttpGet("get/all")]
        public async Task<IActionResult> GetAllOrders([FromQuery] Guid clientId)
        {
            var orders = await _orderServiceClient.GetAllOrdersByClientAsync(clientId);
            return orders.Count == 0
                ? NotFound(new { message = "⚠️ Нет доступных заявок." })
                : Ok(orders); 
        }

        /// <summary>
        /// 🔍 Получение конкретной заявки клиента.
        /// </summary>
        [HttpGet("get/{orderId}")]
        public async Task<IActionResult> GetOrderById(Guid orderId)
        {
            var order = await _orderServiceClient.GetOrderByIdAsync(orderId);
            return order == null
                ? NotFound(new { message = $"❌ Заявка {orderId} не найдена." })
                : Ok(order); 
        }

        /// <summary>
        /// 🗑️ Удаление заявки клиентом.
        /// </summary>
        [HttpDelete("delete/{orderId}")]
        public async Task<IActionResult> DeleteOrder(Guid orderId)
        {
            _logger.LogInformation("🗑️ Удаление заявки {OrderId}", orderId);

            var result = await _orderServiceClient.DeleteOrderAsync(orderId);
            if (!result)
            {
                return BadRequest(new { message = $"❌ Ошибка при удалении заявки {orderId}." });
            }

            return Ok(new { message = $"✅ Заявка {orderId} успешно удалена." });
        }

    }
}
