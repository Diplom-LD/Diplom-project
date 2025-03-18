using OrderService.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace OrderService.DTO.Orders.UpdateOrders
{
    public class UpdateOrderStatusDTO
    {
        [Required(ErrorMessage = "Идентификатор заявки обязателен.")]
        public Guid OrderId { get; set; }

        [Required(ErrorMessage = "Новый статус заявки обязателен.")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public FulfillmentStatus NewStatus { get; set; }

        [Required(ErrorMessage = "Этап выполнения обязателен.")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public WorkProgress NewWorkProgress { get; set; }
    }
}