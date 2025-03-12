using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderService.Models.Orders
{
    public class OrderEquipment
    {
        [Key]
        public Guid ID { get; set; } = Guid.NewGuid();

        [ForeignKey("Order")]
        public Guid OrderID { get; set; }

        public string ModelName { get; set; } = string.Empty;
        public decimal ModelPrice { get; set; }
        public string ModelSource { get; set; } = string.Empty;
        public int ModelBTU { get; set; }
        public int ServiceArea { get; set; }
        public int WorkDuration { get; set; }
        public string ToolsAndMaterialsRequired { get; set; } = string.Empty;

        public virtual Order Order { get; set; } = null!;
    }
}
