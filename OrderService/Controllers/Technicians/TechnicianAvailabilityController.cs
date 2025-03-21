using Microsoft.AspNetCore.Mvc;
using OrderService.Services.Technicians;
using OrderService.Models.Users;

namespace OrderService.Controllers.Technicians
{
    [ApiController]
    [Route("technicians/availability")]
    public class TechnicianAvailabilityController(TechnicianAvailabilityService availabilityService) : ControllerBase
    {
        private readonly TechnicianAvailabilityService _availabilityService = availabilityService;

        /// <summary>
        /// 🔍 Получить всех доступных техников на сегодня.
        /// </summary>
        [HttpGet("today")]
        public async Task<ActionResult<List<Technician>>> GetAvailableTechniciansToday()
        {
            var today = DateTime.UtcNow.Date;
            var available = await _availabilityService.GetAvailableTechniciansAsync(today);

            return Ok(available);
        }
    }
}
