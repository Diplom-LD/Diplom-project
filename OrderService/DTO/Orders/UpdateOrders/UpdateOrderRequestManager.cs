using OrderService.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace OrderService.DTO.Orders.UpdateOrders
{
    public class UpdateOrderRequestManager
    {
        [Required(ErrorMessage = "Идентификатор заявки обязателен.")]
        public Guid OrderId { get; set; } 

        [Required(ErrorMessage = "ManagerId обязателен.")]
        public Guid ManagerId { get; set; }

        public string? InstallationAddress { get; set; }

        public DateTimeOffset? InstallationDate { get; set; } 

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public PaymentMethod? PaymentMethod { get; set; }

        public string? Notes { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Стоимость работ должна быть положительной.")]
        public decimal? WorkCost { get; set; }

        public List<Guid>? TechnicianIds { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public FulfillmentStatus? FulfillmentStatus { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public OrderType? OrderType { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public PaymentStatus? PaymentStatus { get; set; }

        public string? ClientName { get; set; }

        [Phone(ErrorMessage = "Некорректный номер телефона.")]
        public string? ClientPhone { get; set; }

        [EmailAddress(ErrorMessage = "Некорректный email.")]
        public string? ClientEmail { get; set; }

        public EquipmentDTO? Equipment { get; set; } 

        public class EquipmentDTO
        {
            public string? ModelName { get; set; }

            public string? ModelSource { get; set; } 

            [Range(1000, 300000, ErrorMessage = "BTU должен быть в диапазоне 1000-300000.")]
            public int? BTU { get; set; } 

            [Range(5, 200, ErrorMessage = "Площадь обслуживания должна быть в диапазоне 5-200 м².")]
            public int? ServiceArea { get; set; } 

            [Range(0.01, double.MaxValue, ErrorMessage = "Цена оборудования должна быть положительной.")]
            public decimal? Price { get; set; } 

            [Range(1, int.MaxValue, ErrorMessage = "Количество должно быть не менее 1.")]
            public int? Quantity { get; set; } 
        }
    }
}
