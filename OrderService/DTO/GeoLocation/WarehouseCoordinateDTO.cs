namespace OrderService.DTO.GeoLocation
{
    public class WarehouseCoordinateDTO
    {
        public Guid WarehouseId { get; set; }
        public string Name { get; set; } = null!;
        public string Address { get; set; } = null!;
        public string ContactPerson { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class TechnicianCoordinateDTO
    {
        public Guid TechnicianId { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Address { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
