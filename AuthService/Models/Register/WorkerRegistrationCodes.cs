namespace AuthService.Models.Register
{
    public class WorkerRegistrationCodes
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = null!;
    }
}