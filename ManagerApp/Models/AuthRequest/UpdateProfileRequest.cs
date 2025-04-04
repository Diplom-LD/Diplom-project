using System.ComponentModel.DataAnnotations;

namespace ManagerApp.Models.AuthRequest
{
    public class UpdateProfileRequest
    {
        [MaxLength(50)]
        public string? FirstName { get; set; }

        [MaxLength(50)]
        public string? LastName { get; set; }

        [Phone]
        public string? Phone { get; set; }

        [MaxLength(200)]
        public string? Address { get; set; }
    }
}
