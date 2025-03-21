using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace ManagerApp.Controllers.Maps
{
    public class MapController(IConfiguration configuration) : Controller
    {
        private readonly IConfiguration _configuration = configuration;

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
    }
}
