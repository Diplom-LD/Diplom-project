using OrderService.Models.Orders;

namespace OrderService.Models.Users
{
    public class Client : User
    {
        public List<Order> Orders { get; set; } = [];
    }
}
