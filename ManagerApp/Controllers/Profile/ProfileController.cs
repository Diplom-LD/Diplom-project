using ManagerApp.Clients;
using ManagerApp.Models.AuthRequest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ManagerApp.Controllers.Profile
{
    [Authorize]
    public class ProfileController(AuthServiceClient authClient, ILogger<ProfileController> logger) : Controller
    {
        private readonly AuthServiceClient _authClient = authClient;
        private readonly ILogger<ProfileController> _logger = logger;

        [HttpGet]
        public async Task<IActionResult> ViewProfile()
        {
            string? token = Request.Cookies["accessToken"];
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("Access token missing in cookie");
                return RedirectToAction("Login", "Auth");
            }

            var profile = await _authClient.GetMyProfileAsync(token);
            if (profile == null)
            {
                _logger.LogWarning("Profile data could not be loaded from AuthService");
                if (IsAjaxRequest()) return NotFound(new { message = "Failed to load profile." });

                TempData["Error"] = "Failed to load profile.";
                return View("Profile");
            }

            if (IsAjaxRequest())
            {
                return Json(profile);
            }

            var model = new UpdateProfileRequest
            {
                FirstName = profile.FirstName,
                LastName = profile.LastName,
                Phone = profile.PhoneNumber,
                Address = profile.Address
            };

            return View("Profile", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest model)
        {
            string? token = Request.Cookies["accessToken"];
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("Access token missing in cookie (POST)");
                return Unauthorized(new { message = "Not authorized." });
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model validation failed on profile update");
                return BadRequest(new { message = "Invalid profile data." });
            }

            var success = await _authClient.UpdateProfileAsync(model, token);
            if (!success)
            {
                _logger.LogWarning("Failed to update profile via AuthServiceClient");
                return BadRequest(new { message = "Failed to update profile." });
            }

            _logger.LogInformation("User profile updated successfully.");

            return Json(new { message = "Profile updated successfully!" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfileRazor(UpdateProfileRequest model)
        {
            if (!ModelState.IsValid)
            {
                return View("Profile", model);
            }

            string? token = Request.Cookies["accessToken"];
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Login", "Auth");
            }

            var success = await _authClient.UpdateProfileAsync(model, token);
            if (!success)
            {
                TempData["Error"] = "Failed to update profile.";
                return View("Profile", model);
            }

            TempData["Success"] = "Profile updated successfully!";
            return RedirectToAction(nameof(ViewProfile));
        }

        private bool IsAjaxRequest()
        {
            return Request.Headers.XRequestedWith == "XMLHttpRequest";
        }
    }
}
