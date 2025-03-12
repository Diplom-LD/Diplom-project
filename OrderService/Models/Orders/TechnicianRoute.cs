using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using OrderService.DTO.GeoLocation;
using OrderService.Models.Users;

namespace OrderService.Models.Orders
{
    public class TechnicianRoute
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [ForeignKey("Order")]
        public Guid OrderId { get; set; }
        public virtual Order Order { get; set; } = null!;

        [ForeignKey("Technician")]
        public Guid TechnicianId { get; set; }
        public virtual Technician Technician { get; set; } = null!;

        public bool IsFinalRoute { get; set; }
        public string RouteJson { get; set; } = string.Empty;

        public List<RouteDTO> GetRoute()
        {
            return string.IsNullOrEmpty(RouteJson)
                ? []
                : JsonSerializer.Deserialize<List<RouteDTO>>(RouteJson) ?? [];
        }

        public void SetRoute(List<RouteDTO> route)
        {
            RouteJson = JsonSerializer.Serialize(route);
        }
    }
}
