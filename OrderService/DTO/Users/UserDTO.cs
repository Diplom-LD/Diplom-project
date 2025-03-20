using OrderService.Models.Users;

namespace OrderService.DTO.Users
{
    public class UserDTO
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class TechnicianDTO : UserDTO
    {
        public bool IsAvailable { get; set; }
        public Guid? CurrentOrderId { get; set; }

        public TechnicianDTO() { }

        public TechnicianDTO(Technician technician)
        {
            Id = technician.Id;
            Email = technician.Email;
            FullName = technician.FullName;
            Address = technician.Address;
            Latitude = technician.Latitude;
            Longitude = technician.Longitude;
            PhoneNumber = technician.PhoneNumber;
            Role = "Technician";
            IsAvailable = technician.IsAvailable;
            CurrentOrderId = technician.CurrentOrderId;
        }
    }
}