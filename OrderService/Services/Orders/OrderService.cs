using OrderService.Models.Enums;
using OrderService.Models.Orders;
using OrderService.Repositories.Orders;
using OrderService.Services.GeoLocation;
using OrderService.DTO.Orders;
using OrderService.Services.Technicians;
using Microsoft.Extensions.Logging;
using OrderService.Models.Warehouses;

namespace OrderService.Services.Orders
{
    public class OrderService(
        OrderRepository orderRepository,
        GeoCodingService geoCodingService,
        TechnicianAvailabilityService technicianAvailabilityService,
        EquipmentService equipmentService,
        NearestLocationFinderService nearestLocationFinderService,
        TechnicianRouteSaveService technicianRouteSaveService,
        ILogger<OrderService> logger)
    {
        private readonly OrderRepository _orderRepository = orderRepository;
        private readonly GeoCodingService _geoCodingService = geoCodingService;
        private readonly TechnicianAvailabilityService _technicianAvailabilityService = technicianAvailabilityService;
        private readonly EquipmentService _equipmentService = equipmentService;
        private readonly NearestLocationFinderService _nearestLocationFinderService = nearestLocationFinderService;
        private readonly TechnicianRouteSaveService _technicianRouteSaveService = technicianRouteSaveService;
        private readonly ILogger<OrderService> _logger = logger;

        /// <summary>
        /// 📌 Создание заявки
        /// </summary>
        public async Task<CreatedOrderResponseDTO?> CreateOrderAsync(CreateOrderRequest request)
        {
            _logger.LogInformation("📌 Создание заявки для клиента {ClientId}", request.ClientId);

            // 1️⃣ Конвертируем адрес в координаты
            var location = await _geoCodingService.GetCoordinatesAsync(request.InstallationAddress);
            if (location == null)
            {
                _logger.LogError("❌ Ошибка: Невозможно определить координаты для {Address}", request.InstallationAddress);
                return null;
            }

            // 2️⃣ Определяем склад(-ы), наличие ресурсов и ближайших техников
            var nearestLocationData = await _nearestLocationFinderService.FindNearestLocationWithRoutesAsync(
                location.Value.Latitude, location.Value.Longitude,
                request.OrderType, request.RequiredBTU,
                request.TechnicianIds?.Select(id => id.ToString()).ToList(),
                request.TechnicianCount);

            if (nearestLocationData.NearestWarehouse == null)
            {
                _logger.LogError("❌ Не найден склад с необходимыми ресурсами для заявки!");
                return null;
            }

            if (nearestLocationData.SelectedTechnicians.Count == 0)
            {
                _logger.LogError("❌ Нет доступных техников для выполнения заявки!");
                return null;
            }

            _logger.LogInformation("✅ Найден склад {WarehouseName} и {TechnicianCount} техников",
                nearestLocationData.NearestWarehouse.Name, nearestLocationData.SelectedTechnicians.Count);

            // 3️⃣ Получаем оборудование и материалы со склада
            List<EquipmentDTO> selectedEquipment;

            if (request.UseWarehouseEquipment)
            {
                var equipmentStock = await _equipmentService.GetEquipmentForOrderAsync(request, nearestLocationData.NearestWarehouse.Id.ToString());
                selectedEquipment = [.. equipmentStock.Select(e => new EquipmentDTO
        {
            Id = e.ID,
            ModelName = e.ModelName,
            BTU = e.BTU,
            ServiceArea = e.ServiceArea,
            Price = e.Price,
            Quantity = e.Quantity
        })];
            }
            else
            {
                selectedEquipment = [];
            }

            var equipmentStockList = selectedEquipment.Select(e => new EquipmentStock
            {
                ID = e.Id,
                ModelName = e.ModelName,
                BTU = e.BTU,
                ServiceArea = e.ServiceArea,
                Price = e.Price,
                Quantity = e.Quantity,
                WarehouseId = nearestLocationData.NearestWarehouse.Id
            }).ToList();

            var selectedMaterials = await _equipmentService.GetRequiredMaterials(equipmentStockList, nearestLocationData.NearestWarehouse.Id.ToString());

            // 4️⃣ Создаём заявку
            var order = new Order
            {
                OrderType = request.OrderType,
                FulfillmentStatus = FulfillmentStatus.New,
                WorkProgress = WorkProgress.OrderPlaced,
                PaymentStatus = request.PaymentStatus,
                PaymentMethod = request.PaymentMethod,
                CreationOrderDate = DateTime.UtcNow,
                InstallationDate = request.InstallationDate,
                InstallationAddress = request.InstallationAddress,
                WorkCost = request.WorkCost,
                EquipmentCost = request.EquipmentCost,
                TotalCost = request.WorkCost + request.EquipmentCost,
                AssignedTechnicians = [.. nearestLocationData.SelectedTechnicians.Select(t => new OrderTechnician { TechnicianID = t.Id })],
                ClientID = request.ClientId,
                ManagerId = request.ManagerId,
                Equipment = [.. selectedEquipment
            .Select(e => new OrderEquipment
            {
                ID = Guid.NewGuid(),
                ModelName = e.ModelName,
                ModelPrice = e.Price,
                ModelBTU = e.BTU,
                ServiceArea = e.ServiceArea,
                WorkDuration = 0,
                ModelSource = "Warehouse",
                ToolsAndMaterialsRequired = string.Join(", ", selectedMaterials.Select(m => m.MaterialName))
            })]
            };

            await _orderRepository.CreateOrderAsync(order);

            // 5️⃣ Сохранение маршрутов
            if (nearestLocationData.Routes.Count != 0)
            {
                await _technicianRouteSaveService.SaveInitialRoutesAsync(order.Id, nearestLocationData.Routes);
                _logger.LogInformation("✅ Первоначальные маршруты для заявки {OrderId} успешно сохранены.", order.Id);
            }

            // ✅ Возвращаем заказ + маршруты
            return new CreatedOrderResponseDTO(order, nearestLocationData.Routes);
        }


        /// <summary>
        /// Обновление статуса заявки
        /// </summary>
        public async Task<bool> UpdateOrderStatusAsync(Guid orderId, FulfillmentStatus newStatus)
        {
            var order = await _orderRepository.GetOrderByIdAsync(orderId);
            if (order == null)
            {
                _logger.LogError("❌ Ошибка: Заявка {OrderId} не найдена!", orderId);
                return false;
            }

            order.FulfillmentStatus = newStatus;
            await _orderRepository.UpdateOrderAsync(order);

            _logger.LogInformation("✅ Статус заявки {OrderId} обновлён до {NewStatus}", orderId, newStatus);
            return true;
        }

        /// <summary>
        /// Получение заявки по ID
        /// </summary>
        public async Task<Order?> GetOrderByIdAsync(Guid orderId)
        {
            var order = await _orderRepository.GetOrderByIdAsync(orderId);
            if (order == null)
            {
                _logger.LogWarning("❌ Заявка с ID {OrderId} не найдена!", orderId);
            }
            return order;
        }

        /// <summary>
        /// Получение всех заявок
        /// </summary>
        public async Task<List<Order>> GetAllOrdersAsync()
        {
            var orders = await _orderRepository.GetAllOrdersAsync();
            _logger.LogInformation("📌 Получено {OrderCount} заявок", orders.Count);
            return orders;
        }

        /// <summary>
        /// Удаление заявки по ID
        /// </summary>
        public async Task<bool> DeleteOrderAsync(Guid orderId)
        {
            var order = await _orderRepository.GetOrderByIdAsync(orderId);
            if (order == null)
            {
                _logger.LogWarning("❌ Заявка с ID {OrderId} не найдена!", orderId);
                return false;
            }

            bool result = await _orderRepository.DeleteOrderAsync(orderId);
            if (result)
            {
                _logger.LogInformation("✅ Заявка с ID {OrderId} успешно удалена", orderId);
            }
            return result;
        }
    }
}
