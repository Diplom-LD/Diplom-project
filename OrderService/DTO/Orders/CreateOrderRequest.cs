using System.Text.Json.Serialization;
using OrderService.Models.Enums;

namespace OrderService.DTO.Orders
{
    public class CreateOrderRequest
    {
        [JsonConverter(typeof(JsonStringEnumConverter))] 
        public OrderType OrderType { get; set; } // Тип заявки (установка, обслуживание)

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public FulfillmentStatus FulfillmentStatus { get; set; } // Статус выполнения

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public PaymentStatus PaymentStatus { get; set; } // Статус оплаты

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public PaymentMethod PaymentMethod { get; set; } // Метод оплаты

        public DateTime InstallationDate { get; set; } // Дата установки
        public required string InstallationAddress { get; set; } // Адрес установки
        public decimal WorkCost { get; set; } // Стоимость работ
        public decimal EquipmentCost { get; set; } // Стоимость оборудования
        public int? RequiredBTU { get; set; } // Требуемый BTU (если есть)
        public List<Guid>? TechnicianIds { get; set; } // Список ID техников
        public int TechnicianCount { get; set; } = 2; // Количество техников (по умолчанию 2)
        public bool UseWarehouseEquipment { get; set; } // Использовать оборудование со склада?

        public Guid ClientId { get; set; } // ID клиента
        public Guid ManagerId { get; set; } // ID менеджера
    }
}
