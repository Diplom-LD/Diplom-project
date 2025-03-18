using OrderService.Models.Technicians;

namespace OrderService.Models.Users
{
    public class Technician : User
    {
        public bool IsAvailable { get; set; } = true;
        public Guid? CurrentOrderId { get; set; }
        public List<TechnicianAppointment> Appointments { get; set; } = [];
    }
}
