using Microsoft.AspNetCore.Identity;

namespace AuthService.Models.User
{
    public class User : IdentityUser
    {
        // Custom fields
        public string Role { get; set; } = null!; // Role of the user (Client, Manager, Worker)
        public string? FirstName { get; set; } // First name of the user
        public string? LastName { get; set; } // Last name of the user
        public string? Phone { get; set; } // Phone number (inherited from IdentityUser)
        public string? Address { get; set; } // Address of the user
    }

    // IdentityUser already includes:
    // - Id (GUID) -> Unique user identifier
    // - UserName -> Login name
    // - Email -> User's email
    // - PhoneNumber -> User's phone number
    // - PasswordHash -> Hashed password
    // - SecurityStamp -> Security token for authentication
    // - ConcurrencyStamp -> Used for optimistic concurrency checks
    // - LockoutEnabled, AccessFailedCount, LockoutEnd -> Account lockout settings

    public class Client : User { }

    public class Manager : User { }

    public class Worker : User { }
}
