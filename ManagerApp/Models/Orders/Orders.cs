using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ManagerApp.Models.Orders
{
    public class OrderRequest
    {
        [Required(ErrorMessage = "Тип заявки обязателен.")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public OrderType OrderType { get; set; }

        [Required(ErrorMessage = "Дата установки обязательна.")]
        public DateTimeOffset InstallationDate { get; set; }

        [Required(ErrorMessage = "Адрес установки обязателен.")]
        [StringLength(255, MinimumLength = 5, ErrorMessage = "Адрес должен содержать от 5 до 255 символов.")]
        public string InstallationAddress { get; set; } = null!;

        [Required(ErrorMessage = "Менеджер обязателен.")]
        public Guid ManagerId { get; set; }

        [Required(ErrorMessage = "Способ оплаты обязателен.")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public PaymentMethod PaymentMethod { get; set; }

        [Required(ErrorMessage = "Оборудование обязательно.")]
        public EquipmentDTO Equipment { get; set; } = null!;

        public List<Guid>? TechnicianIds { get; set; }

        [Required(ErrorMessage = "Имя клиента обязательно.")]
        public string FullName { get; set; } = null!;

        [Required(ErrorMessage = "Номер телефона обязателен.")]
        [Phone(ErrorMessage = "Некорректный номер телефона.")]
        public string PhoneNumber { get; set; } = null!;

        [Required(ErrorMessage = "Email обязателен.")]
        [EmailAddress(ErrorMessage = "Некорректный email.")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Статус выполнения обязателен.")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public FulfillmentStatus FulfillmentStatus { get; set; }

        [Required(ErrorMessage = "Статус оплаты обязателен.")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public PaymentStatus PaymentStatus { get; set; }

        [Required(ErrorMessage = "Стоимость работ обязательна.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Стоимость работ должна быть положительной.")]
        public decimal WorkCost { get; set; }

        public class EquipmentDTO
        {
            [Required(ErrorMessage = "Название модели обязательно.")]
            public string ModelName { get; set; } = null!;

            [Required(ErrorMessage = "Источник модели обязателен.")]
            public string ModelSource { get; set; } = null!;
            public string? ModelUrl { get; set; }  

            [Required(ErrorMessage = "BTU обязателен.")]
            [Range(1000, 300000, ErrorMessage = "BTU должен быть в диапазоне 1000-300000.")]
            public int BTU { get; set; }

            [Required(ErrorMessage = "Площадь обслуживания обязательна.")]
            [Range(5, 200, ErrorMessage = "Площадь обслуживания должна быть в диапазоне 5-200 м².")]
            public int ServiceArea { get; set; }

            [Required(ErrorMessage = "Цена оборудования обязательна.")]
            [Range(0.01, double.MaxValue, ErrorMessage = "Цена оборудования должна быть положительной.")]
            public decimal Price { get; set; }

            [Required(ErrorMessage = "Количество оборудования обязательно.")]
            [Range(1, int.MaxValue, ErrorMessage = "Количество должно быть не менее 1.")]
            public int Quantity { get; set; } = 1;
        }
    }
    public class OrderResponse
    {
        public Guid Id { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public OrderType OrderType { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public FulfillmentStatus FulfillmentStatus { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public WorkProgress WorkProgress { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public PaymentStatus PaymentStatus { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public PaymentMethod PaymentMethod { get; set; }

        public DateTimeOffset CreationOrderDate { get; set; }
        public DateTimeOffset InstallationDate { get; set; }
        public string InstallationAddress { get; set; } = null!;
        public double InstallationLatitude { get; set; }
        public double InstallationLongitude { get; set; }
        public string Notes { get; set; } = string.Empty;
        public decimal WorkCost { get; set; }
        public decimal EquipmentCost { get; set; }
        public decimal MaterialsCost { get; set; }
        public decimal TotalCost { get; set; }

        public Guid? ClientID { get; set; }
        public string? ClientName { get; set; }
        public string? ClientPhone { get; set; }
        public string? ClientEmail { get; set; }
        public string? ManagerName { get; set; }
        public Guid? ManagerId { get; set; }

        public List<OrderEquipmentDTO> Equipment { get; set; } = [];
        public List<OrderMaterialDTO> RequiredMaterials { get; set; } = [];
        public List<OrderToolDTO> RequiredTools { get; set; } = [];
        public List<OrderTechnicianDTO> AssignedTechnicians { get; set; } = [];
        public List<RouteDTO> InitialRoutes { get; set; } = [];
        public List<RouteDTO> FinalRoutes { get; set; } = [];

        public class OrderEquipmentDTO
        {
            public string ModelName { get; set; } = null!;
            public string ModelSource { get; set; } = null!;
            public int ModelBTU { get; set; }
            public int ServiceArea { get; set; }
            public decimal ModelPrice { get; set; }
            public int Quantity { get; set; }
            public string? ModelUrl { get; set; }
        }

        public class OrderMaterialDTO
        {
            public string MaterialName { get; set; } = null!;
            public int Quantity { get; set; }
            public decimal MaterialPrice { get; set; }
        }

        public class OrderToolDTO
        {
            public string ToolName { get; set; } = null!;
            public int Quantity { get; set; }
        }

        public class OrderTechnicianDTO
        {
            public Guid TechnicianID { get; set; }
        }

        public class RouteDTO
        {
            public Guid TechnicianId { get; set; }
            public string TechnicianName { get; set; } = null!;
            public string PhoneNumber { get; set; } = null!;
            public double Distance { get; set; }
            public double Duration { get; set; }
            public List<RoutePoint> RoutePoints { get; set; } = [];
            public bool IsViaWarehouse { get; set; }
        }

        public class RoutePoint
        {
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public bool IsStopPoint { get; set; }
        }
    }

}
