using System.ComponentModel.DataAnnotations;

namespace AuthService.Models.Register
{
    public class ManagerRegistrationCodes
    {
        [Key]
        [Required]
        public string Code { get; set; } = null!;

    }
}
