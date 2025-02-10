using System.ComponentModel.DataAnnotations;

namespace AuthService.Models.Register
{
    public class RegisterRequest
    {
        [Required(ErrorMessage = "Login is required")]
        [RegularExpression("^[a-zA-Z0-9_-]{4,20}$", ErrorMessage = "Login must be 4-20 characters and can only contain letters, numbers, hyphens, and underscores.")]
        public string Login { get; set; } = null!;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters long")]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "Role is required")]
        [RegularExpression("^(client|manager|worker)$", ErrorMessage = "Role must be client, manager, or worker")]
        public string Role { get; set; } = null!;

        [RegularExpression("^[a-zA-Zа-яА-ЯёЁ]+$", ErrorMessage = "First name can only contain letters")]
        [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters.")]
        public string? FirstName { get; set; }

        [RegularExpression("^[a-zA-Zа-яА-ЯёЁ]+$", ErrorMessage = "Last name can only contain letters")]
        [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters.")]
        public string? LastName { get; set; }

        [Phone(ErrorMessage = "Invalid phone number format.")]
        [RegularExpression(@"^\+?\d{10,15}$", ErrorMessage = "Phone number must be 10-15 digits and can start with '+'")]
        public string? Phone { get; set; }

        [StringLength(200, MinimumLength = 6, ErrorMessage = "Address must be between 6 and 200 characters.")]
        public string? Address { get; set; }
        public string? RegistrationCode { get; set; }
    }
}
