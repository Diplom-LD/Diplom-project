using OrderService.Repositories.Users;
using OrderService.Models.Enums;
using OrderService.Repositories.Orders;
using System.Collections.Concurrent;

namespace OrderService.Services.Orders
{
    public class TechnicianSimulationService(
        TechnicianTrackingService technicianTrackingService,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<TechnicianSimulationService> logger)
    {
        private readonly TechnicianTrackingService _technicianTrackingService = technicianTrackingService;
        private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
        private readonly ILogger<TechnicianSimulationService> _logger = logger;

        // 🔴 Храним токены отмены для каждого техника
        private static readonly ConcurrentDictionary<Guid, CancellationTokenSource> _activeSimulations = new();

        /// <summary>
        /// 🚀 Запускает симуляцию движения ВСЕХ техников для заявки.
        /// </summary>
        public async Task<bool> SimulateAllTechniciansMovementAsync(Guid orderId, int intervalSeconds = 2)
        {
            _logger.LogInformation("🚀 Запуск симуляции всех техников для заявки {OrderId}", orderId);

            using var scope = _serviceScopeFactory.CreateScope();
            var orderRepository = scope.ServiceProvider.GetRequiredService<OrderRepository>();

            var order = await orderRepository.GetOrderByIdAsync(orderId);
            if (order == null)
            {
                _logger.LogError("❌ Ошибка: Заявка {OrderId} не найдена!", orderId);
                return false;
            }

            var technicians = await orderRepository.GetTechniciansByOrderIdAsync(orderId);
            if (technicians.Count == 0)
            {
                _logger.LogWarning("⚠️ В заявке {OrderId} нет назначенных техников!", orderId);
                return false;
            }

            if (technicians.Any(t => _activeSimulations.ContainsKey(t.Id)))
            {
                _logger.LogWarning("⚠️ Симуляция уже идёт хотя бы для одного техника. Пропускаем повторный запуск.");
                return false;
            }

            _logger.LogInformation("🚦 Запуск симуляции для {TechnicianCount} техников...", technicians.Count);

            List<Task> simulationTasks = [.. technicians.Select(technician => SimulateTechnicianMovementAsync(technician.Id, orderId, intervalSeconds))];

            await Task.WhenAll(simulationTasks);

            _logger.LogInformation("✅ Все техники прибыли на место! Закрываем WebSocket для заявки {OrderId}.", orderId);
            await _technicianTrackingService.CloseWebSocketForOrderAsync(orderId);

            return true;
        }

        /// <summary>
        /// 🔄 Запускает симуляцию движения ОДНОГО техника по маршруту к заявке.
        /// </summary>
        public async Task SimulateTechnicianMovementAsync(Guid technicianId, Guid orderId, int intervalSeconds = 2)
        {
            if (technicianId == Guid.Empty || orderId == Guid.Empty)
            {
                _logger.LogError("❌ Некорректные параметры: TechnicianID = {TechnicianId}, OrderID = {OrderId}", technicianId, orderId);
                return;
            }

            _logger.LogInformation("🚗 Запуск симуляции движения техника {TechnicianId} к заявке {OrderId}", technicianId, orderId);

            using var scope = _serviceScopeFactory.CreateScope();
            var orderRepository = scope.ServiceProvider.GetRequiredService<OrderRepository>();
            var userRepository = scope.ServiceProvider.GetRequiredService<UserPostgreRepository>();

            var order = await orderRepository.GetOrderByIdAsync(orderId);
            if (order == null)
            {
                _logger.LogError("❌ Ошибка: Заявка {OrderId} не найдена!", orderId);
                return;
            }

            if (order.FulfillmentStatus == FulfillmentStatus.Cancelled)
            {
                _logger.LogWarning("⚠️ Заявка {OrderId} была отменена. Остановка симуляции.", orderId);
                return;
            }

            var technician = await userRepository.GetTechnicianByIdAsync(technicianId);
            if (technician == null)
            {
                _logger.LogError("❌ Ошибка: Техник {TechnicianId} не найден!", technicianId);
                return;
            }

            var routes = order.GetInitialRoutes();
            if (routes == null || routes.Count == 0)
            {
                _logger.LogError("❌ Ошибка: Нет маршрутов для заявки {OrderId}", orderId);
                return;
            }

            var route = routes.FirstOrDefault(r => r.TechnicianId == technicianId);
            if (route == null || route.RoutePoints.Count < 2)
            {
                _logger.LogWarning("⚠️ Недостаточно точек маршрута для техника {TechnicianId}. Пропускаем...", technicianId);
                return;
            }

            _logger.LogInformation("🚦 Начало движения техника {TechnicianId}, точек маршрута: {PointsCount}", technicianId, route.RoutePoints.Count);

            var cts = new CancellationTokenSource();
            if (!_activeSimulations.TryAdd(technicianId, cts))
            {
                _logger.LogWarning("⚠️ Симуляция для техника {TechnicianId} уже запущена!", technicianId);
                return;
            }

            try
            {
                for (int i = 1; i < route.RoutePoints.Count; i++)
                {
                    var previous = route.RoutePoints[i - 1];
                    var current = route.RoutePoints[i];

                    await MoveSmoothlyBetweenPoints(
                        technicianId,
                        previous.Latitude, previous.Longitude,
                        current.Latitude, current.Longitude,
                        intervalSeconds,
                        cts.Token
                    );
                }

                _logger.LogInformation("✅ Техник {TechnicianId} прибыл к клиенту по заявке {OrderId}", technicianId, orderId);

                await _technicianTrackingService.MarkTechnicianAsArrivedAsync(technicianId, orderId);
                bool allArrived = await _technicianTrackingService.CheckIfAllTechniciansArrivedAsync(orderId);

                if (allArrived)
                {
                    _logger.LogInformation("✅ Все техники прибыли! Закрываем WebSocket.");
                    await _technicianTrackingService.CloseWebSocketForOrderAsync(orderId);
                }
            }
            finally
            {
                _activeSimulations.TryRemove(technicianId, out _);
                cts.Dispose();
            }
        }


        /// <summary>
        /// 🛑 Останавливает симуляцию всех техников в заявке.
        /// </summary>
        public async Task<bool> StopSimulationForOrderAsync(Guid orderId)
        {
            _logger.LogInformation("🛑 Остановка всех симуляций для заявки {OrderId}", orderId);

            using var scope = _serviceScopeFactory.CreateScope();
            var orderRepository = scope.ServiceProvider.GetRequiredService<OrderRepository>();

            var technicians = await orderRepository.GetTechniciansByOrderIdAsync(orderId);
            if (technicians.Count == 0)
            {
                _logger.LogWarning("⚠️ В заявке {OrderId} нет активных симуляций!", orderId);
                return false;
            }

            bool stopped = false;
            foreach (var technician in technicians)
            {
                if (_activeSimulations.TryRemove(technician.Id, out var cts))
                {
                    cts.Cancel(); 
                    cts.Dispose();
                    stopped = true;
                    _logger.LogInformation("🛑 Симуляция для техника {TechnicianId} остановлена.", technician.Id);
                }
            }

            return stopped;
        }


        private async Task MoveSmoothlyBetweenPoints(
    Guid technicianId,
    double startLat,
    double startLng,
    double endLat,
    double endLng,
    int totalDurationSeconds,
    CancellationToken token)
        {
            double distanceKm = CalculateDistance(startLat, startLng, endLat, endLng);

            int steps = Math.Clamp((int)(distanceKm * 40), 60, 120);

            double baseDelay = (totalDurationSeconds * 1000.0) / steps;
            var random = new Random();

            for (int i = 1; i <= steps; i++)
            {
                if (token.IsCancellationRequested)
                {
                    _logger.LogWarning("⛔ Движение техника {TechnicianId} остановлено.", technicianId);
                    return;
                }

                double lat = Interpolate(startLat, endLat, i / (double)steps);
                double lng = Interpolate(startLng, endLng, i / (double)steps);

                await _technicianTrackingService.UpdateTechnicianLocationAsync(technicianId, lat, lng);

                try
                {
                    double speedFactor = random.NextDouble() * 0.2 + 0.9; 
                    int delay = (int)(baseDelay * speedFactor);

                    if (random.NextDouble() < 0.07)
                    {
                        int microPause = random.Next(100, 300);
                        _logger.LogInformation("🕒 Техник {TechnicianId} ненадолго притормозил на {PauseMs} мс", technicianId, microPause);
                        delay += microPause;
                    }

                    await Task.Delay(delay, token);
                }
                catch (TaskCanceledException)
                {
                    _logger.LogWarning("⛔ Движение техника {TechnicianId} прервано.", technicianId);
                    return;
                }
            }
        }

        private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371; 
            double dLat = ToRadians(lat2 - lat1);
            double dLon = ToRadians(lon2 - lon1);

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private static double ToRadians(double deg) => deg * Math.PI / 180;

        private static double Interpolate(double start, double end, double fraction)
        {
            return start + (end - start) * fraction;
        }



    }
}
