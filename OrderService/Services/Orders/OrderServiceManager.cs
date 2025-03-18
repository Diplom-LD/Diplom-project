using OrderService.Models.Enums;
using OrderService.Models.Orders;
using OrderService.Repositories.Orders;
using OrderService.Services.GeoLocation;
using OrderService.Services.Technicians;
using OrderService.Repositories.Users;
using OrderService.Models.Users;
using OrderService.Services.GeoLocation.GeoCodingClient;
using OrderService.DTO.Orders.CreateOrders;
using OrderService.DTO.Orders.UpdateOrders;
using Microsoft.EntityFrameworkCore;
using OrderService.DTO.Orders;

namespace OrderService.Services.Orders
{
    public class OrderServiceManager(
        OrderRepository orderRepository,
        GeoCodingService geoCodingService,
        NearestLocationFinderService nearestLocationFinderService,
        TechnicianRouteSaveService technicianRouteSaveService,
        TechnicianTrackingService technicianTrackingService,
        TechnicianSimulationService technicianSimulationService,
        UserPostgreRepository userPostgreRepository,
        UserRedisRepository userRedisRepository,
        ILogger<OrderServiceManager> logger)
    {
        private readonly OrderRepository _orderRepository = orderRepository;
        private readonly GeoCodingService _geoCodingService = geoCodingService;
        private readonly NearestLocationFinderService _nearestLocationFinderService = nearestLocationFinderService;
        private readonly TechnicianRouteSaveService _technicianRouteSaveService = technicianRouteSaveService;
        private readonly TechnicianSimulationService _technicianSimulationService = technicianSimulationService;
        private readonly UserPostgreRepository _userPostgreRepository = userPostgreRepository;
        private readonly UserRedisRepository _userRedisRepository = userRedisRepository;
        private readonly ILogger<OrderServiceManager> _logger = logger;
        private readonly TechnicianTrackingService _technicianTrackingService = technicianTrackingService;

        /// <summary>
        /// 📌 Менеджер создаёт новую заявку (с выбором склада, оборудования, материалов и техников).
        /// </summary>
        public async Task<CreatedOrderResponseDTO?> CreateOrderByManagerAsync(CreateOrderRequestManager request)
        {
            _logger.LogInformation("📌 Менеджер {ManagerId} создаёт заявку", request.ManagerId);

            var user = await _userPostgreRepository.GetUserByIdAsync(request.ManagerId ?? Guid.Empty);
            if (user is not Manager manager)
            {
                _logger.LogError("❌ Ошибка: Пользователь с ID {ManagerId} не является менеджером!", request.ManagerId);
                return null;
            }

            return await ProcessManagerOrderCreationAsync(
                request,
                request.OrderType,
                null,
                request.FullName,
                request.PhoneNumber,
                request.Email,
                manager.Id,
                manager,
                request.Equipment.ModelName,
                request.TechnicianIds?.Select(id => id.ToString()).ToList()
            );
        }

        /// <summary>
        /// 🔄 Логика создания заявки менеджером
        /// </summary>
        private async Task<CreatedOrderResponseDTO?> ProcessManagerOrderCreationAsync(
    CreateOrderRequestManager request,
    OrderType orderType,
    Guid? clientId,
    string clientName,
    string clientPhone,
    string clientEmail,
    Guid managerId,
    Manager manager,
    string? equipmentModel,
    List<string>? technicianIds)
        {
            _logger.LogInformation("🔄 Создание заявки менеджером...");

            await using var transaction = await _orderRepository.BeginTransactionAsync();

            try
            {
                // 1️⃣ Определение координат установки
                if (string.IsNullOrWhiteSpace(request.InstallationAddress))
                {
                    _logger.LogError("❌ Ошибка: Адрес установки не указан!");
                    return null;
                }

                var location = await _geoCodingService.GetCoordinatesAsync(request.InstallationAddress);
                if (location == null)
                {
                    _logger.LogError("❌ Ошибка: Невозможно определить координаты для {Address}", request.InstallationAddress);
                    return null;
                }

                _logger.LogInformation("📍 Определены координаты установки: {Latitude}, {Longitude}",
                    location.Value.Latitude, location.Value.Longitude);

                _logger.LogInformation("🔍 Входные параметры: TechnicianIds = {TechnicianIds}, EquipmentModel = {EquipmentModel}",
                    technicianIds != null ? string.Join(", ", technicianIds) : "None", equipmentModel);

                // 2️⃣ Поиск склада, оборудования и техников
                var nearestLocationData = await _nearestLocationFinderService.FindNearestLocationsAsync(
                    location.Value.Latitude, location.Value.Longitude,
                    orderType, equipmentModel, technicianIds);

                if (nearestLocationData.NearestWarehouses.Count == 0)
                {
                    _logger.LogError("❌ Не найден склад с необходимыми ресурсами!");
                    return null;
                }

                if (nearestLocationData.SelectedTechnicians.Count == 0)
                {
                    _logger.LogError("❌ Нет доступных техников для выполнения заявки!");
                    return null;
                }

                _logger.LogInformation("📌 Координаты для установки: {Latitude}, {Longitude}",
                        location.Value.Latitude, location.Value.Longitude);

                // 3️⃣ Создание заявки
                var orderId = Guid.NewGuid();
                _logger.LogInformation("📌 Создаётся заявка {OrderId} для {ClientName} ({ClientPhone})",
                    orderId, clientName, clientPhone);

                var order = new Order
                {
                    Id = orderId,
                    OrderType = orderType,
                    FulfillmentStatus = FulfillmentStatus.New,
                    WorkProgress = WorkProgress.OrderPlaced,
                    PaymentStatus = request.PaymentStatus,
                    PaymentMethod = request.PaymentMethod,
                    CreationOrderDate = DateTime.UtcNow,
                    InstallationDate = request.InstallationDate == default ? DateTime.UtcNow : request.InstallationDate,
                    InstallationAddress = request.InstallationAddress,
                    InstallationLatitude = location.Value.Latitude,
                    InstallationLongitude = location.Value.Longitude,
                    Notes = request.Notes ?? string.Empty,
                    WorkCost = request.WorkCost,
                    ClientID = clientId,
                    ClientName = clientName,
                    ClientPhone = clientPhone,
                    ClientEmail = clientEmail,
                    ManagerId = managerId,
                    Manager = manager,
                    Equipment = [],
                    RequiredMaterials = [],
                    RequiredTools = [],
                    AssignedTechnicians = []  // <== Добавляем техников
                };

                // 4️⃣ Сохранение заявки в БД
                var created = await _orderRepository.CreateOrderAsync(order);
                if (!created)
                {
                    _logger.LogError("❌ Ошибка при создании заявки {OrderId}!", orderId);
                    await transaction.RollbackAsync();
                    return null;
                }

                _logger.LogInformation("✅ Заявка {OrderId} успешно создана.", orderId);

                // 5️⃣ Добавление оборудования, материалов и инструментов
                order.Equipment.AddRange(nearestLocationData.AvailableEquipment.Select(e => new OrderEquipment
                {
                    ModelName = e.ModelName,
                    ModelSource = e.ModelSource,
                    ModelBTU = e.ModelBTU,
                    ServiceArea = e.ServiceArea,
                    ModelPrice = e.ModelPrice,
                    Quantity = e.Quantity,
                    OrderID = orderId
                }));

                order.RequiredMaterials.AddRange(nearestLocationData.AvailableMaterials.Select(m => new OrderRequiredMaterial
                {
                    MaterialName = m.MaterialName,
                    Quantity = m.Quantity,
                    MaterialPrice = m.MaterialPrice,
                    OrderId = orderId
                }));

                order.RequiredTools.AddRange(nearestLocationData.AvailableTools.Select(t => new OrderRequiredTool
                {
                    ToolName = t.ToolName,
                    Quantity = t.Quantity,
                    OrderId = orderId
                }));

                // 6️⃣ Добавление техников в заявку
                order.AssignedTechnicians.AddRange(nearestLocationData.SelectedTechnicians.Select(t => new OrderTechnician
                {
                    TechnicianID = t.Id,
                    OrderID = orderId
                }));

                _logger.LogInformation("👷 Назначены техники: {TechnicianIds}",
                    string.Join(", ", order.AssignedTechnicians.Select(t => t.TechnicianID)));

                foreach (var technician in nearestLocationData.SelectedTechnicians)
                {
                    await _userRedisRepository.SetTechnicianLocationAsync(
                        technician.Id, technician.Latitude, technician.Longitude, orderId
                    );
                    _logger.LogInformation("📡 Установлен OrderId в Redis для техника {TechnicianId}: {OrderId}",
                        technician.Id, orderId);
                }

                // 7️⃣ Сохранение маршрутов перед фиксацией транзакции
                await _technicianRouteSaveService.SaveInitialRoutesAsync(orderId, nearestLocationData.Routes);

                // 8️⃣ Фиксация транзакции
                await _orderRepository.SaveChangesAsync();
                await transaction.CommitAsync();
                _logger.LogInformation("✅ Заявка {OrderId} успешно сохранена.", orderId);

                return new CreatedOrderResponseDTO(order, nearestLocationData.Routes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ошибка при создании заявки менеджером. Откат транзакции...");
                await transaction.RollbackAsync();
                return null;
            }
        }


        public async Task<bool> UpdateOrderGeneralDetailsAsync(Guid orderId, UpdateOrderRequestManager request)
        {
            _logger.LogInformation("🔧 Менеджер {ManagerId} обновляет общие данные заявки {OrderId}", request.ManagerId, orderId);

            await using var transaction = await _orderRepository.BeginTransactionAsync();

            try
            {
                var order = await _orderRepository.GetOrderByIdAsync(orderId);
                if (order == null)
                {
                    _logger.LogError("❌ Ошибка: Заявка {OrderId} не найдена!", orderId);
                    return false;
                }

                _orderRepository.AttachEntity(order);

                // Обновляем только не критичные параметры
                order.Notes = request.Notes ?? order.Notes;
                order.WorkCost = request.WorkCost ?? order.WorkCost;
                order.PaymentStatus = request.PaymentStatus ?? order.PaymentStatus;
                order.PaymentMethod = request.PaymentMethod ?? order.PaymentMethod;

                // Обновляем данные клиента, если он не привязан к базе
                if (order.ClientID == null)
                {
                    order.ClientName = request.ClientName ?? order.ClientName;
                    order.ClientPhone = request.ClientPhone ?? order.ClientPhone;
                    order.ClientEmail = request.ClientEmail ?? order.ClientEmail;
                }
                else
                {
                    _logger.LogWarning("⚠️ Попытка изменить данные клиента в заявке {OrderId}, но клиент привязан к базе (ClientID = {ClientID})!", order.Id, order.ClientID);
                }

                await _orderRepository.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("✅ Общие данные заявки {OrderId} успешно обновлены.", orderId);
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                _logger.LogWarning("⚠️ Конфликт обновления заявки {OrderId}. Повторная попытка...", orderId);
                await transaction.RollbackAsync();
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ошибка при обновлении общих данных заявки {OrderId}. Откат транзакции...", orderId);
                await transaction.RollbackAsync();
                return false;
            }
        }

        /// <summary>
        /// 📌 Возвращает список всех заявок.
        /// </summary>
        public async Task<List<OrderDTO>> GetAllOrdersAsync()
        {
            _logger.LogInformation("📜 Получение всех заявок...");

            var orders = await _orderRepository.GetAllOrdersAsync();
            if (orders.Count == 0)
            {
                _logger.LogWarning("⚠️ Нет доступных заявок!");
            }

            return [.. orders.Select(order => new OrderDTO(order))];
        }

        /// <summary>
        /// 🔍 Возвращает заявку по ID.
        /// </summary>
        public async Task<OrderDTO?> GetOrderByIdAsync(Guid orderId)
        {
            _logger.LogInformation("🔍 Получение заявки {OrderId}...", orderId);

            var order = await _orderRepository.GetOrderByIdAsync(orderId);
            if (order == null)
            {
                _logger.LogError("❌ Заявка {OrderId} не найдена!", orderId);
                return null;
            }

            return new OrderDTO(order);
        }


        /// <summary>
        /// 🗑️ Удаляет заявку по ID.
        /// </summary>
        public async Task<bool> DeleteOrderAsync(Guid orderId)
        {
            _logger.LogInformation("🗑️ Удаление заявки {OrderId}...", orderId);

            var order = await _orderRepository.GetOrderByIdAsync(orderId);
            if (order == null)
            {
                _logger.LogError("❌ Ошибка: Заявка {OrderId} не найдена!", orderId);
                return false;
            }

            await using var transaction = await _orderRepository.BeginTransactionAsync();
            try
            {
                var deleted = await _orderRepository.DeleteOrderAsync(orderId);
                if (!deleted)
                {
                    _logger.LogError("❌ Ошибка при удалении заявки {OrderId}!", orderId);
                    await transaction.RollbackAsync();
                    return false;
                }

                await transaction.CommitAsync();
                _logger.LogInformation("✅ Заявка {OrderId} успешно удалена.", orderId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ошибка при удалении заявки {OrderId}. Откат транзакции...", orderId);
                await transaction.RollbackAsync();
                return false;
            }
        }


        public async Task<bool> UpdateOrderStatusAsync(Guid orderId, FulfillmentStatus newStatus)
        {
            _logger.LogInformation("🔄 Обновление статуса заявки {OrderId} -> {NewStatus}", orderId, newStatus);

            await using var transaction = await _orderRepository.BeginTransactionAsync();

            try
            {
                var order = await _orderRepository.GetOrderByIdAsync(orderId);
                if (order == null)
                {
                    _logger.LogError("❌ Ошибка: Заявка {OrderId} не найдена!", orderId);
                    return false;
                }

                _orderRepository.AttachEntity(order);

                var currentStatus = order.FulfillmentStatus;
                var currentProgress = order.WorkProgress;

                if (currentStatus == FulfillmentStatus.New && newStatus == FulfillmentStatus.InProgress)
                {
                    order.FulfillmentStatus = FulfillmentStatus.InProgress;
                    order.WorkProgress = WorkProgress.OrderProcessed;
                    _logger.LogInformation("📌 Заявка {OrderId} обработана менеджером. Статус -> InProgress", orderId);

                    await _technicianTrackingService.OpenWebSocketForOrderAsync(orderId);
                    await _technicianSimulationService.SimulateAllTechniciansMovementAsync(orderId);
                }
                else if (currentStatus == FulfillmentStatus.InProgress && currentProgress == WorkProgress.OrderProcessed)
                {
                    order.WorkProgress = WorkProgress.WorkersOnTheRoad;
                    _logger.LogInformation("🚗 Техники направляются к клиенту!");
                }
                else if (currentStatus == FulfillmentStatus.InProgress && currentProgress == WorkProgress.WorkersOnTheRoad)
                {
                    order.WorkProgress = WorkProgress.InstallationStarted;
                    _logger.LogInformation("🔧 Техники прибыли на место. Началась установка...");
                }
                else if (newStatus == FulfillmentStatus.Completed && currentStatus == FulfillmentStatus.InProgress)
                {
                    _logger.LogInformation("✅ Менеджер завершил заявку {OrderId}. Статус -> Completed", orderId);

                    order.FulfillmentStatus = FulfillmentStatus.Completed;
                    order.WorkProgress = WorkProgress.InstallationCompleted;

                    await ReleaseTechniciansAsync(order.Id);
                    await _technicianTrackingService.CloseWebSocketForOrderAsync(order.Id);
                }
                else if (newStatus == FulfillmentStatus.Cancelled)
                {
                    order.FulfillmentStatus = FulfillmentStatus.Cancelled;
                    _logger.LogWarning("⚠️ Заявка {OrderId} была отменена!", orderId);

                    await ReleaseTechniciansAsync(order.Id);
                    await _technicianTrackingService.CloseWebSocketForOrderAsync(order.Id);
                }
                else
                {
                    _logger.LogWarning("⚠️ Недопустимый переход статусов: {CurrentStatus} -> {NewStatus}", currentStatus, newStatus);
                    return false;
                }

                await _orderRepository.SaveChangesAsync();
                await transaction.CommitAsync();
                _logger.LogInformation("✅ Статус заявки {OrderId} успешно обновлён на {NewStatus}, этап: {WorkProgress}", orderId, order.FulfillmentStatus, order.WorkProgress);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ошибка при обновлении статуса заявки {OrderId}. Откат транзакции...", orderId);
                await transaction.RollbackAsync();
                return false;
            }
        }


        private async Task ReleaseTechniciansAsync(Guid orderId)
        {
            _logger.LogInformation("🆓 Освобождение техников, привязанных к заявке {OrderId}", orderId);

            var technicians = await _orderRepository.GetTechniciansByOrderIdAsync(orderId);
            if (technicians.Count == 0)
            {
                _logger.LogWarning("⚠️ В заявке {OrderId} не было назначенных техников.", orderId);
                return;
            }

            foreach (var technician in technicians)
            {
                technician.IsAvailable = true;
                technician.CurrentOrderId = null;
                _logger.LogInformation("✅ Техник {TechnicianId} теперь доступен.", technician.Id);

                await _userRedisRepository.RemoveTechnicianLocationAsync(technician.Id);
            }

            await _orderRepository.SaveChangesAsync();
        }


    }
}
