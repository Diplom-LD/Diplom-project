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
                return Json(new { success = false, message = "Invalid login, email or password" });
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
                return Json(new { success = false, message = "Invalid login, email or password" });
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
                var validationErrors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                string validationMessage = string.Join("; ", validationErrors);
                return Json(new { success = false, message = $"Invalid input data: {validationMessage}" });
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

            if (!registerResponse.IsSuccessStatusCode)
            {
                string errorMessage = "Registration failed.";
                try
                {
                    using var doc = JsonDocument.Parse(responseContent);

                    if (doc.RootElement.TryGetProperty("message", out var messageProperty))
                    {
                        errorMessage = messageProperty.GetString() ?? errorMessage;
                    }
                    else if (doc.RootElement.TryGetProperty("errors", out var errorsProperty))
                    {
                        var errorMessages = new List<string>();

                        foreach (var property in errorsProperty.EnumerateObject())
                        {
                            foreach (var value in property.Value.EnumerateArray())
                            {
                                errorMessages.Add($"{property.Name}: {value.GetString()}");
                            }
                        }

                        if (errorMessages.Count > 0)
                        {
                            errorMessage = string.Join("; ", errorMessages);
                        }
                    }
                }
                catch (JsonException)
                {
                    _logger.LogWarning("Failed to parse JSON response from AuthService: {ResponseContent}", responseContent);
                }

                _logger.LogWarning("SignUp failed: {ErrorMessage}", errorMessage);
                return Json(new { success = false, message = errorMessage });
            }

            _logger.LogInformation("Registration successful, attempting to log in user...");

            var loginRequest = new LoginRequest
            {
                Identifier = model.Email, 
                Password = model.Password
            };

            HttpResponseMessage loginResponse;
            try
            {
                loginResponse = await _authServiceClient.PostAsync("/auth/sign-in", loginRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Auto-login failed: Exception when sending request to AuthService.");
                return Json(new { success = false, message = "Registration successful, but login failed. Please sign in manually." });
            }

            var loginContent = await loginResponse.Content.ReadAsStringAsync();

            if (!loginResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Auto-login failed: {ErrorMessage}", loginContent);
                return Json(new { success = false, message = "Registration successful, but login failed. Please sign in manually." });
            }

            var loginResponseData = _jsonService.Deserialize<LoginResponse>(loginContent);
            if (loginResponseData?.AccessToken == null)
            {
                return Json(new { success = false, message = "Registration successful, but login failed. Please sign in manually." });
            }

            string? refreshToken = ExtractRefreshToken(loginResponse);
            if (string.IsNullOrEmpty(refreshToken))
            {
                return Json(new { success = false, message = "Registration successful, but login failed. Please sign in manually." });
            }

            SetAuthCookies(loginResponseData.AccessToken, refreshToken);
            _logger.LogInformation("User successfully authenticated after registration.");

            return Json(new { success = true, redirectUrl = Url.Action("Home", "ManagerHome") });
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
