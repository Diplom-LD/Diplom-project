using AuthService.Data;
using AuthService.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AuthService.Controllers
{
    [Route("AuthService/auth")]
    [ApiController]
    [Authorize]
    public class AccountController(AuthDbContext _dbContext, ILogger<AccountController> _logger) : ControllerBase
    {
        [HttpGet("myprofile")]
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

        [HttpGet("getprofile/{loginOrEmail}")]
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

        [HttpPut("updateprofile")]
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
    }
}
