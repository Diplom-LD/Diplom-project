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
        public PaymentMethod PaymentMethod { get; set; }
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.UnPaid;
        public DateTime CreationOrderDate { get; set; } = DateTime.UtcNow;
        public DateTime InstallationDate { get; set; }

        [Required(ErrorMessage = "Адрес установки обязателен.")]
        public required string InstallationAddress { get; set; }

        /// <summary>
        /// 📌 Координаты установки
        /// </summary>
        public double InstallationLatitude { get; set; }
        public double InstallationLongitude { get; set; }

        public string Notes { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue, ErrorMessage = "Стоимость работ должна быть положительной.")]
        public decimal WorkCost { get; set; }

        [NotMapped]
        public decimal EquipmentCost => Equipment.Sum(e => e.ModelPrice * e.Quantity);

        [NotMapped]
        public decimal MaterialsCost => RequiredMaterials.Sum(m => m.MaterialPrice * m.Quantity);

        [NotMapped]
        public decimal TotalCost => WorkCost + EquipmentCost + MaterialsCost;

        [Required]
        public required List<OrderEquipment> Equipment { get; set; } = [];

        [Required]
        public required List<OrderRequiredMaterial> RequiredMaterials { get; set; } = [];

        [Required]
        public required List<OrderRequiredTool> RequiredTools { get; set; } = [];

        [Required]
        public required List<OrderTechnician> AssignedTechnicians { get; set; } = [];

        [Column(TypeName = "jsonb")]
        public string InitialRoutesJson { get; set; } = "[]";

        [Column(TypeName = "jsonb")]
        public string FinalRoutesJson { get; set; } = "[]";

        public virtual List<TechnicianRoute> TechnicianRoutes { get; set; } = [];

        public Guid? ClientID { get; set; }

        [ForeignKey("ClientID")]
        public virtual Client? Client { get; set; }

        [Required(ErrorMessage = "Имя клиента обязательно.")]
        public required string ClientName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Телефон клиента обязателен.")]
        [Phone(ErrorMessage = "Некорректный номер телефона.")]
        public required string ClientPhone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email клиента обязателен.")]
        [EmailAddress(ErrorMessage = "Некорректный email.")]
        public required string ClientEmail { get; set; } = string.Empty;

        /* Посчитанные BTU клиентом*/
        [Range(1000, 300000, ErrorMessage = "BTU должен быть в диапазоне от 1000 до 300000.")]
        public int ClientCalculatedBTU { get; set; }

        [Range(1000, 300000, ErrorMessage = "Минимальный BTU должен быть в диапазоне от 1000 до 300000.")]
        public int ClientMinBTU { get; set; }

        [Range(1000, 300000, ErrorMessage = "Максимальный BTU должен быть в диапазоне от 1000 до 300000.")]
        public int ClientMaxBTU { get; set; }

        public Guid? ManagerId { get; set; }

        [ForeignKey("ManagerId")]
        public virtual Manager? Manager { get; set; }

        public List<RouteDTO> GetInitialRoutes()
        {
            return TryDeserializeRoutes(InitialRoutesJson);
        }

        public void SetInitialRoutes(List<RouteDTO> routes)
        {
            InitialRoutesJson = TrySerializeRoutes(routes);
        }

        public List<RouteDTO> GetFinalRoutes()
        {
            return TryDeserializeRoutes(FinalRoutesJson);
        }

        public void SetFinalRoutes(List<RouteDTO> routes)
        {
            FinalRoutesJson = TrySerializeRoutes(routes);
        }

        private static List<RouteDTO> TryDeserializeRoutes(string json)
        {
            try
            {
                return string.IsNullOrEmpty(json) ? [] : JsonSerializer.Deserialize<List<RouteDTO>>(json) ?? [];
            }
            catch
            {
                return [];
            }
        }

        private static string TrySerializeRoutes(List<RouteDTO> routes)
        {
            return routes is { Count: > 0 } ? JsonSerializer.Serialize(routes) : "[]";
        }
    }

    public class OrderEquipment
    {
        [Key]
        public Guid ID { get; set; } = Guid.NewGuid();

        [ForeignKey("Order")]
        public Guid OrderID { get; set; }

        [Required]
        public required string ModelName { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal ModelPrice { get; set; }

        [Required]
        public required string ModelSource { get; set; } 

        [MaxLength(500)]
        public string? ModelUrl { get; set; } 

        [Range(1000, 300000)]
        public int ModelBTU { get; set; }

        [Range(5, 200)]
        public int ServiceArea { get; set; }

        public int WorkDuration { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; } = 1;

        public virtual Order Order { get; set; } = null!;
    }


    public class OrderRequiredMaterial
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [ForeignKey("Order")]
        public Guid OrderId { get; set; }

        [Required]
        public required string MaterialName { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal MaterialPrice { get; set; }

        public Guid WarehouseId { get; set; }

        public virtual Order Order { get; set; } = null!;
    }

    public class OrderRequiredTool
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [ForeignKey("Order")]
        public Guid OrderId { get; set; }

        [Required]
        public required string ToolName { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        public Guid WarehouseId { get; set; }

        public virtual Order Order { get; set; } = null!;
    }

    public class OrderTechnician
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [ForeignKey("Order")]
        public Guid OrderID { get; set; }

        public virtual Order Order { get; set; } = null!;

        [ForeignKey("Technician")]
        public Guid TechnicianID { get; set; }

        public virtual Technician Technician { get; set; } = null!;
    }
}
