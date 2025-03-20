using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ManagerApp.Clients;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Net.Http;

namespace ManagerApp.Controllers.Home
{
    [Authorize]
    public class ManagerHomeController(AuthServiceClient _authServiceClient, ILogger<ManagerHomeController> _logger) : Controller
    {
        public IActionResult Home()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            try
            {
                _logger.LogInformation("Logout request initiated.");

                var refreshToken = Request.Cookies["refreshToken"];
                if (!string.IsNullOrEmpty(refreshToken))
                {
                    var response = await _authServiceClient.PostWithCookiesAsync("/auth/sign-out", new { }, $"refreshToken={refreshToken}");

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogWarning("Logout failed with status code: {StatusCode}", response.StatusCode);
                        TempData["LogoutMessage"] = "Logout failed.";
                        return RedirectToAction("Auth", "Auth");
                    }
                }

                Response.Cookies.Delete("accessToken");
                Response.Cookies.Delete("refreshToken");

                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                _logger.LogInformation("User successfully logged out.");

                TempData["LogoutMessage"] = "You have successfully logged out.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logout failed: Exception when sending request to AuthService.");
                TempData["LogoutMessage"] = "Logout service is unavailable.";
            }

            return RedirectToAction("Auth", "Auth");
        }


        [HttpGet("get-clients")]
        public async Task<IActionResult> GetClients()
        {
            try
            {
                var accessToken = Request.Cookies["accessToken"];
                if (string.IsNullOrEmpty(accessToken))
                {
                    _logger.LogWarning("Access token is missing.");
                    return Unauthorized(new { message = "Access token is missing." });
                }

                _logger.LogInformation("Fetching clients from AuthService...");
                var clients = await _authServiceClient.GetClientsAsync(accessToken);

                if (clients == null || clients.Count == 0)
                {
                    _logger.LogWarning("No clients found.");
                    return Ok(new List<object>());
                }

                _logger.LogInformation("Clients received: {Count}", clients.Count);
                return Ok(clients);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching clients from AuthService.");
                return StatusCode(500, new { message = "Error fetching clients." });
            }
        }
    }
}
