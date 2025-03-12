using Microsoft.AspNetCore.Mvc;
using OrderService.DTO.Orders;

namespace OrderService.Controllers.Orders
{
    [ApiController]
    [Route("order")]
    public class OrderController(Services.Orders.OrderService orderService, ILogger<OrderController> logger) : ControllerBase
    {
        private readonly Services.Orders.OrderService _orderService = orderService;
        private readonly ILogger<OrderController> _logger = logger;

        /// <summary>
        /// 📌 Создание новой заявки
        /// </summary>
        [HttpPost("create")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            _logger.LogInformation("📌 Получен запрос на создание заявки для клиента {ClientId}", request.ClientId);

            var response = await _orderService.CreateOrderAsync(request);
            if (response == null)
            {
                _logger.LogError("❌ Ошибка при создании заявки для клиента {ClientId}", request.ClientId);
                return BadRequest(new { message = "Ошибка при создании заявки. Проверьте входные данные." });
            }

            _logger.LogInformation("✅ Заявка успешно создана. ID: {OrderId}", response.Order.Id);
            return CreatedAtAction(nameof(GetOrderById), new { orderId = response.Order.Id }, response);
        }


        /// <summary>
        /// 🔍 Получить заявку по ID
        /// </summary>
        [HttpGet("{orderId}")]
        public async Task<IActionResult> GetOrderById(Guid orderId)
        {
            var order = await _orderService.GetOrderByIdAsync(orderId);
            if (order == null)
            {
                _logger.LogWarning("⚠️ Заявка {OrderId} не найдена.", orderId);
                return NotFound(new { message = "Заявка не найдена." });
            }

            return Ok(order);
        }

        /// <summary>
        /// 📜 Получить список всех заявок
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllOrders()
        {
            var orders = await _orderService.GetAllOrdersAsync();
            return Ok(orders);
        }

        /// <summary>
        /// ✏️ Обновление статуса заявки
        /// </summary>
        [HttpPut("{orderId}/status")]
        public async Task<IActionResult> UpdateOrderStatus(Guid orderId, [FromBody] UpdateOrderStatusRequest request)
        {
            var result = await _orderService.UpdateOrderStatusAsync(orderId, request.NewStatus);
            if (!result)
            {
                _logger.LogError("❌ Не удалось обновить статус заявки {OrderId}", orderId);
                return BadRequest(new { message = "Ошибка при обновлении статуса заявки." });
            }

            _logger.LogInformation("✅ Статус заявки {OrderId} обновлён до {NewStatus}", orderId, request.NewStatus);
            return NoContent();
        }

        /// <summary>
        /// 🗑 Удаление заявки по ID
        /// </summary>
        [HttpDelete("{orderId}")]
        public async Task<IActionResult> DeleteOrder(Guid orderId)
        {
            var result = await _orderService.DeleteOrderAsync(orderId);
            if (!result)
            {
                _logger.LogError("❌ Не удалось удалить заявку {OrderId}", orderId);
                return NotFound(new { message = "Заявка не найдена." });
            }

            _logger.LogInformation("✅ Заявка {OrderId} успешно удалена.", orderId);
            return NoContent();
        }
    }
}
