using OrderService.DTO.GeoLocation;
using OrderService.DTO.Users;
using OrderService.DTO.Warehouses;

namespace OrderService.Services.GeoLocation
{
    public interface IOptimizedRouteService
    {
        /// <summary>
        /// ✅ Строит маршрут техника с учетом всех ресурсов (оборудование, материалы, инструменты)
        /// </summary>
        Task<List<RouteDTO>> BuildOptimizedRouteAsync(
            double jobLatitude, double jobLongitude,
            List<WarehouseDTO> warehouses,
            List<TechnicianDTO> technicians);
    }
}
