using OrderService.DTO.GeoLocation;
using OrderService.DTO.Users;

namespace OrderService.Services.GeoLocation
{
    public interface IOptimizedRouteService
    {
        Task<List<RouteDTO>> BuildOptimizedRouteAsync(
            double jobLatitude,
            double jobLongitude,
            WarehouseDTO? primaryWarehouse,
            WarehouseDTO? secondaryWarehouse,
            List<TechnicianDTO> technicians);
    }
}
