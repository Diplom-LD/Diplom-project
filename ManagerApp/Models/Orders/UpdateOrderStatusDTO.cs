namespace ManagerApp.Models.Orders
{
    public class UpdateOrderStatusDTO
    {
        public Guid OrderId { get; set; }
        public FulfillmentStatus NewStatus { get; set; }
    }
}
