using OrderService.Models.Enums;
using OrderService.Models.Orders;
using OrderService.Repositories.Orders;
using OrderService.Repositories.Users;
using OrderService.Models.Users;
using OrderService.Services.GeoLocation.GeoCodingClient;
using OrderService.DTO.Orders.CreateOrders;
using OrderService.DTO.Orders;

namespace OrderService.Services.Orders
{
    public class OrderServiceClient(
        OrderRepository orderRepository,
        GeoCodingService geoCodingService,
        UserPostgreRepository userPostgreRepository,
        ILogger<OrderServiceClient> logger)
    {
        private readonly OrderRepository _orderRepository = orderRepository;
        private readonly GeoCodingService _geoCodingService = geoCodingService;
        private readonly UserPostgreRepository _userPostgreRepository = userPostgreRepository;
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
            {
                throw new ArgumentException($"'{nameof(clientName)}' cannot be null or empty.", nameof(clientName));
            }

            if (string.IsNullOrEmpty(clientPhone))
            {
                throw new ArgumentException($"'{nameof(clientPhone)}' cannot be null or empty.", nameof(clientPhone));
            }

            if (string.IsNullOrEmpty(clientEmail))
            {
                throw new ArgumentException($"'{nameof(clientEmail)}' cannot be null or empty.", nameof(clientEmail));
            }

            _logger.LogInformation("🔄 Создание заявки клиентом...");

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

                // 2️⃣ Создание заявки
                var orderId = Guid.NewGuid();
                _logger.LogInformation("📌 Создаётся заявка {OrderId} от клиента {ClientName} ({ClientPhone})",
                    orderId, clientName, clientPhone);

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
                    Notes = request.Notes ?? string.Empty,
                    WorkCost = 0,
                    ClientID = clientId,
                    ClientName = clientName,
                    ClientPhone = clientPhone,
                    ClientEmail = clientEmail,
                    ManagerId = null,
                    Manager = null,
                    Equipment = [],
                    RequiredMaterials = [],
                    RequiredTools = [],
                    AssignedTechnicians = [],
                    ClientCalculatedBTU = request.ClientCalculatedBTU,
                    ClientMinBTU = request.ClientMinBTU,
                    ClientMaxBTU = request.ClientMaxBTU
                };

                // 3️⃣ Сохранение заявки в БД
                var created = await _orderRepository.CreateOrderAsync(order);
                if (!created)
                {
                    _logger.LogError("❌ Ошибка при создании заявки {OrderId}!", orderId);
                    await transaction.RollbackAsync();
                    return null;
                }

                _logger.LogInformation("✅ Заявка {OrderId} успешно создана.", orderId);

                // 4️⃣ Фиксация транзакции
                await _orderRepository.SaveChangesAsync();
                await transaction.CommitAsync();
                _logger.LogInformation("✅ Заявка {OrderId} сохранена в БД.", orderId);

                return new CreatedOrderResponseDTO(order, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ошибка при создании заявки клиентом. Откат транзакции...");
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
        /// 🔍 Получение заявки по ID.
        /// </summary>
        public async Task<OrderDTO?> GetOrderByIdAsync(Guid orderId)
        {
            var order = await _orderRepository.GetOrderByIdAsync(orderId);
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
