using System.ComponentModel.DataAnnotations;

namespace AuthService.Models.Auth
{
    public class LoginRequest
    {
        [Required(ErrorMessage = "Login or Email is required")]
        public string Identifier { get; set; } = null!; // Может быть логином или email

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = null!;
    }
}
