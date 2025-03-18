namespace OrderService.DTO.Orders.TechnicianLocation
{
    public class TechnicianLocationDTO
    {
        public Guid TechnicianId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public Guid OrderId { get; set; }

        public TechnicianLocationDTO() { }

        public TechnicianLocationDTO(Guid technicianId, double latitude, double longitude, Guid orderId)
        {
            TechnicianId = technicianId;
            Latitude = latitude;
            Longitude = longitude;
            OrderId = orderId; 
        }
    }
}
