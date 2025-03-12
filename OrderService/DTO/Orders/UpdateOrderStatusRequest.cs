using OrderService.Models.Enums;

namespace OrderService.DTO.Orders
{
    public class UpdateOrderStatusRequest
    {
        public FulfillmentStatus NewStatus { get; set; }
    }
}
