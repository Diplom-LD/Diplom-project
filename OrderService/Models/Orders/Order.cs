using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using OrderService.DTO.GeoLocation;
using OrderService.Models.Enums;
using OrderService.Models.Users;

namespace OrderService.Models.Orders
{
    public class Order
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public OrderType OrderType { get; set; }
        public FulfillmentStatus FulfillmentStatus { get; set; } = FulfillmentStatus.New;
        public WorkProgress WorkProgress { get; set; } = WorkProgress.OrderPlaced;
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.UnPaid;
        public PaymentMethod PaymentMethod { get; set; }
        public DateTime CreationOrderDate { get; set; } = DateTime.UtcNow;
        public DateTime InstallationDate { get; set; }
        public string InstallationAddress { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public decimal WorkCost { get; set; }
        public decimal EquipmentCost { get; set; }
        public decimal TotalCost { get; set; }

        public List<OrderTechnician> AssignedTechnicians { get; set; } = [];
        public List<OrderEquipment> Equipment { get; set; } = [];
        public List<TechnicianRoute> TechnicianRoutes { get; set; } = [];

        public string InitialRoutesJson { get; set; } = string.Empty; 
        public string FinalRoutesJson { get; set; } = string.Empty; 

        [ForeignKey("Client")]
        public Guid ClientID { get; set; }
        public virtual Client Client { get; set; } = null!;

        [ForeignKey("Manager")]
        public Guid ManagerId { get; set; }
        public virtual Manager Manager { get; set; } = null!;

        /// <summary>
        /// 📌 Получить сохранённые первоначальные маршруты из JSON
        /// </summary>
        public List<RouteDTO> GetInitialRoutes()
        {
            return string.IsNullOrEmpty(InitialRoutesJson)
                ? []
                : JsonSerializer.Deserialize<List<RouteDTO>>(InitialRoutesJson) ?? [];
        }

        /// <summary>
        /// 📌 Сохранить первоначальные маршруты в JSON
        /// </summary>
        public void SetInitialRoutes(List<RouteDTO> routes)
        {
            InitialRoutesJson = JsonSerializer.Serialize(routes);
        }

        /// <summary>
        /// 📌 Получить сохранённые финальные маршруты из JSON
        /// </summary>
        public List<RouteDTO> GetFinalRoutes()
        {
            return string.IsNullOrEmpty(FinalRoutesJson)
                ? []
                : JsonSerializer.Deserialize<List<RouteDTO>>(FinalRoutesJson) ?? [];
        }

        /// <summary>
        /// 📌 Сохранить финальные маршруты в JSON
        /// </summary>
        public void SetFinalRoutes(List<RouteDTO> routes)
        {
            FinalRoutesJson = JsonSerializer.Serialize(routes);
        }
    }
}
