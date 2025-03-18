using Microsoft.AspNetCore.Mvc;
using OrderService.Services.Orders;
using OrderService.DTO.Orders.CreateOrders;
using OrderService.DTO.Orders.UpdateOrders;

namespace OrderService.Controllers.Orders
{
    [ApiController]
    [Route("manager/orders")]
    public class OrderManagerController(OrderServiceManager orderServiceManager, ILogger<OrderManagerController> logger) : ControllerBase
    {
        private readonly OrderServiceManager _orderServiceManager = orderServiceManager;
        private readonly ILogger<OrderManagerController> _logger = logger;

        /// <summary>
        /// 📌 Создание заявки менеджером (сразу указывается склад, оборудование, материалы, техники).
        /// </summary>
        [HttpPost("create")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequestManager request)
        {
            _logger.LogInformation("📌 Менеджер {ManagerId} создаёт заявку", request.ManagerId);

            var result = await _orderServiceManager.CreateOrderByManagerAsync(request);
            if (result == null)
            {
                return BadRequest("❌ Ошибка при создании заявки.");
            }

            return Ok(result);
        }

        /// <summary>
        /// ✏️ Обновление некритических параметров заявки менеджером.
        /// </summary>
        [HttpPut("update/{orderId}")]
        public async Task<IActionResult> UpdateOrder(Guid orderId, [FromBody] UpdateOrderRequestManager request)
        {
            _logger.LogInformation("✏️ Менеджер {ManagerId} редактирует заявку {OrderId}", request.ManagerId, orderId);

            if (orderId != request.OrderId)
            {
                return BadRequest("❌ Ошибка: Несовпадение идентификаторов заявки.");
            }

            var result = await _orderServiceManager.UpdateOrderGeneralDetailsAsync(orderId, request);
            if (!result)
            {
                return BadRequest("❌ Ошибка при обновлении заявки.");
            }

            return Ok($"✅ Заявка {orderId} успешно обновлена.");
        }

        [HttpGet("get/all")]
        public async Task<IActionResult> GetAllOrders()
        {
            _logger.LogInformation("📜 Получение всех заявок...");

            var orders = await _orderServiceManager.GetAllOrdersAsync();
            if (orders.Count == 0)
            {
                return NotFound(new { message = "⚠️ Нет доступных заявок." });
            }

            return Ok(orders);
        }

        [HttpGet("get/{orderId}")]
        public async Task<IActionResult> GetOrderById(Guid orderId)
        {
            _logger.LogInformation("🔍 Получение заявки {OrderId}", orderId);

            var order = await _orderServiceManager.GetOrderByIdAsync(orderId);
            if (order == null)
            {
                return NotFound(new { message = $"❌ Заявка {orderId} не найдена." });
            }

            return Ok(order);
        }

        [HttpDelete("delete/{orderId}")]
        public async Task<IActionResult> DeleteOrder(Guid orderId)
        {
            _logger.LogInformation("🗑️ Удаление заявки {OrderId}", orderId);

            var result = await _orderServiceManager.DeleteOrderAsync(orderId);
            if (!result)
            {
                return BadRequest(new { message = $"❌ Ошибка при удалении заявки {orderId}." });
            }

            return Ok(new { message = $"✅ Заявка {orderId} успешно удалена." });
        }


        [HttpPut("update-status")]
        public async Task<IActionResult> UpdateOrderStatus([FromBody] UpdateOrderStatusDTO request)
        {
            if (request == null || request.OrderId == Guid.Empty)
            {
                return BadRequest("❌ Ошибка: Некорректный запрос.");
            }

            var result = await _orderServiceManager.UpdateOrderStatusAsync(request.OrderId, request.NewStatus);

            if (!result)
            {
                return BadRequest($"❌ Ошибка обновления статуса заявки {request.OrderId}.");
            }

            return Ok($"✅ Статус заявки {request.OrderId} успешно обновлён на {request.NewStatus}.");
        }

    }
}
