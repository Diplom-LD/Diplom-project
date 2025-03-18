using OrderService.DTO.GeoLocation;

namespace OrderService.Services.GeoLocation.RouteServiceClient
{
    public interface IRouteServiceClient
    {
        Task<RouteDTO?> GetRouteAsync(double startLat, double startLon, double endLat, double endLon);
    }
}
