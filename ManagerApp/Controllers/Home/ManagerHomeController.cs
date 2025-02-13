using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ManagerApp.Clients;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;

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

    }
}
