namespace AuthService.Models.Register
{
    public class ManagerRegistrationCodes
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = null!;
    }
}