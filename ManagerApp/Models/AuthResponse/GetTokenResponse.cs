namespace ManagerApp.Models.AuthResponse
{
    public class GetTokenResponse
    {
        public string UserId { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Role { get; set; } = null!;
    }
}
