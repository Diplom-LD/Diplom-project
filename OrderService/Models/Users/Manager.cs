using OrderService.Models.Orders;

namespace OrderService.Models.Users
{
    public class Manager : User
    {
        public List<Order> ManagedOrders { get; set; } = [];
    }
}
