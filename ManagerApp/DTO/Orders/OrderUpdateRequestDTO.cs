using ManagerApp.Models.Orders;

namespace ManagerApp.DTO.Orders
{
    public class OrderUpdateRequestDTO
    {
        public Guid OrderId { get; set; }
        public Guid? ManagerId { get; set; }
        public string? Notes { get; set; }
        public decimal? WorkCost { get; set; }
        public string? ClientName { get; set; }
        public string? ClientPhone { get; set; }
        public string? ClientEmail { get; set; }
        public PaymentStatus? PaymentStatus { get; set; }
        public PaymentMethod? PaymentMethod { get; set; }
        public FulfillmentStatus? FulfillmentStatus { get; set; }
    }

}
