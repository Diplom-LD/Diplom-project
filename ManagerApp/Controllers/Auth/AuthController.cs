using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using ManagerApp.Models.AuthRequest;
using ManagerApp.Models.AuthResponse;
using ManagerApp.Clients;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ManagerApp.Controllers.Auth
{
    public class AuthController : Controller
    {
        private readonly AuthServiceClient _authServiceClient;
        private readonly ILogger<AuthController> _logger;

        public AuthController(AuthServiceClient authServiceClient, ILogger<AuthController> logger)
        {
            _authServiceClient = authServiceClient;
            _logger = logger;
        }

        public IActionResult Auth()
        {
            return View(new AuthViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SignIn(LoginRequest model)
        {

            Console.WriteLine(" SignIn method called");

            if (!ModelState.IsValid)
            {
                ViewData["LoginError"] = "Invalid input data";
                return View("Auth", new AuthViewModel { LoginModel = model });
            }

            Console.WriteLine(" Sending request to AuthService: /AuthService/auth/login");

            var response = await _authServiceClient.PostAsync("/AuthService/auth/login", model);
            var responseContent = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"Response status: {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("SignIn failed: {StatusCode} - {Response}", response.StatusCode, responseContent);
                ViewData["LoginError"] = "Invalid login or password";
                return View("Auth", new AuthViewModel { LoginModel = model });
            }

            var loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseContent);
            if (loginResponse is null ||
                string.IsNullOrEmpty(loginResponse.AccessToken) ||
                string.IsNullOrEmpty(loginResponse.RefreshToken))
            {
                _logger.LogWarning("SignIn failed: Empty response from AuthService.");
                ViewData["LoginError"] = "Authentication failed.";
                return View("Auth", new AuthViewModel { LoginModel = model });
            }

            Response.Cookies.Append("accessToken", loginResponse.AccessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddMinutes(15)
            });

            Response.Cookies.Append("refreshToken", loginResponse.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(7)
            });

            return RedirectToAction("Home", "ManagerHome");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SignUp(RegisterRequest model)
        {
            if (!ModelState.IsValid)
            {
                ViewData["RegisterError"] = "Invalid input data";
                return View("Auth", new AuthViewModel { RegisterModel = model });
            }

            var registerResponse = await _authServiceClient.PostAsync("/AuthService/register", model);
            var registerContent = await registerResponse.Content.ReadAsStringAsync();

            if (!registerResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("SignUp failed: {StatusCode} - {Response}", registerResponse.StatusCode, registerContent);
                ViewData["RegisterError"] = "Registration failed.";
                return View("Auth", new AuthViewModel { RegisterModel = model });
            }

            var loginModel = new LoginRequest
            {
                Identifier = model.Email,  
                Password = model.Password
            };

            var signInResponse = await _authServiceClient.PostAsync("/AuthService/auth/login", loginModel);
            var signInContent = await signInResponse.Content.ReadAsStringAsync();

            if (!signInResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Auto SignIn after SignUp failed: {StatusCode} - {Response}", signInResponse.StatusCode, signInContent);
                ViewData["RegisterError"] = "Registration succeeded, but login failed.";
                return View("Auth", new AuthViewModel { RegisterModel = model });
            }

            var loginResponse = JsonSerializer.Deserialize<LoginResponse>(signInContent);
            if (loginResponse is null || string.IsNullOrEmpty(loginResponse.AccessToken) || string.IsNullOrEmpty(loginResponse.RefreshToken))
            {
                _logger.LogWarning("Auto SignIn failed: Empty response from AuthService.");
                ViewData["RegisterError"] = "Registration succeeded, but authentication failed.";
                return View("Auth", new AuthViewModel { RegisterModel = model });
            }

            Response.Cookies.Append("accessToken", loginResponse.AccessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddMinutes(15)
            });

            Response.Cookies.Append("refreshToken", loginResponse.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(7)
            });

            _logger.LogInformation("User {Email} successfully registered and logged in.", model.Email);

            return RedirectToAction("Index", "ManagerHome");
        }

    }
}
