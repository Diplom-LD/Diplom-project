using OrderService.DTO.GeoLocation;
using OrderService.Models.Orders;

namespace OrderService.DTO.Orders
{
    public class CreatedOrderResponseDTO(Order order, List<RouteDTO> routes)
    {
        public Order Order { get; set; } = order;
        public List<RouteDTO> Routes { get; set; } = routes;
    }
}
