using ManagerApp.Clients;
using ManagerApp.DTO.Orders;
using Microsoft.AspNetCore.Mvc;

namespace ManagerApp.Controllers.Maps
{
    public class MapController(IConfiguration configuration, OrderServiceClient orderServiceClient) : Controller
    {
        private readonly IConfiguration _configuration = configuration;
        private readonly OrderServiceClient _orderServiceClient = orderServiceClient;

        /// <summary>
        /// Возвращает API-ключ для карт, полученный из конфигурации.
        /// </summary>
        [HttpGet("/maps/api-key")]
        public IActionResult GetMapApiKey()
        {
            var apiKey = _configuration["MapTiler:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                return NotFound(new { message = "API key not found" });
            }
            return Json(new { apiKey });
        }

        /// <summary>
        /// 🌍 Отображение всех складов и адресов техников на карте.
        /// </summary>
        [HttpGet("/maps/all")]
        public async Task<IActionResult> ShowLocations()
        {
            var accessToken = Request.Cookies["accessToken"];
            if (string.IsNullOrEmpty(accessToken))
            {
                return RedirectToAction("Login", "Account");
            }

            var locations = await _orderServiceClient.GetAllLocationsAsync(accessToken);
            if (locations == null)
            {
                TempData["Error"] = "Не удалось получить данные для карты.";
                return View("Map", new AllLocationsResponseDTO());
            }

            return View("Map", locations);
        }
    }
}
