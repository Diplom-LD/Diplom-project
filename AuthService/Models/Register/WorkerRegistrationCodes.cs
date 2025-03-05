using System.ComponentModel.DataAnnotations;

namespace AuthService.Models.Register
{
    public class WorkerRegistrationCodes
    {
        [Key]
        [Required]
        public string Code { get; set; } = null!;
    }
}
