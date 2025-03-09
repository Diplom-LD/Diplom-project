namespace OrderService.Services.GeoLocation
{
    public interface IGeoCodingService
    {
        Task<(double Latitude, double Longitude, string DisplayName)?> GetCoordinatesAsync(string address);
        Task<(double Latitude, double Longitude, string DisplayName)?> GetBestCoordinateAsync(string address);
    }

}
