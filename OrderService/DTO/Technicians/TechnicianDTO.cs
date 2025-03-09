namespace OrderService.DTO.Technicians
{
    public class TechnicianDTO
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public bool IsAvailable { get; set; }
        public string? CurrentOrderId { get; set; }
    }
}
