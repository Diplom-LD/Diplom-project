using System.ComponentModel.DataAnnotations;

namespace ManagerApp.Models.AuthRequest
{
    public class LoginRequest
    {
        [Required(ErrorMessage = "Identifier (login or email) is required")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Identifier must be between 3 and 50 characters.")]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$|^[a-zA-Z0-9_-]{3,50}$",
            ErrorMessage = "Identifier must be a valid email or a username (3-50 characters, letters, numbers, hyphens, and underscores).")]
        [Display(Name = "Username or Email")]
        public string Identifier { get; set; } = null!;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = null!;
    }
}
