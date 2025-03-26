using OrderService.DTO.GeoLocation;
using System.Text.Json;
using OrderService.Models.Orders;

namespace OrderService.DTO.Orders
{
    public class OrderDTO(Order order)
    {
        public Guid Id { get; set; } = order.Id;
        public string OrderType { get; set; } = order.OrderType.ToString();
        public string FulfillmentStatus { get; set; } = order.FulfillmentStatus.ToString();
        public string WorkProgress { get; set; } = order.WorkProgress.ToString();
        public string PaymentStatus { get; set; } = order.PaymentStatus.ToString();
        public string PaymentMethod { get; set; } = order.PaymentMethod.ToString();
        public DateTimeOffset CreationOrderDate { get; set; } = order.CreationOrderDate;
        public DateTimeOffset InstallationDate { get; set; } = order.InstallationDate;
        public string InstallationAddress { get; set; } = order.InstallationAddress;
        public double InstallationLatitude { get; set; } = order.InstallationLatitude;
        public double InstallationLongitude { get; set; } = order.InstallationLongitude;
        public string Notes { get; set; } = order.Notes;
        public decimal WorkCost { get; set; } = order.WorkCost;
        public decimal EquipmentCost { get; set; } = order.EquipmentCost;
        public decimal MaterialsCost { get; set; } = order.MaterialsCost;
        public decimal TotalCost { get; set; } = order.TotalCost;

        public Guid? ClientID { get; set; } = order.ClientID;
        public string? ClientName { get; set; } = order.ClientName;
        public string? ClientPhone { get; set; } = order.ClientPhone;
        public string? ClientEmail { get; set; } = order.ClientEmail;

        public int ClientCalculatedBTU { get; set; } = order.ClientCalculatedBTU;
        public int ClientMinBTU { get; set; } = order.ClientMinBTU;
        public int ClientMaxBTU { get; set; } = order.ClientMaxBTU;
        public string? ManagerName { get; set; } = order.Manager?.FullName ?? "Not Assigned";
        public Guid? ManagerId { get; set; } = order.ManagerId;
        public List<OrderEquipmentDTO> Equipment { get; set; } = order.Equipment?.Select(e => new OrderEquipmentDTO(e)).ToList() ?? [];
        public List<OrderMaterialDTO> RequiredMaterials { get; set; } = order.RequiredMaterials?.Select(m => new OrderMaterialDTO(m)).ToList() ?? [];
        public List<OrderToolDTO> RequiredTools { get; set; } = order.RequiredTools?.Select(t => new OrderToolDTO(t)).ToList() ?? [];
        public List<OrderTechnicianDTO> AssignedTechnicians { get; set; } = order.AssignedTechnicians?.Select(t => new OrderTechnicianDTO(t)).ToList() ?? [];

        /// <summary>
        /// 🚗 Маршруты перед выполнением заявки.
        /// </summary>
        public List<RouteDTO> InitialRoutes { get; set; } = TryDeserializeRoutes(order.InitialRoutesJson);

        /// <summary>
        /// 🚗 Финальные маршруты после выполнения заявки.
        /// </summary>
        public List<RouteDTO> FinalRoutes { get; set; } = TryDeserializeRoutes(order.FinalRoutesJson);

        /// <summary>
        /// 📌 Метод безопасной десериализации JSON в список маршрутов
        /// </summary>
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
    }

    public class OrderEquipmentDTO
    {
        public string ModelName { get; set; } = string.Empty;
        public decimal ModelPrice { get; set; }
        public string ModelSource { get; set; } = string.Empty; 
        public string? ModelUrl { get; set; } 
        public int ModelBTU { get; set; }
        public int ServiceArea { get; set; }
        public int Quantity { get; set; }

        public OrderEquipmentDTO() { }

        public OrderEquipmentDTO(OrderEquipment equipment)
        {
            ModelName = equipment.ModelName;
            ModelPrice = equipment.ModelPrice;
            ModelSource = equipment.ModelSource;
            ModelUrl = equipment.ModelSource == "Store" ? equipment.ModelUrl : null;
            ModelBTU = equipment.ModelBTU;
            ServiceArea = equipment.ServiceArea;
            Quantity = equipment.Quantity;
        }
    }


    public class OrderMaterialDTO
    {
        public string MaterialName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal MaterialPrice { get; set; }

        public OrderMaterialDTO() { }

        public OrderMaterialDTO(OrderRequiredMaterial material)
        {
            MaterialName = material.MaterialName;
            Quantity = material.Quantity;
            MaterialPrice = material.MaterialPrice;
        }
    }

    public class OrderToolDTO
    {
        public string ToolName { get; set; } = string.Empty;
        public int Quantity { get; set; }

        public OrderToolDTO() { }

        public OrderToolDTO(OrderRequiredTool tool)
        {
            ToolName = tool.ToolName;
            Quantity = tool.Quantity;
        }
    }

    public class OrderTechnicianDTO
    {
        public Guid TechnicianID { get; set; }

        public OrderTechnicianDTO() { }

        public OrderTechnicianDTO(OrderTechnician technician)
        {
            TechnicianID = technician.TechnicianID;
        }
    }
}
