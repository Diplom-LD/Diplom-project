using AuthService.Data;
using AuthService.Models.User;
using AuthService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace AuthService.Controllers
{
    [Authorize]
    [Route("auth/account")]
    [ApiController]
    [Authorize]
    public class AccountController(AuthDbContext _dbContext, ILogger<AccountController> _logger, GeoCodingService _geoCodingService, RabbitMqProducerService _rabbitMqProducerService) : ControllerBase
    {
        [HttpGet("my-profile")]
        public async Task<IActionResult> GetMyProfile()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return Unauthorized(new { message = "User is not authenticated" });
            }

            string? userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out Guid userId))
            {
                return Unauthorized(new { message = "User ID is missing or invalid in the token" });
            }

            string? tokenStamp = User.FindFirst("SecurityStamp")?.Value;

            var user = await _dbContext.Users
                .Where(u => u.Id == userId)
                .Select(u => new
                {
                    u.Id,
                    u.UserName,
                    u.Email,
                    u.Role,
                    u.FirstName,
                    u.LastName,
                    u.PhoneNumber,
                    u.Address,
                    u.Latitude,
                    u.Longitude,
                    u.SecurityStamp
                })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                _logger.LogWarning("User with ID {UserId} not found", userId);
                return NotFound(new { message = "User not found" });
            }

            if (user.SecurityStamp != tokenStamp)
            {
                _logger.LogWarning("SecurityStamp mismatch for user {UserId}", userId);
                return Unauthorized(new { message = "Session is no longer valid. Please re-authenticate." });
            }

            return Ok(new
            {
                user.Id,
                user.UserName,
                user.Email,
                user.Role,
                user.FirstName,
                user.LastName,
                user.PhoneNumber,
                user.Address,
                user.Latitude,
                user.Longitude
            });
        }

        [HttpGet("get-profile/{loginOrEmail}")]
        [Authorize(Roles = "manager")]
        public async Task<IActionResult> GetProfile([FromRoute, Required] string loginOrEmail)
        {
            if (string.IsNullOrWhiteSpace(loginOrEmail))
            {
                return BadRequest(new { message = "Login or Email must not be empty" });
            }

            string? userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            string? tokenStamp = User.FindFirst("SecurityStamp")?.Value;

            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out Guid currentUserId))
            {
                return Unauthorized(new { message = "User ID is missing or invalid in the token" });
            }

            var currentUser = await _dbContext.Users
                .Where(u => u.Id == currentUserId)
                .Select(u => new { u.SecurityStamp })
                .FirstOrDefaultAsync();

            if (currentUser == null || currentUser.SecurityStamp != tokenStamp)
            {
                _logger.LogWarning("SecurityStamp mismatch or user not found: {UserId}", currentUserId);
                return Unauthorized(new { message = "Session is no longer valid. Please log in again." });
            }

            var user = await _dbContext.Users
                .Where(u => u.UserName == loginOrEmail || u.Email == loginOrEmail)
                .Select(u => new
                {
                    u.Id,
                    u.UserName,
                    u.Email,
                    u.Role,
                    u.FirstName,
                    u.LastName,
                    u.PhoneNumber,
                    u.Address,
                    u.Latitude,
                    u.Longitude
                })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                _logger.LogWarning("User with login or email {LoginOrEmail} not found", loginOrEmail);
                return NotFound(new { message = "User not found" });
            }

            return Ok(user);
        }

        [HttpPut("update-profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            string? userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            string? tokenStamp = User.FindFirst("SecurityStamp")?.Value;

            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out Guid userId))
            {
                return Unauthorized(new { message = "User ID is missing or invalid in the token" });
            }

            var user = await _dbContext.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("UpdateProfile: User {UserId} not found", userId);
                return NotFound(new { message = "User not found" });
            }

            if (user.SecurityStamp != tokenStamp)
            {
                _logger.LogWarning("SecurityStamp mismatch for user {UserId}", userId);
                return Unauthorized(new { message = "Session is no longer valid. Please log in again." });
            }

            _logger.LogInformation("User {UserId} is updating their profile", userId);

            bool isUpdated = false;
            bool isAddressUpdated = !string.IsNullOrWhiteSpace(request.Address) && request.Address.Trim() != user.Address;

            if (!string.IsNullOrWhiteSpace(request.FirstName) && request.FirstName.Trim() != user.FirstName)
            {
                user.FirstName = request.FirstName.Trim();
                isUpdated = true;
            }
            if (!string.IsNullOrWhiteSpace(request.LastName) && request.LastName.Trim() != user.LastName)
            {
                user.LastName = request.LastName.Trim();
                isUpdated = true;
            }
            if (!string.IsNullOrWhiteSpace(request.Phone) && request.Phone.Trim() != user.PhoneNumber)
            {
                user.PhoneNumber = request.Phone.Trim();
                isUpdated = true;
            }
            if (!string.IsNullOrWhiteSpace(request.Address) && request.Address.Trim() != user.Address)
            {
                user.Address = request.Address.Trim();
                isUpdated = true;
            }

            if (isAddressUpdated && !string.IsNullOrWhiteSpace(user.Address))
            {
                var coordinates = await _geoCodingService.GetBestCoordinateAsync(user.Address);
                if (coordinates.HasValue)
                {
                    user.Latitude = coordinates.Value.Latitude;
                    user.Longitude = coordinates.Value.Longitude;
                    _logger.LogInformation("Updated location for User {UserId}: {Latitude}, {Longitude}", userId, user.Latitude, user.Longitude);
                    isUpdated = true;
                }
                else
                {
                    _logger.LogWarning("GeoCodingService: Unable to retrieve coordinates for User {UserId} with Address {Address}", userId, user.Address);
                }
            }

            if (!isUpdated)
            {
                return Ok(new { message = "No changes detected" });
            }

            await _dbContext.SaveChangesAsync();

            await _rabbitMqProducerService.PublishUserUpdatedAsync(user);
            _logger.LogInformation("📤 Sent updated user profile to RabbitMQ: {UserId}, {Name}", user.Id, $"{user.FirstName} {user.LastName}");

            return Ok(new { message = "Profile updated successfully" });
        }


        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            string? userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            string? tokenStamp = User.FindFirst("SecurityStamp")?.Value;

            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out Guid userId))
            {
                return Unauthorized(new { message = "User ID is missing or invalid in the token" });
            }

            var user = await _dbContext.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("ChangePassword: User {UserId} not found", userId);
                return NotFound(new { message = "User not found" });
            }

            if (user.SecurityStamp != tokenStamp)
            {
                _logger.LogWarning("SecurityStamp mismatch for user {UserId}", userId);
                return Unauthorized(new { message = "Session is no longer valid. Please log in again." });
            }

            var passwordHasher = new PasswordHasher<User>();

            if (string.IsNullOrEmpty(user.PasswordHash))
            {
                _logger.LogWarning("ChangePassword: User {UserId} has no password set", userId);
                return BadRequest(new { message = "User does not have a password set. Please reset your password." });
            }

            if (passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword) != PasswordVerificationResult.Success)
            {
                return BadRequest(new { message = "Current password is incorrect" });
            }

            user.PasswordHash = passwordHasher.HashPassword(user, request.NewPassword);

            if (HttpContext.RequestServices.GetService<UserManager<User>>() is { } userManager)
            {
                await userManager.UpdateSecurityStampAsync(user);
            }

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("User {UserId} changed password successfully", userId);
            return Ok(new { message = "Password changed successfully" });
        }


        [HttpDelete("delete-account")]
        public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountRequest request)
        {
            string? userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            string? tokenStamp = User.FindFirst("SecurityStamp")?.Value;

            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out Guid userId))
            {
                return Unauthorized(new { message = "User ID is missing or invalid in the token" });
            }

            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                _logger.LogWarning("DeleteAccount: User {UserId} not found", userId);
                return NotFound(new { message = "User not found" });
            }

            if (user.SecurityStamp != tokenStamp)
            {
                _logger.LogWarning("SecurityStamp mismatch for user {UserId}", userId);
                return Unauthorized(new { message = "Session is no longer valid. Please log in again." });
            }

            var passwordHasher = new PasswordHasher<User>();

            if (string.IsNullOrEmpty(user.PasswordHash))
            {
                return BadRequest(new { message = "Your account does not have a password. Use an OAuth method to remove your account." });
            }

            if (passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) != PasswordVerificationResult.Success)
            {
                _logger.LogWarning("DeleteAccount failed: Incorrect password for user {UserId}", userId);
                return BadRequest(new { message = "Password is incorrect" });
            }

            if (HttpContext.RequestServices.GetService<UserManager<User>>() is { } userManager)
            {
                await userManager.UpdateSecurityStampAsync(user);
            }

            Response.Cookies.Delete("refreshToken");

            _logger.LogInformation("User {UserId} is deleting their account", userId);
            try
            {
                _dbContext.Users.Remove(user);
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting account for user {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred while deleting the account. Please try again later." });
            }

            _logger.LogInformation("User {UserId} deleted their account successfully", userId);
            return Ok(new { message = "Account deleted successfully" });
        }


        [HttpGet("get-all-clients")]
        [Authorize(Roles = "manager")]
        public async Task<IActionResult> GetAllClients()
        {
            string? userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            string? tokenStamp = User.FindFirst("SecurityStamp")?.Value;

            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out Guid userId))
            {
                return Unauthorized(new { message = "User ID is missing or invalid in the token" });
            }

            var manager = await _dbContext.Users
                .Where(u => u.Id == userId)
                .Select(u => new { u.SecurityStamp })
                .FirstOrDefaultAsync();

            if (manager == null || manager.SecurityStamp != tokenStamp)
            {
                _logger.LogWarning("SecurityStamp mismatch or manager not found: {UserId}", userId);
                return Unauthorized(new { message = "Session is no longer valid. Please log in again." });
            }

            _logger.LogInformation("🟡 Fetching all clients from database...");

            var clients = await _dbContext.Users
                .Where(u => u.Role == "client")
                .Select(u => new
                {
                    u.Id,
                    u.UserName,
                    u.Email,
                    u.FirstName,
                    u.LastName,
                    u.PhoneNumber,
                    u.Address,
                    u.Latitude,
                    u.Longitude
                })
                .ToListAsync();

            _logger.LogInformation("🟢 Found {Count} clients in database.", clients.Count);

            if (clients.Count == 0)
            {
                _logger.LogWarning("⚠️ No clients found in the database!");
            }

            return Ok(clients);
        }


    }
}