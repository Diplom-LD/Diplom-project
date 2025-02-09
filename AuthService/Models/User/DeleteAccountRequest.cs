using System.ComponentModel.DataAnnotations;

namespace AuthService.Models.User
{
    public class DeleteAccountRequest
    {
        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = null!;
    }
}
