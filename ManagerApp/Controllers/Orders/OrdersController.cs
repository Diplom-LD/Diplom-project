using ManagerApp.Clients;
using ManagerApp.Models.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ManagerApp.Controllers.Orders
{
    [Authorize]
    public class OrdersController(OrderServiceClient orderServiceClient, ILogger<OrdersController> logger) : Controller
    {
        private readonly OrderServiceClient _orderServiceClient = orderServiceClient;
        private readonly ILogger<OrdersController> _logger = logger;

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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOrder([FromBody] OrderRequest request)
        {
            string? accessToken = HttpContext.Request.Cookies["accessToken"];

            if (string.IsNullOrEmpty(accessToken))
            {
                return Unauthorized(new { message = "Access token is missing" });
            }

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

    }
}
