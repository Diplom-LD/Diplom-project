using System.ComponentModel.DataAnnotations;

namespace AuthService.Models.User
{
    public class UpdateProfileRequest
    {
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
    }
}
