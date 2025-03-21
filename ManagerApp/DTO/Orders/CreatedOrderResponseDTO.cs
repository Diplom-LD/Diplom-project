using System.Text.Json.Serialization;
using ManagerApp.Models.Orders;

namespace ManagerApp.DTO.Orders
{

    public class CreatedOrderResponseDTO
    {
        [JsonPropertyName("orderId")]
        public Guid OrderId { get; set; }

        [JsonPropertyName("order")]
        public OrderResponse Order { get; set; } = null!;

        [JsonPropertyName("routes")]
        public List<OrderResponse.RouteDTO> Routes { get; set; } = [];
    }
}
