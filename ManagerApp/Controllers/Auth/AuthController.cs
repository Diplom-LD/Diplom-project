using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;
using ManagerApp.Models.AuthRequest;
using ManagerApp.Models.AuthResponse;
using ManagerApp.Clients;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ManagerApp.Services;
using System;
using System.Text.Json;

namespace ManagerApp.Controllers.Auth
{
    public class AuthController(AuthServiceClient _authServiceClient, ILogger<AuthController> _logger, JsonService _jsonService) : Controller
    {
        public IActionResult Auth()
        {
            return View(new AuthViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> SignIn([FromBody] LoginRequest model)
        {
            _logger.LogInformation("SignIn method called with Identifier: {Identifier}", model.Identifier);

            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid input data" });
            }

            HttpResponseMessage response;
            try
            {
                response = await _authServiceClient.PostAsync("/auth/sign-in", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SignIn failed: Exception when sending request to AuthService.");
                return Json(new { success = false, message = "Authentication service is unavailable." });
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Response status: {StatusCode}", response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                return Json(new { success = false, message = "Invalid login or password" });
            }

            var loginResponse = _jsonService.Deserialize<LoginResponse>(responseContent);
            if (loginResponse?.AccessToken == null)
            {
                return Json(new { success = false, message = "Authentication failed." });
            }

            string? refreshToken = ExtractRefreshToken(response);
            if (string.IsNullOrEmpty(refreshToken))
            {
                return Json(new { success = false, message = "Authentication failed." });
            }

            SetAuthCookies(loginResponse.AccessToken, refreshToken);

            return Json(new { success = true, redirectUrl = Url.Action("Home", "ManagerHome") });
        }

        [HttpPost]
        public async Task<IActionResult> SignUp([FromBody] RegisterRequest model)
        {
            _logger.LogInformation("SignUp method called for Email: {Email}", model.Email);

            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid input data" });
            }

            HttpResponseMessage registerResponse;
            try
            {
                registerResponse = await _authServiceClient.PostAsync("/auth/sign-up", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SignUp failed: Exception when sending request to AuthService.");
                return Json(new { success = false, message = "Registration service is unavailable." });
            }

            var responseContent = await registerResponse.Content.ReadAsStringAsync();
            _logger.LogWarning("SignUp failed: {ErrorMessage}", responseContent);

            if (!registerResponse.IsSuccessStatusCode)
            {
                var responseJson = JsonDocument.Parse(responseContent);
                var errorMessage = responseJson.RootElement.GetProperty("message").GetString();
                return Json(new { success = false, message = errorMessage ?? "Registration failed." });
            }

            return Json(new { success = true, message = "Registration successful, please sign in." });
        }


        private static string? ExtractRefreshToken(HttpResponseMessage response)
        {
            if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
            {
                foreach (var cookie in cookies)
                {
                    var parts = cookie.Split(';');
                    foreach (var part in parts)
                    {
                        var keyValue = part.Split('=', 2);
                        if (keyValue.Length == 2 && keyValue[0].Trim() == "refreshToken")
                        {
                            return keyValue[1].Trim();
                        }
                    }
                }
            }
            return null;
        }

        private void SetAuthCookies(string accessToken, string refreshToken)
        {
            Response.Cookies.Append("accessToken", accessToken, new CookieOptions
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
        }
    }
}
