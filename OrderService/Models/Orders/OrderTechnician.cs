using System.ComponentModel.DataAnnotations.Schema;

namespace OrderService.Models.Orders
{
    public class OrderTechnician
    {
        [ForeignKey("Order")]
        public Guid OrderID { get; set; }

        public virtual Order Order { get; set; } = null!;

        public Guid TechnicianID { get; set; }
    }
}
