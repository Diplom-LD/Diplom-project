using Microsoft.AspNetCore.Identity;

namespace AuthService.Models.User
{
    public class User : IdentityUser
    {
        // Custom fields
        public string Role { get; set; } = null!; // Client, Manager, Worker
        public string? FirstName { get; set; } // First name
        public string? LastName { get; set; } // Last name
        public string? Phone { get; set; } // Phone number
        public string? Address { get; set; } // Address (converted to coordinates)

        // Geo
        public double? Latitude { get; set; } // Latitude
        public double? Longitude { get; set; } // Longitude

        // To exit from all devices
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiry { get; set; }
    }

    public class Client : User { }
    public class Manager : User { }
    public class Worker : User { }
}
