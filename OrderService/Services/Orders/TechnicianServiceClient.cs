using OrderService.DTO.Orders;
using OrderService.Models.Orders;
using OrderService.Repositories.Orders;

namespace OrderService.Services.Orders
{
    public class TechnicianServiceClient(OrderRepository orderRepository, ILogger<TechnicianServiceClient> logger)
    {
        private readonly OrderRepository _orderRepository = orderRepository;
        private readonly ILogger<TechnicianServiceClient> _logger = logger;

        /// <summary>
        /// 📦 Получение всех заявок, в которых задействован техник
        /// </summary>
        public async Task<List<OrderDTO>> GetOrdersForTechnicianAsync(Guid technicianId)
        {
            _logger.LogInformation("📦 Получение заявок для техника {TechnicianId}", technicianId);

            var orders = await _orderRepository.GetOrdersByTechnicianIdAsync(technicianId);

            return [.. orders.Select(o => new OrderDTO(o))];
        }

    }
}
