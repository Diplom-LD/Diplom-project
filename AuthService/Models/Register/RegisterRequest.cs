using System.ComponentModel.DataAnnotations;

namespace AuthService.Models.Register
{
    public class RegisterRequest
    {
        [Required(ErrorMessage = "Login is required")]
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

        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
    }
}
