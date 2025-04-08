using OrderService.Models.Enums;
using OrderService.Models.Orders;
using OrderService.Repositories.Orders;
using OrderService.Repositories.Users;
using OrderService.Models.Users;
using OrderService.Services.GeoLocation.GeoCodingClient;
using OrderService.DTO.Orders.CreateOrders;
using OrderService.DTO.Orders;
using OrderService.Services.GeoLocation;
using OrderService.Repositories.Warehouses;
using OrderService.Services.Technicians;

namespace OrderService.Services.Orders
{
    public class OrderServiceClient(
        OrderRepository orderRepository,
        GeoCodingService geoCodingService,
        NearestLocationFinderService nearestLocationFinderService,
        TechnicianRouteSaveService technicianRouteSaveService,
        EquipmentStockRepository equipmentStockRepository,
        UserPostgreRepository userPostgreRepository,
        UserRedisRepository userRedisRepository,
        ILogger<OrderServiceClient> logger)
    {
        private readonly OrderRepository _orderRepository = orderRepository;
        private readonly GeoCodingService _geoCodingService = geoCodingService;
        private readonly UserPostgreRepository _userPostgreRepository = userPostgreRepository;
        private readonly NearestLocationFinderService _nearestLocationFinderService = nearestLocationFinderService;
        private readonly EquipmentStockRepository _equipmentStockRepository = equipmentStockRepository;
        private readonly UserRedisRepository _userRedisRepository = userRedisRepository;
        private readonly TechnicianRouteSaveService _technicianRouteSaveService = technicianRouteSaveService;
        private readonly ILogger<OrderServiceClient> _logger = logger;

        /// <summary>
        /// 📌 Создание заявки клиентом (⚡ Без склада, оборудования, материалов, техников).
        /// </summary>
        public async Task<CreatedOrderResponseDTO?> CreateOrderByClientAsync(CreateOrderRequestForClient request)
        {
            _logger.LogInformation("📌 Клиент {ClientId} создаёт заявку", request.ClientId);

            var user = await _userPostgreRepository.GetUserByIdAsync(request.ClientId ?? Guid.Empty);
            if (user is not Client client)
            {
                _logger.LogError("❌ Ошибка: Клиент с ID {ClientId} не найден!", request.ClientId);
                return null;
            }

            return await CreateOrderInternalAsync(
                request,
                request.OrderType,
                client.Id,
                client.FullName,
                client.PhoneNumber,
                client.Email
            );
        }

        /// <summary>
        /// 🔄 Внутренняя логика создания заявки клиентом
        /// </summary>
        private async Task<CreatedOrderResponseDTO?> CreateOrderInternalAsync(
            CreateOrderRequestForClient request,
            OrderType _,
            Guid clientId,
            string clientName,
            string clientPhone,
            string clientEmail)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrEmpty(clientName))
                throw new ArgumentException("Client name is required", nameof(clientName));
            if (string.IsNullOrEmpty(clientPhone))
                throw new ArgumentException("Client phone is required", nameof(clientPhone));
            if (string.IsNullOrEmpty(clientEmail))
                throw new ArgumentException("Client email is required", nameof(clientEmail));

            _logger.LogInformation("🔄 Создание и автоматическая обработка заявки клиентом...");

            await using var transaction = await _orderRepository.BeginTransactionAsync();

            try
            {
                // 🔍 Геолокация
                if (string.IsNullOrWhiteSpace(request.InstallationAddress))
                {
                    _logger.LogError("❌ Адрес установки не указан");
                    return null;
                }

                var location = await _geoCodingService.GetCoordinatesAsync(request.InstallationAddress);
                if (location == null)
                {
                    _logger.LogError("❌ Невозможно определить координаты для адреса {Address}", request.InstallationAddress);
                    return null;
                }

                // Создание заявки
                var orderId = Guid.NewGuid();
                var order = new Order
                {
                    Id = orderId,
                    OrderType = request.OrderType,
                    FulfillmentStatus = FulfillmentStatus.New,
                    WorkProgress = WorkProgress.OrderPlaced,
                    PaymentStatus = PaymentStatus.UnPaid,
                    PaymentMethod = request.PaymentMethod,
                    CreationOrderDate = DateTime.UtcNow,
                    InstallationDate = request.InstallationDate == default ? DateTime.UtcNow : request.InstallationDate,
                    InstallationAddress = request.InstallationAddress,
                    InstallationLatitude = location.Value.Latitude,
                    InstallationLongitude = location.Value.Longitude,
                    Notes = request.Notes ?? "",
                    WorkCost = 800,
                    ClientID = clientId,
                    ClientName = clientName,
                    ClientPhone = clientPhone,
                    ClientEmail = clientEmail,
                    ClientCalculatedBTU = request.ClientCalculatedBTU,
                    ClientMinBTU = request.ClientMinBTU,
                    ClientMaxBTU = request.ClientMaxBTU,
                    ManagerId = null,
                    Equipment = [],
                    RequiredMaterials = [],
                    RequiredTools = [],
                    AssignedTechnicians = []
                };

                var created = await _orderRepository.CreateOrderAsync(order);
                if (!created)
                {
                    _logger.LogError("❌ Ошибка при создании заявки {OrderId}", orderId);
                    await transaction.RollbackAsync();
                    return null;
                }

                // Подбор оборудования
                var allEquipment = await _equipmentStockRepository.GetAllAsync();
                var bestEquipment = allEquipment
                    .Where(e => e.BTU >= request.ClientMinBTU && e.BTU <= request.ClientMaxBTU && e.Quantity > 0)
                    .OrderBy(e => e.Price)
                    .FirstOrDefault();

                if (bestEquipment == null)
                {
                    _logger.LogWarning("⚠️ Подходящее оборудование не найдено");
                    await transaction.RollbackAsync();
                    return null;
                }

                // Поиск ресурсов и техников
                var nearestData = await _nearestLocationFinderService.FindNearestLocationsAsync(
                    location.Value.Latitude,
                    location.Value.Longitude,
                    request.OrderType,
                    bestEquipment.ModelName,
                    null);

                if (nearestData.NearestWarehouses.Count == 0)
                {
                    _logger.LogWarning("⚠️ Нет складов с нужными ресурсами");
                    await transaction.RollbackAsync();
                    return null;
                }

                // Назначение менеджера
                var defaultManager = await _userPostgreRepository.GetDefaultManagerAsync();
                if (defaultManager != null)
                {
                    order.ManagerId = defaultManager.Id;
                    _logger.LogInformation("✅ Менеджер {ManagerName} назначен для заявки {OrderId}", defaultManager.FullName, orderId);
                }

                // Добавление ресурсов
                order.Equipment.Add(new OrderEquipment
                {
                    ModelName = bestEquipment.ModelName,
                    ModelBTU = bestEquipment.BTU,
                    ServiceArea = bestEquipment.ServiceArea,
                    ModelPrice = bestEquipment.Price,
                    Quantity = bestEquipment.Quantity,
                    ModelSource = "Warehouse",
                    OrderID = order.Id
                });

                order.RequiredMaterials.AddRange(nearestData.AvailableMaterials.Select(m => new OrderRequiredMaterial
                {
                    MaterialName = m.MaterialName,
                    Quantity = m.Quantity,
                    MaterialPrice = m.MaterialPrice,
                    OrderId = order.Id
                }));

                order.RequiredTools.AddRange(nearestData.AvailableTools.Select(t => new OrderRequiredTool
                {
                    ToolName = t.ToolName,
                    Quantity = t.Quantity,
                    OrderId = order.Id
                }));

                order.AssignedTechnicians.AddRange(nearestData.SelectedTechnicians.Select(t => new OrderTechnician
                {
                    TechnicianID = t.Id,
                    OrderID = order.Id
                }));

                // Обновление Redis-локаций
                foreach (var tech in nearestData.SelectedTechnicians)
                {
                    await _userRedisRepository.SetTechnicianLocationAsync(
                        tech.Id, tech.Latitude, tech.Longitude, order.Id);
                }

                // Маршруты
                order.SetInitialRoutes(nearestData.Routes);
                await _technicianRouteSaveService.SaveInitialRoutesAsync(order, nearestData.Routes);

                // Финальное сохранение
                await _orderRepository.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("✅ Заявка {OrderId} создана и обработана автоматически", orderId);

                return new CreatedOrderResponseDTO(order, nearestData.Routes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ошибка при создании и обработке заявки. Откат транзакции...");
                await transaction.RollbackAsync();
                return null;
            }
        }



        /// <summary>
        /// 📜 Получение всех заявок клиента.
        /// </summary>
        public async Task<List<OrderDTO>> GetAllOrdersByClientAsync(Guid clientId)
        {
            var orders = await _orderRepository.GetOrdersByClientIdAsync(clientId);
            return [.. orders.Select(o => new OrderDTO(o))];
        }

        /// <summary>
        /// 🔍 Получение заявки по ID (с деталями).
        /// </summary>
        public async Task<OrderDTO?> GetOrderByIdAsync(Guid orderId)
        {
            var order = await _orderRepository.GetOrderByIdAsync(orderId, includeDetails: true);
            return order == null ? null : new OrderDTO(order);
        }

        /// <summary>
        /// 🗑️ Удаление заявки.
        /// </summary>
        public async Task<bool> DeleteOrderAsync(Guid orderId)
        {
            _logger.LogInformation("🗑️ Удаление заявки {OrderId}", orderId);
            return await _orderRepository.DeleteOrderAsync(orderId);
        }



    }
}