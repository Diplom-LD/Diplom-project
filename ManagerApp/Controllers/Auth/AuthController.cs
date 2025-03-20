using Microsoft.AspNetCore.Mvc;
using ManagerApp.Models.AuthRequest;
using ManagerApp.Models.AuthResponse;
using ManagerApp.Clients;
using ManagerApp.Services;
using System.Text.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace ManagerApp.Controllers.Auth
{
    public class AuthController(AuthServiceClient authServiceClient, ILogger<AuthController> logger, JsonService jsonService) : Controller
    {
        private readonly AuthServiceClient _authServiceClient = authServiceClient;
        private readonly ILogger<AuthController> _logger = logger;
        private readonly JsonService _jsonService = jsonService;

        public async Task<IActionResult> Auth()
        {
            var accessToken = Request.Cookies["accessToken"];
            var refreshToken = Request.Cookies["refreshToken"];

            if (!string.IsNullOrEmpty(accessToken))
            {
                var isValid = await ValidateAndSignIn(accessToken, refreshToken);
                if (isValid)
                {
                    return RedirectToAction("Home", "ManagerHome");
                }
            }

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

            HttpResponseMessage userDataResponse;
            try
            {
                userDataResponse = await _authServiceClient.GetAsync("/auth/get-token", loginResponse.AccessToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve user data after login.");
                return Json(new { success = false, message = "Authentication successful, but failed to retrieve user data." });
            }

            var userDataContent = await userDataResponse.Content.ReadAsStringAsync();
            var userData = _jsonService.Deserialize<GetTokenResponse>(userDataContent);

            if (userData == null)
            {
                _logger.LogError("Failed to deserialize user data after login.");
                return Json(new { success = false, message = "Authentication successful, but failed to retrieve user data." });
            }

            if (userData.Role?.ToLower() != "manager")
            {
                _logger.LogWarning("Unauthorized login attempt: User {Identifier} has role {Role}", model.Identifier, userData.Role);
                return Json(new { success = false, message = "Access denied. Only managers can log in." });
            }

            await SignInUser(userData, loginResponse.AccessToken);
            SetAuthCookies(loginResponse.AccessToken, refreshToken);

            return Json(new { success = true, redirectUrl = Url.Action("Home", "ManagerHome") });
        }


        [HttpPost]
        public async Task<IActionResult> SignUp([FromBody] RegisterRequest model)
        {
            _logger.LogInformation("SignUp method called for Email: {Email}", model.Email);

            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Invalid registration data" });
            }

            try
            {
                var registerResponse = await _authServiceClient.PostAsync("/auth/sign-up", model);

                if (!registerResponse.IsSuccessStatusCode)
                {
                    var errorMessage = await ExtractErrorMessageAsync(registerResponse);
                    _logger.LogWarning("Registration failed: {ErrorMessage}", errorMessage);

                    return StatusCode((int)registerResponse.StatusCode, new { success = false, message = errorMessage });
                }

                _logger.LogInformation("Registration successful, attempting auto-login...");

                var loginRequest = new LoginRequest
                {
                    Identifier = model.Email,
                    Password = model.Password
                };

                var loginResponse = await _authServiceClient.PostAsync("/auth/sign-in", loginRequest);
                if (!loginResponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Auto-login failed after registration. Status code: {StatusCode}", loginResponse.StatusCode);
                    return BadRequest(new { success = false, message = "Registration successful, but login failed. Please sign in manually." });
                }

                var loginResponseData = _jsonService.Deserialize<LoginResponse>(await loginResponse.Content.ReadAsStringAsync());
                var refreshToken = ExtractRefreshToken(loginResponse);

                if (loginResponseData?.AccessToken == null || string.IsNullOrEmpty(refreshToken))
                {
                    _logger.LogWarning("Auto-login token extraction failed after registration.");
                    return BadRequest(new { success = false, message = "Registration successful, but login failed. Please sign in manually." });
                }

                var userDataResponse = await _authServiceClient.GetAsync("/auth/get-token", loginResponseData.AccessToken);
                if (!userDataResponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to retrieve user data after registration. Status code: {StatusCode}", userDataResponse.StatusCode);
                    return BadRequest(new { success = false, message = "Registration successful, but failed to retrieve user data." });
                }

                var userData = _jsonService.Deserialize<GetTokenResponse>(await userDataResponse.Content.ReadAsStringAsync());
                if (userData == null)
                {
                    _logger.LogWarning("Failed to deserialize user data after registration.");
                    return BadRequest(new { success = false, message = "Registration successful, but failed to retrieve user data." });
                }

                await SignInUser(userData, loginResponseData.AccessToken);
                SetAuthCookies(loginResponseData.AccessToken, refreshToken);

                _logger.LogInformation("User successfully authenticated after registration.");
                return Ok(new { success = true, redirectUrl = Url.Action("Home", "ManagerHome") });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SignUp failed due to an unexpected error.");
                return StatusCode(500, new { success = false, message = "Registration service is unavailable. Please try again later." });
            }
        }

        private async Task<string> ExtractErrorMessageAsync(HttpResponseMessage response)
        {
            try
            {
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("📩 Received error response: {Content}", content);

                var errorObj = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
                if (errorObj != null)
                {
                    string errorMessage = errorObj.TryGetValue("message", out var message) ? message?.ToString() ?? "Unknown error." : "Unknown error.";

                    if (errorObj.TryGetValue("errors", out var errors) && errors is JsonElement errorsElement)
                    {
                        var errorDetails = new List<string>();

                        foreach (var error in errorsElement.EnumerateObject())
                        {
                            string fieldName = error.Name;
                            string fieldErrors = string.Join("; ", error.Value.EnumerateArray().Select(e => e.GetString()));
                            errorDetails.Add($"{fieldName}: {fieldErrors}");
                        }

                        if (errorDetails.Count != 0)
                        {
                            errorMessage += "\nDetails:\n" + string.Join("\n", errorDetails);
                        }
                    }

                    return errorMessage;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to parse error response.");
            }

            return $"Request failed with status code {response.StatusCode}";
        }

        private async Task<bool> ValidateAndSignIn(string accessToken, string? refreshToken)
        {
            try
            {
                HttpResponseMessage response = await _authServiceClient.GetAsync("/auth/get-token", accessToken);

                if (response.IsSuccessStatusCode)
                {
                    var userDataContent = await response.Content.ReadAsStringAsync();
                    var userData = _jsonService.Deserialize<GetTokenResponse>(userDataContent);

                    if (userData == null)
                    {
                        _logger.LogWarning("Failed to deserialize GetTokenResponse.");
                        return false;
                    }

                    await SignInUser(userData, accessToken);
                    return true;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && !string.IsNullOrEmpty(refreshToken))
                {
                    _logger.LogWarning("Access token expired, attempting to refresh token.");

                    var refreshResponse = await _authServiceClient.PostAsync("/auth/refresh-token", new { refreshToken });

                    if (refreshResponse.IsSuccessStatusCode)
                    {
                        var refreshContent = await refreshResponse.Content.ReadAsStringAsync();
                        var refreshData = _jsonService.Deserialize<LoginResponse>(refreshContent);

                        if (!string.IsNullOrEmpty(refreshData?.AccessToken))
                        {
                            var userDataResponse = await _authServiceClient.GetAsync("/auth/get-token", refreshData.AccessToken);
                            var userDataRefreshContent = await userDataResponse.Content.ReadAsStringAsync();
                            var userData = _jsonService.Deserialize<GetTokenResponse>(userDataRefreshContent);

                            if (userData == null)
                            {
                                _logger.LogWarning("Failed to deserialize GetTokenResponse.");
                                return false;
                            }

                            await SignInUser(userData, refreshData.AccessToken);
                            SetAuthCookies(refreshData.AccessToken, refreshToken);
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token validation failed.");
            }
            return false;
        }

        private async Task SignInUser(GetTokenResponse userData, string accessToken)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, userData.UserName),
                new(ClaimTypes.NameIdentifier, userData.UserId),
                new(ClaimTypes.Email, userData.Email),
                new(ClaimTypes.Role, userData.Role),
                new("accessToken", accessToken)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
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