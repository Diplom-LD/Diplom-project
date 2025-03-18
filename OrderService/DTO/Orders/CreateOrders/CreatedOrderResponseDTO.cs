using OrderService.Models.Orders;
using OrderService.DTO.GeoLocation;

namespace OrderService.DTO.Orders.CreateOrders
{
    public class CreatedOrderResponseDTO(Order order, List<RouteDTO>? routes)
    {
        public Guid OrderId { get; set; } = order.Id;
        public OrderDTO Order { get; set; } = new OrderDTO(order);
        public List<RouteDTO> Routes { get; set; } = routes ?? []; 
    }
}
