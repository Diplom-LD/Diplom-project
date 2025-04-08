using System.Net.WebSockets;
using Microsoft.AspNetCore.Mvc;
using OrderService.Models.Enums;
using OrderService.Repositories.Orders;
using OrderService.Services.GeoLocation;
using OrderService.Services.Orders;

namespace OrderService.Controllers.Orders
{
    [ApiController]
    [Route("technicians")]
    public class TechnicianTrackingController(
        TechnicianTrackingService trackingService,
        TechnicianSimulationService simulationService,
        ILogger<TechnicianTrackingController> logger) : ControllerBase
    {
        private readonly TechnicianTrackingService _trackingService = trackingService;
        private readonly TechnicianSimulationService _simulationService = simulationService;
        private readonly ILogger<TechnicianTrackingController> _logger = logger;

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

            return new EmptyResult();
        }

        /// <summary>
        /// 📡 Получение координат от техника по WebSocket.
        /// </summary>
        [HttpGet("track/send/{technicianId}")]
        public async Task<IActionResult> ReceiveTechnicianLocation(Guid technicianId)
        {
            if (!HttpContext.WebSockets.IsWebSocketRequest)
                return BadRequest(new { error = "❌ Это не WebSocket-запрос!" });

            _logger.LogInformation("📡 WebSocket соединение от техника {TechnicianId} установлено", technicianId);

            using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();

            try
            {
                using var scope = HttpContext.RequestServices.CreateScope();
                var orderRepository = scope.ServiceProvider.GetRequiredService<OrderRepository>();

                var order = await orderRepository.GetOrderByTechnicianIdAsync(technicianId);
                if (order != null)
                {
                    order.FulfillmentStatus = FulfillmentStatus.InProgress;
                    order.WorkProgress = WorkProgress.WorkersOnTheRoad;
                    await orderRepository.UpdateOrderAsync(order);

                    _logger.LogInformation("🔄 Заявка {OrderId} обновлена: InProgress + WorkersOnTheRoad", order.Id);

                    var technicians = await orderRepository.GetTechniciansByOrderIdAsync(order.Id);
                    foreach (var tech in technicians)
                    {
                        var location = await _trackingService.GetTechnicianLocationAsync(tech.Id);
                        if (location != null)
                        {
                            var distance = DistanceCalculator.CalculateDistance(
                                location.Latitude, location.Longitude,
                                order.InstallationLatitude, order.InstallationLongitude);

                            if (distance < 0.000045)
                            {
                                _logger.LogInformation("✅ Техник {TechnicianId} уже прибыл — меняем статус заявки {OrderId} на InstallationStarted", tech.Id, order.Id);
                                order.WorkProgress = WorkProgress.InstallationStarted;
                                await orderRepository.UpdateOrderAsync(order);
                                break;
                            }
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("⚠️ Заявка не найдена для техника {TechnicianId}", technicianId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ошибка при обновлении статуса заявки для техника {TechnicianId}", technicianId);
            }

            await _trackingService.ReceiveTechnicianCoordinatesAsync(technicianId, webSocket);

            _logger.LogInformation("🔌 WebSocket-соединение от техника {TechnicianId} закрыто", technicianId);

            return new EmptyResult();
        }

        /// <summary>
        /// 🛰️ WebSocket для клиента, отслеживающего движение техников.
        /// </summary>
        [HttpGet("orders/{orderId}/client-track")]
        public async Task<IActionResult> ClientTrackOrder(Guid orderId)
        {
            if (!HttpContext.WebSockets.IsWebSocketRequest)
                return BadRequest(new { error = "❌ Это не WebSocket-запрос!" });

            var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();

            try
            {
                await _trackingService.TrackTechniciansAsync(orderId, webSocket);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ошибка во время WebSocket-трекинга клиента для заявки {OrderId}", orderId);
            }
            finally
            {
                if (webSocket.State != WebSocketState.Closed && webSocket.State != WebSocketState.Aborted)
                {
                    try
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Закрытие клиентского WebSocket", CancellationToken.None);
                    }
                    catch (Exception closeEx)
                    {
                        _logger.LogWarning(closeEx, "⚠️ Ошибка при принудительном закрытии WebSocket клиента для заявки {OrderId}", orderId);
                    }
                }

                _logger.LogInformation("🔌 [ClientTrack] Соединение WebSocket закрыто для заявки {OrderId}", orderId);
            }

            return new EmptyResult();
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

                return Ok(new
                {
                    message = allArrived
                        ? "✅ Все техники прибыли к заявке."
                        : "⏳ Некоторые техники еще в пути.",
                    orderId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "❌ Ошибка при проверке прибытия техников.", details = ex.Message });
            }
        }
    }
}
