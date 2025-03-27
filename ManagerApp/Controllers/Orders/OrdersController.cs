using ManagerApp.Clients;
using ManagerApp.DTO.Orders;
using ManagerApp.Models.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace ManagerApp.Controllers.Orders
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly OrderServiceClient _orderServiceClient;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(OrderServiceClient orderServiceClient, ILogger<OrdersController> logger)
        {
            _orderServiceClient = orderServiceClient;
            _logger = logger;
        }

        public IActionResult Orders()
        {
            return View();
        }

        public IActionResult NewOrder()
        {
            return View();
        }

        /// <summary>
        /// Получает список всех заявок через OrderServiceClient.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetOrders()
        {
            string? accessToken = HttpContext.Request.Cookies["accessToken"];

            if (string.IsNullOrEmpty(accessToken))
            {
                return Unauthorized(new { message = "Access token is missing" });
            }

            var orders = await _orderServiceClient.GetAllOrdersAsync(accessToken);

            if (orders == null)
            {
                return StatusCode(500, new { message = "Failed to retrieve orders" });
            }

            return Json(orders);
        }

        /// <summary>
        /// Создаёт новую заявку через OrderServiceClient.
        /// </summary>
        [HttpPost("/manager/orders/create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOrder([FromBody] OrderRequest request)
        {
            string? accessToken = HttpContext.Request.Cookies["accessToken"];

            if (string.IsNullOrEmpty(accessToken))
            {
                return Unauthorized(new { message = "Access token is missing" });
            }

            var managerIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(managerIdStr, out var managerId))
            {
                return BadRequest(new { message = "Invalid ManagerId in token." });
            }

            request.ManagerId = managerId;

            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    message = "Validation failed",
                    errors = ModelState
                        .Where(kvp => kvp.Value?.Errors.Count > 0)
                        .ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                        )
                });
            }

            try
            {
                var result = await _orderServiceClient.CreateOrderAsync(request, accessToken);

                if (result == null)
                {
                    return StatusCode(500, new { message = "Failed to create order." });
                }

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ошибка при создании заявки.");
                return StatusCode(500, new { message = "Internal server error." });
            }
        }


        /// <summary>
        /// Получает заявку по её ID.
        /// </summary>
        [HttpGet("/orders/details/{id:guid}")]
        public async Task<IActionResult> OrderDetails(Guid id)
        {
            string? accessToken = HttpContext.Request.Cookies["accessToken"];

            if (string.IsNullOrEmpty(accessToken))
            {
                return Unauthorized(new { message = "Access token is missing" });
            }

            var order = await _orderServiceClient.GetOrderByIdAsync(id, accessToken);

            if (order == null)
            {
                return NotFound(new { message = $"Order with ID {id} not found." });
            }

            return View("OrderDetails", order);
        }


        [HttpPut("/manager/orders/update/{orderId:guid}")]
        public async Task<IActionResult> UpdateOrderFields(Guid orderId, [FromBody] OrderUpdateRequestDTO updateDto)
        {
            _logger.LogInformation("📥 RAW Request: orderId = {OrderId}, DTO = {@Dto}", orderId, updateDto);

            string? accessToken = HttpContext.Request.Cookies["accessToken"];

            if (string.IsNullOrEmpty(accessToken))
                return Unauthorized(new { message = "Access token is missing" });

            if (orderId != updateDto.OrderId)
                return BadRequest(new { message = "Mismatched order ID" });

            var managerIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(managerIdStr, out var managerId))
                return BadRequest(new { message = "Invalid ManagerId in token." });

            updateDto.ManagerId = managerId;

            _logger.LogInformation("📤 Обновление заявки {OrderId} менеджером {ManagerId}: {Payload}",
                updateDto.OrderId, updateDto.ManagerId, JsonSerializer.Serialize(updateDto));

            var result = await _orderServiceClient.UpdateOrderFieldsAsync(updateDto, accessToken);

            if (!result)
                return StatusCode(500, new { message = "Failed to update order fields." });

            return Ok(new { message = "Order updated successfully." });
        }


        [HttpPut("/manager/orders/update-status")]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateOrderStatusDTO request)
        {
            var accessToken = HttpContext.Request.Cookies["accessToken"];

            if (string.IsNullOrEmpty(accessToken))
                return Unauthorized(new { message = "Access token is missing" });

            var result = await _orderServiceClient.UpdateOrderStatusAsync(request.OrderId, request.NewStatus, accessToken);

            if (!result)
                return BadRequest(new { message = "❌ Не удалось обновить статус заявки." });

            return Ok(new { message = "✅ Статус успешно обновлён." });
        }

    }
}