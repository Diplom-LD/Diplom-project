namespace ManagerApp.Models.AuthRequest
{
    public class AuthViewModel
    {
        public LoginRequest LoginModel { get; set; }
        public RegisterRequest RegisterModel { get; set; }

        public AuthViewModel()
        {
            LoginModel = new LoginRequest();
            RegisterModel = new RegisterRequest();
        }
    }
}
