using AuthService.Data;
using AuthService.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AuthService.Controllers
{
    [Route("auth/account")]
    [ApiController]
    [Authorize]
    public class AccountController(AuthDbContext _dbContext, ILogger<AccountController> _logger) : ControllerBase
    {
        [HttpGet("my-profile")]
        public async Task<IActionResult> GetMyProfile()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return Unauthorized(new { message = "User is not authenticated" });
            }

            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User ID is missing in the token" });
            }

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
                    u.Phone,
                    u.Address
                })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                _logger.LogWarning("User with ID {UserId} not found", userId);
                return NotFound(new { message = "User not found" });
            }

            return Ok(user);
        }

        [HttpGet("get-profile/{loginOrEmail}")]
        [Authorize(Roles = "manager")]
        public async Task<IActionResult> GetProfile([FromRoute, Required] string loginOrEmail)
        {
            if (string.IsNullOrWhiteSpace(loginOrEmail))
            {
                return BadRequest(new { message = "Login or Email must not be empty" });
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
                    u.Phone,
                    u.Address
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

            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User ID is missing in the token" });
            }

            var user = await _dbContext.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("UpdateProfile: User {UserId} not found", userId);
                return NotFound(new { message = "User not found" });
            }

            _logger.LogInformation("User {UserId} is updating their profile", userId);

            if (request.FirstName != null) user.FirstName = request.FirstName.Trim();
            if (request.LastName != null) user.LastName = request.LastName.Trim();
            if (request.Phone != null) user.Phone = request.Phone.Trim();
            if (request.Address != null) user.Address = request.Address.Trim();

            await _dbContext.SaveChangesAsync();

            return Ok(new { message = "Profile updated successfully" });
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User ID is missing in the token" });
            }

            var user = await _dbContext.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("ChangePassword: User {UserId} not found", userId);
                return NotFound(new { message = "User not found" });
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
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("User {UserId} changed password successfully", userId);
            return Ok(new { message = "Password changed successfully" });
        }

        [HttpDelete("delete-account")]
        public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountRequest request)
        {
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User ID is missing in the token" });
            }

            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                _logger.LogWarning("DeleteAccount: User {UserId} not found", userId);
                return NotFound(new { message = "User not found" });
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

    }
}
