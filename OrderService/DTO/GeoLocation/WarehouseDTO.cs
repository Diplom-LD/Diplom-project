namespace OrderService.DTO.GeoLocation
{
    public class WarehouseDTO
    {
        public string Id { get; set; } = string.Empty; 
        public string Name { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

}
