using ManagerApp.Clients;
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


    }
}
