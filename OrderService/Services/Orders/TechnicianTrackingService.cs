using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using OrderService.Repositories.Users;
using OrderService.Models.Enums;
using OrderService.Repositories.Orders;
using OrderService.DTO.Orders.TechnicianLocation;
using OrderService.Services.GeoLocation;
using Microsoft.Extensions.DependencyInjection;

namespace OrderService.Services.Orders
{
    public class TechnicianTrackingService(
        UserRedisRepository userRedisRepository,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<TechnicianTrackingService> logger)
    {
        private readonly UserRedisRepository _userRedisRepository = userRedisRepository;
        private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
        private readonly ILogger<TechnicianTrackingService> _logger = logger;

        private static readonly Dictionary<Guid, List<WebSocket>> _connections = [];

        /// <summary>
        /// 📡 Подключение менеджера к WebSocket для отслеживания техников по заявке.
        /// </summary>
        public async Task TrackTechniciansAsync(Guid orderId, WebSocket webSocket)
        {
            _logger.LogInformation("📡 Менеджер подключился к отслеживанию заявки {OrderId}", orderId);

            lock (_connections)
            {
                if (!_connections.ContainsKey(orderId))
                    _connections[orderId] = [];

                _connections[orderId].Add(webSocket);
            }

            try
            {
                while (webSocket.State == WebSocketState.Open)
                {
                    if (await ShouldCloseTrackingAsync(orderId))
                    {
                        _logger.LogWarning("⚠️ Все техники прибыли, закрываем WebSocket для заявки {OrderId}.", orderId);
                        break;
                    }

                    await NotifyManagerAsync(orderId);
                    await Task.Delay(2000);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ошибка WebSocket для заявки {OrderId}", orderId);
            }
            finally
            {
                await RemoveWebSocketConnection(orderId, webSocket);
            }
        }


        /// <summary>
        /// 📌 Обновление местоположения техника в Redis и уведомление WebSocket.
        /// </summary>
        public async Task<bool> UpdateTechnicianLocationAsync(Guid technicianId, double latitude, double longitude)
        {
            _logger.LogInformation("📡 Обновление локации техника {TechnicianId}: {Latitude}, {Longitude}", technicianId, latitude, longitude);

            using var scope = _serviceScopeFactory.CreateScope();
            var orderRepository = scope.ServiceProvider.GetRequiredService<OrderRepository>();

            var order = await orderRepository.GetOrderByTechnicianIdAsync(technicianId);
            if (order == null)
            {
                _logger.LogWarning("⚠️ Техник {TechnicianId} не привязан к активной заявке.", technicianId);
                return false;
            }

            await _userRedisRepository.SetTechnicianLocationAsync(technicianId, latitude, longitude, order.Id);

            if (await ShouldCloseTrackingAsync(order.Id))
            {
                await CloseWebSocketForOrderAsync(order.Id);
                return false;
            }

            var distance = DistanceCalculator.CalculateDistance(latitude, longitude, order.InstallationLatitude, order.InstallationLongitude);
            if (distance < 0.1) 
            {
                _logger.LogInformation("✅ Техник {TechnicianId} прибыл к заявке {OrderId}", technicianId, order.Id);

                if (await HaveAllTechniciansArrivedAsync(order.Id))
                {
                    _logger.LogInformation("✅ Все техники прибыли! Меняем статус заявки {OrderId} на 'InstallationStarted'", order.Id);

                    order.WorkProgress = WorkProgress.InstallationStarted;
                    await orderRepository.UpdateOrderAsync(order);

                    _logger.LogInformation("✅ Статус заявки {OrderId} успешно обновлен на 'InstallationStarted'", order.Id);

                    await CloseWebSocketForOrderAsync(order.Id);
                }
            }


            await NotifyManagerAsync(order.Id);
            return true;
        }


        /// <summary>
        /// 🔍 Проверяет, нужно ли закрыть WebSocket для заявки.
        /// </summary>
        private async Task<bool> ShouldCloseTrackingAsync(Guid orderId)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var orderRepository = scope.ServiceProvider.GetRequiredService<OrderRepository>();

            var order = await orderRepository.GetOrderByIdAsync(orderId);
            if (order == null || order.FulfillmentStatus is FulfillmentStatus.Cancelled)
            {
                _logger.LogWarning("⚠️ [WebSocket] Заявка {OrderId} закрыта или отменена.", orderId);
                return true;
            }

            return await HaveAllTechniciansArrivedAsync(orderId);
        }


        /// <summary>
        /// 📌 Проверяет, добрались ли все техники к заявке.
        /// </summary>
        public async Task<bool> HaveAllTechniciansArrivedAsync(Guid orderId)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var orderRepository = scope.ServiceProvider.GetRequiredService<OrderRepository>();

            var order = await orderRepository.GetOrderByIdAsync(orderId);
            if (order == null) return false;

            var technicians = await orderRepository.GetTechniciansByOrderIdAsync(orderId);
            foreach (var technician in technicians)
            {
                var loc = await _userRedisRepository.GetTechnicianLocationAsync(technician.Id);
                if (loc == null || DistanceCalculator.CalculateDistance(loc.Latitude, loc.Longitude, order.InstallationLatitude, order.InstallationLongitude) > 0.1)
                {
                    return false;
                }
            }
            return true;
        }


        /// <summary>
        /// ❌ Закрывает WebSocket-подключения для заявки.
        /// </summary>
        public async Task CloseWebSocketForOrderAsync(Guid orderId)
        {
            _logger.LogWarning("⚠️ Закрываем WebSocket-соединения для заявки {OrderId}.", orderId);

            List<WebSocket> socketsToClose;
            lock (_connections)
            {
                if (!_connections.TryGetValue(orderId, out var sockets) || sockets.Count == 0) return;
                socketsToClose = [.. sockets.Where(s => s.State == WebSocketState.Open)];
                _connections.Remove(orderId);
            }

            foreach (var socket in socketsToClose)
            {
                if (socket.State == WebSocketState.Open)
                {
                    try
                    {
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "All technicians arrived", CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Ошибка при закрытии WebSocket для заявки {OrderId}", orderId);
                    }
                }
            }
        }


        /// <summary>
        /// 📡 Отправляет обновленные координаты техников менеджерам.
        /// </summary>
        public async Task NotifyManagerAsync(Guid orderId)
        {
            if (!_connections.TryGetValue(orderId, out var activeConnections) || activeConnections.Count == 0) return;

            using var scope = _serviceScopeFactory.CreateScope();
            var orderRepository = scope.ServiceProvider.GetRequiredService<OrderRepository>();

            var technicians = await orderRepository.GetTechniciansByOrderIdAsync(orderId);
            var locations = await Task.WhenAll(technicians.Select(async t =>
            {
                var location = await _userRedisRepository.GetTechnicianLocationAsync(t.Id);
                return location != null ? new TechnicianLocationDTO
                {
                    TechnicianId = t.Id,
                    Latitude = location.Latitude,
                    Longitude = location.Longitude,
                    OrderId = location.OrderId 
                } : null;
            }));

            var validLocations = locations.Where(l => l != null).ToList();
            var jsonData = JsonSerializer.Serialize(validLocations);
            var buffer = Encoding.UTF8.GetBytes(jsonData);

            foreach (var webSocket in activeConnections)
            {
                if (webSocket.State == WebSocketState.Open)
                {
                    await webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
        }

        /// <summary>
        /// ❌ Удаляет WebSocket-соединение из списка активных подключений.
        /// </summary>
        private async Task RemoveWebSocketConnection(Guid orderId, WebSocket webSocket)
        {
            lock (_connections)
            {
                if (_connections.TryGetValue(orderId, out var sockets))
                {
                    sockets.Remove(webSocket);
                    if (sockets.Count == 0)
                        _connections.Remove(orderId);
                }
            }

            if (webSocket.State == WebSocketState.Open || webSocket.State == WebSocketState.CloseReceived)
            {
                try
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed", CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "⚠️ Ошибка при закрытии WebSocket для заявки {OrderId}", orderId);
                }
            }

            _logger.LogWarning("⚠️ [WebSocket] Соединение закрыто для заявки {OrderId}", orderId);
        }



        /// <summary>
        /// 🔍 Проверяет, прибыли ли все назначенные техники к месту выполнения заявки.
        /// </summary>
        public async Task<bool> CheckIfAllTechniciansArrivedAsync(Guid orderId)
        {
            _logger.LogInformation("🔍 Проверка прибытия всех техников для заявки {OrderId}", orderId);

            using var scope = _serviceScopeFactory.CreateScope();
            var scopedOrderRepository = scope.ServiceProvider.GetRequiredService<OrderRepository>();

            var technicians = await scopedOrderRepository.GetTechniciansByOrderIdAsync(orderId);
            if (technicians.Count == 0)
            {
                _logger.LogWarning("⚠️ В заявке {OrderId} нет назначенных техников!", orderId);
                return false;
            }

            int arrivedCount = 0;
            foreach (var technician in technicians)
            {
                var location = await _userRedisRepository.GetTechnicianLocationAsync(technician.Id);
                if (location != null)
                {
                    var order = await scopedOrderRepository.GetOrderByIdAsync(orderId);
                    if (order == null)
                    {
                        _logger.LogError("❌ Ошибка: заявка {OrderId} не найдена!", orderId);
                        return false;
                    }

                    double distance = DistanceCalculator.CalculateDistance(
                        location.Latitude, location.Longitude,
                        order.InstallationLatitude, order.InstallationLongitude);

                    if (distance < 0.1)
                    {
                        arrivedCount++;
                    }
                }
            }

            bool allArrived = arrivedCount == technicians.Count;
            if (allArrived)
            {
                _logger.LogInformation("✅ Все техники прибыли к заявке {OrderId}", orderId);
            }
            else
            {
                _logger.LogInformation("⏳ {Arrived}/{Total} техников прибыли к заявке {OrderId}", arrivedCount, technicians.Count, orderId);
            }

            return allArrived;
        }


  
        /// ✅ Отмечает техника как прибывшего к заявке.
        /// </summary>
        public async Task MarkTechnicianAsArrivedAsync(Guid technicianId, Guid orderId)
        {
            _logger.LogInformation("✅ Техник {TechnicianId} прибыл к заявке {OrderId}", technicianId, orderId);

            using var scope = _serviceScopeFactory.CreateScope();
            var scopedOrderRepository = scope.ServiceProvider.GetRequiredService<OrderRepository>();
            var userRepository = scope.ServiceProvider.GetRequiredService<UserPostgreRepository>(); 

            var order = await scopedOrderRepository.GetOrderByIdAsync(orderId);
            if (order == null)
            {
                _logger.LogError("❌ Ошибка: заявка {OrderId} не найдена!", orderId);
                return;
            }

            var technician = await userRepository.GetTechnicianByIdAsync(technicianId);
            if (technician == null)
            {
                _logger.LogError("❌ Ошибка: техник {TechnicianId} не найден!", technicianId);
                return;
            }

            // Обновляем статус работы заявки
            order.WorkProgress = WorkProgress.InstallationStarted;
            await scopedOrderRepository.UpdateOrderAsync(order);

            _logger.LogInformation("🔄 Статус заявки {OrderId} обновлен на 'InstallationStarted' после прибытия техника {TechnicianId}", orderId, technicianId);
        }

        public async Task OpenWebSocketForOrderAsync(Guid orderId)
        {
            _logger.LogInformation("📡 Открываем WebSocket-трекинг для заявки {OrderId}", orderId);

            using var scope = _serviceScopeFactory.CreateScope();
            var orderRepository = scope.ServiceProvider.GetRequiredService<OrderRepository>();

            var order = await orderRepository.GetOrderByIdAsync(orderId);
            if (order == null)
            {
                _logger.LogError("❌ Ошибка: заявка {OrderId} не найдена!", orderId);
                return;
            }

            // Имитируем WebSocket-соединение (для вызова из OrderServiceManager)
            using var fakeWebSocket = WebSocket.CreateFromStream(Stream.Null, new WebSocketCreationOptions
            {
                IsServer = true
            });

            await TrackTechniciansAsync(orderId, fakeWebSocket);
        }

    }
}
