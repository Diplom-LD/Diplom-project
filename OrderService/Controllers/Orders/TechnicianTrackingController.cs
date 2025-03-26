using Microsoft.AspNetCore.Mvc;
using OrderService.Services.Orders;
using OrderService.DTO.Orders.TechnicianLocation;

namespace OrderService.Controllers.Orders
{
    [ApiController]
    [Route("technicians")]
    public class TechnicianTrackingController(TechnicianTrackingService trackingService, TechnicianSimulationService simulationService) : ControllerBase
    {
        private readonly TechnicianTrackingService _trackingService = trackingService;
        private readonly TechnicianSimulationService _simulationService = simulationService;

        /// <summary>
        /// 📡 WebSocket для live-трекинга передвижения техников по заявке.
        /// </summary>
        [HttpGet("orders/{orderId}/track")]
        public async Task<IActionResult> TrackOrder(Guid orderId)
        {
            if (!HttpContext.WebSockets.IsWebSocketRequest)
                return BadRequest(new { error = "❌ Это не WebSocket-запрос!" });

            using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();

            _ = _simulationService.SimulateAllTechniciansMovementAsync(orderId);

            await _trackingService.TrackTechniciansAsync(orderId, webSocket);

            return StatusCode(101); 
        }

        /// <summary>
        /// 🔄 Обновление текущего местоположения техника.
        /// </summary>
        [HttpPatch("{technicianId}/location")]
        public async Task<IActionResult> UpdateTechnicianLocation(Guid technicianId, [FromBody] TechnicianLocationDTO location)
        {
            if (technicianId != location.TechnicianId)
            {
                return BadRequest(new { error = "❌ TechnicianId в URL и теле запроса не совпадают." });
            }

            try
            {
                var result = await _trackingService.UpdateTechnicianLocationAsync(technicianId, location.Latitude, location.Longitude);

                if (!result)
                {
                    return NotFound(new { error = $"❌ Техник {technicianId} не найден или ошибка при обновлении." });
                }

                // 🔥 Новая проверка: если все техники прибыли – отключаем WebSocket
                await _trackingService.CheckIfAllTechniciansArrivedAsync(location.OrderId);

                return Ok(new { message = "✅ Местоположение обновлено.", technicianId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "❌ Ошибка при обновлении местоположения техника.", details = ex.Message });
            }
        }

        /// <summary>
        /// ✅ Проверка, добрались ли все техники к месту заявки.
        /// </summary>
        [HttpGet("orders/{orderId}/check-arrival")]
        public async Task<IActionResult> CheckTechniciansArrival(Guid orderId)
        {
            try
            {
                var allArrived = await _trackingService.CheckIfAllTechniciansArrivedAsync(orderId);

                if (allArrived)
                {
                    return Ok(new { message = "✅ Все техники прибыли к заявке.", orderId });
                }

                return Ok(new { message = "⏳ Некоторые техники еще в пути.", orderId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "❌ Ошибка при проверке прибытия техников.", details = ex.Message });
            }
        }
    }
}
