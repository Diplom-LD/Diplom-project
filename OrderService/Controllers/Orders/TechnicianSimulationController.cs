using Microsoft.AspNetCore.Mvc;
using OrderService.Services.Orders;

namespace OrderService.Controllers.Orders
{
    [ApiController]
    [Route("simulation/orders")]
    public class TechnicianSimulationController(TechnicianSimulationService simulationService, ILogger<TechnicianSimulationController> logger) : ControllerBase
    {
        private readonly TechnicianSimulationService _simulationService = simulationService;
        private readonly ILogger<TechnicianSimulationController> _logger = logger;

        /// <summary>
        /// 🚗 Запуск тестового движения ВСЕХ техников по маршруту к заявке.
        /// </summary>
        [HttpPost("{orderId}/start")]
        public async Task<IActionResult> StartSimulationForAllTechnicians(Guid orderId)
        {
            if (orderId == Guid.Empty)
            {
                _logger.LogWarning("❌ Ошибка: OrderID не передан.");
                return BadRequest(new { error = "❌ Ошибка: OrderID обязателен." });
            }

            _logger.LogInformation("🚀 Запуск симуляции передвижения ВСЕХ техников к заявке {OrderId}", orderId);

            try
            {
                var started = await _simulationService.SimulateAllTechniciansMovementAsync(orderId);
                if (!started)
                {
                    return NotFound(new { error = "⚠️ Нет доступных техников для симуляции!" });
                }

                return StatusCode(202, new { message = $"🚗 Симуляция передвижения ВСЕХ техников для заявки {orderId} запущена." });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "⚠️ Симуляция уже запущена для заявки {OrderId}", orderId);
                return Conflict(new { error = $"⚠️ Симуляция уже выполняется для заявки {orderId}!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ошибка при запуске симуляции ВСЕХ техников для заявки {OrderId}", orderId);
                return StatusCode(500, new { error = "❌ Внутренняя ошибка сервера при запуске симуляции." });
            }
        }

        /// <summary>
        /// 🛑 Остановка симуляции ВСЕХ техников в заявке.
        /// </summary>
        [HttpPost("{orderId}/stop")]
        public async Task<IActionResult> StopSimulationForAllTechnicians(Guid orderId)
        {
            if (orderId == Guid.Empty)
            {
                _logger.LogWarning("❌ Ошибка: OrderID не передан.");
                return BadRequest(new { error = "❌ Ошибка: OrderID обязателен." });
            }

            _logger.LogInformation("🛑 Запрос на остановку симуляции ВСЕХ техников для заявки {OrderId}", orderId);

            try
            {
                bool stopped = await _simulationService.StopSimulationForOrderAsync(orderId);

                if (stopped)
                {
                    return Ok(new { message = $"🛑 Симуляция ВСЕХ техников для заявки {orderId} остановлена." });
                }
                else
                {
                    return NotFound(new { error = $"⚠️ Симуляция для заявки {orderId} не найдена или уже остановлена." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ошибка при остановке симуляции для заявки {OrderId}", orderId);
                return StatusCode(500, new { error = "❌ Внутренняя ошибка сервера при остановке симуляции." });
            }
        }
    }
}
