using OrderService.Models.Technicians;

namespace OrderService.Models.Users
{
    public class Technician : User
    {
        public new double Latitude { get; set; }
        public new double Longitude { get; set; }
        public bool IsAvailable { get; set; } = true;
        public Guid? CurrentOrderId { get; set; }
        public List<TechnicianAppointment> Appointments { get; set; } = [];
    }
}
