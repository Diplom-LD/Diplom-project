using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using ManagerApp.Models.AuthRequest;
using ManagerApp.Models.AuthResponse;
using ManagerApp.Clients;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ManagerApp.Services;

namespace ManagerApp.Controllers.Auth
{
    public class AuthController(AuthServiceClient _authServiceClient, ILogger<AuthController> _logger, JsonService _jsonService) : Controller
    {
        public IActionResult Auth()
        {
            return View(new AuthViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SignIn(LoginRequest model)
        {
            _logger.LogInformation("SignIn method called with Identifier: {Identifier}", model.Identifier);

            if (!ModelState.IsValid)
            {
                LogModelErrors();
                ViewData["LoginError"] = "Invalid input data";
                return View("Auth", new AuthViewModel { LoginModel = model });
            }

            _logger.LogInformation("Sending request to AuthService: /auth/sign-in");

            HttpResponseMessage response;
            try
            {
                response = await _authServiceClient.PostAsync("/auth/sign-in", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SignIn failed: Exception when sending request to AuthService.");
                ViewData["LoginError"] = "Authentication service is unavailable.";
                return View("Auth", new AuthViewModel { LoginModel = model });
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Response status: {StatusCode}, Response body: {ResponseContent}", response.StatusCode, responseContent);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("SignIn failed: {StatusCode} - {Response}", response.StatusCode, responseContent);
                ViewData["LoginError"] = "Invalid login or password";
                return View("Auth", new AuthViewModel { LoginModel = model });
            }

            if (string.IsNullOrWhiteSpace(responseContent))
            {
                _logger.LogWarning("SignIn failed: Empty response from AuthService.");
                ViewData["LoginError"] = "Authentication failed.";
                return View("Auth", new AuthViewModel { LoginModel = model });
            }

            var loginResponse = _jsonService.Deserialize<LoginResponse>(responseContent);
            if (loginResponse == null || string.IsNullOrEmpty(loginResponse.AccessToken))
            {
                _logger.LogWarning("SignIn failed: Invalid response structure from AuthService.");
                ViewData["LoginError"] = "Authentication failed.";
                return View("Auth", new AuthViewModel { LoginModel = model });
            }

            string? refreshToken = ExtractRefreshToken(response);
            if (string.IsNullOrEmpty(refreshToken))
            {
                _logger.LogWarning("SignIn failed: Refresh token not found in Set-Cookie headers.");
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

            Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(7)
            });

            return RedirectToAction("Home", "ManagerHome");
        }

        private static string? ExtractRefreshToken(HttpResponseMessage response)
        {
            if (response.Headers.TryGetValues("Set-Cookie", out var setCookieHeaders))
            {
                foreach (var cookieHeader in setCookieHeaders)
                {
                    if (cookieHeader.StartsWith("refreshToken=", StringComparison.OrdinalIgnoreCase))
                    {
                        var parts = cookieHeader.Split(';')[0].Split('=');
                        if (parts.Length == 2)
                        {
                            return parts[1];
                        }
                    }
                }
            }
            return null;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SignUp(RegisterRequest model)
        {
            _logger.LogInformation("SignUp method called for Email: {Email}", model.Email);

            if (!ModelState.IsValid)
            {
                LogModelErrors();
                ViewData["RegisterError"] = "Invalid input data";
                return View("Auth", new AuthViewModel { RegisterModel = model });
            }

            var registerResponse = await _authServiceClient.PostAsync("/auth/sign-up", model);
            var registerContent = await registerResponse.Content.ReadAsStringAsync();

            if (!registerResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("SignUp failed: {StatusCode} - {Response}", registerResponse.StatusCode, registerContent);
                ViewData["RegisterError"] = "Registration failed.";
                return View("Auth", new AuthViewModel { RegisterModel = model });
            }

            return await SignIn(new LoginRequest { Identifier = model.Email, Password = model.Password });
        }

        private void LogModelErrors()
        {
            foreach (var key in ModelState.Keys)
            {
                var value = ModelState[key];
                if (value?.Errors.Count > 0)
                {
                    _logger.LogWarning("ModelState Error for {Key}: {Errors}", key, string.Join(", ", value.Errors.Select(e => e.ErrorMessage)));
                }
            }
        }
    }
}
