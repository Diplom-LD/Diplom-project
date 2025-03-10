using OrderService.DTO.Technicians;

namespace OrderService.DTO.GeoLocation
{
    public class NearestLocationResultDTO
    {
        public WarehouseDTO? NearestWarehouse { get; set; }
        public List<TechnicianDTO> SelectedTechnicians { get; set; } = [];
        public List<RouteDTO> Routes { get; set; } = [];
    }
}
