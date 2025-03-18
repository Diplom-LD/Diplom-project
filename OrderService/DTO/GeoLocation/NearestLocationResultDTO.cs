using OrderService.DTO.Users;
using OrderService.DTO.Warehouses;
using OrderService.DTO.Orders;

namespace OrderService.DTO.GeoLocation
{
    public class NearestLocationResultDTO
    {
        public List<WarehouseDTO> NearestWarehouses { get; set; }
        public List<TechnicianDTO> SelectedTechnicians { get; set; }
        public List<RouteDTO> Routes { get; set; }

        public List<OrderEquipmentDTO> AvailableEquipment { get; set; }
        public List<OrderMaterialDTO> AvailableMaterials { get; set; }
        public List<OrderToolDTO> AvailableTools { get; set; }

        public NearestLocationResultDTO()
        {
            NearestWarehouses = [];
            SelectedTechnicians = [];
            Routes = [];
            AvailableEquipment = [];
            AvailableMaterials = [];
            AvailableTools = [];
        }
    }
}
