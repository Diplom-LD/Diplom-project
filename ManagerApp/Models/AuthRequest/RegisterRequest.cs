using System.ComponentModel.DataAnnotations;

namespace ManagerApp.Models.AuthRequest
{
    public class RegisterRequest
    {
        [Required(ErrorMessage = "Login is required")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Login must be between 3 and 50 characters.")]
        [RegularExpression("^[a-zA-Z0-9_-]{3,50}$", ErrorMessage = "Login can only contain letters, numbers, hyphens, and underscores.")]
        [Display(Name = "Username")]
        public string Login { get; set; } = null!;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 100 characters.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "Confirm password is required")]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = null!;

        [Required(ErrorMessage = "First name is required")]
        [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters.")]
        [RegularExpression("^[a-zA-Zа-яА-ЯёЁ]+$", ErrorMessage = "First name can only contain letters.")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = null!;

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters.")]
        [RegularExpression("^[a-zA-Zа-яА-ЯёЁ]+$", ErrorMessage = "Last name can only contain letters.")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = null!;

        [Required(ErrorMessage = "Phone is required")]
        [RegularExpression(@"^\+?\d{10,15}$", ErrorMessage = "Phone number must be 10-15 digits and can start with '+'")]
        [Display(Name = "Phone Number")]
        public string Phone { get; set; } = null!;

        [Required(ErrorMessage = "Address is required")]
        [StringLength(200, MinimumLength = 6, ErrorMessage = "Address must be between 6 and 200 characters.")]
        [Display(Name = "Address")]
        public string Address { get; set; } = null!;

        [Display(Name = "Registration Code")]
        public string? RegistrationCode { get; set; }

        // When scalable, role selection will occur in the start view
        [Required(ErrorMessage = "Role is required")]
        public string Role { get; set; } = "manager";

    }
}
