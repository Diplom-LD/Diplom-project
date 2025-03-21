using ManagerApp.Clients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ManagerApp.Controllers.Technicians
{
    [Authorize]
    public class TechnicianController(OrderServiceClient orderServiceClient, ILogger<TechnicianController> logger) : Controller
    {
        private readonly OrderServiceClient _orderServiceClient = orderServiceClient;
        private readonly ILogger<TechnicianController> _logger = logger;

        [HttpGet("technicians/available-today")]
        public async Task<IActionResult> GetAvailableTechniciansToday()
        {
            try
            {
                string? accessToken = HttpContext.Request.Cookies["accessToken"];

                if (string.IsNullOrWhiteSpace(accessToken))
                {
                    _logger.LogWarning("⛔ Отсутствует accessToken в cookies.");
                    return Unauthorized("Access token is missing.");
                }

                var availableTechnicians = await _orderServiceClient.GetAvailableTechniciansTodayAsync(accessToken);

                if (availableTechnicians == null)
                {
                    return StatusCode(500, new { message = "Не удалось получить список техников." });
                }

                return Json(availableTechnicians);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ошибка при получении доступных техников на сегодня.");
                return StatusCode(500, new { message = "Внутренняя ошибка при получении данных." });
            }
        }

    }
}
