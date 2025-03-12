using OrderService.DTO.Users;

namespace OrderService.DTO.GeoLocation
{
    public class NearestLocationResultDTO
    {
        public WarehouseDTO? NearestWarehouse { get; set; }  
        public WarehouseDTO? SecondaryWarehouse { get; set; } 
        public List<TechnicianDTO> SelectedTechnicians { get; set; } = [];
        public List<RouteDTO> Routes { get; set; } = [];
    }
}
