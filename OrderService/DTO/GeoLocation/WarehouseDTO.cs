namespace OrderService.DTO.GeoLocation
{
    public class WarehouseDTO
    {
        public Guid Id { get; set; } 
        public string Name { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
