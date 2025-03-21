namespace ManagerApp.DTO.Technicians
{
    public class TechnicianDTO
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Address { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public bool IsAvailable { get; set; }
        public Guid? CurrentOrderId { get; set; }
        public List<TechnicianAppointmentDTO> Appointments { get; set; } = [];
    }

    public class TechnicianAppointmentDTO
    {
        public DateTime Date { get; set; }
        public Guid OrderId { get; set; }
    }

}
