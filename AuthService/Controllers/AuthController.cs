using AuthService.Models.Auth;
using AuthService.Models.User;
using AuthService.Data;
using AuthService.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace AuthService.Controllers
{
    [Route("auth")]
    [ApiController]
    public class AuthController(AuthDbContext _dbContext, TokenService _tokenService, ILogger<AuthController> _logger) : ControllerBase
    {
        private readonly PasswordHasher<User> _passwordHasher = new();

        [HttpPost("sign-in")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { message = "Invalid request format" });

            bool isEmail = new EmailAddressAttribute().IsValid(request.Identifier);

            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => (isEmail && u.Email == request.Identifier) || (!isEmail && u.UserName == request.Identifier));

            if (user == null || string.IsNullOrEmpty(user.PasswordHash))
            {
                _logger.LogWarning("Login failed: User {Identifier} not found", request.Identifier);
                return Unauthorized(new { message = "Invalid login or password" });
            }

            if (_passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) != PasswordVerificationResult.Success)
            {
                _logger.LogWarning("Login failed: Incorrect password for user {Identifier}", request.Identifier);
                return Unauthorized(new { message = "Invalid login or password" });
            }

            var accessToken = _tokenService.GenerateAccessToken(user);
            var refreshToken = _tokenService.GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);

            await _dbContext.SaveChangesAsync();

            _tokenService.SetRefreshToken(Response, Request, refreshToken, user.RefreshTokenExpiry.Value);

            _logger.LogInformation("User {Identifier} successfully logged in", request.Identifier);
            return Ok(new { accessToken });
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken()
        {
            var refreshToken = _tokenService.GetRefreshToken(Request);
            if (refreshToken == null)
            {
                _logger.LogWarning("Token refresh failed: No refresh token provided");
                return Unauthorized(new { message = "No refresh token provided" });
            }

            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);

            if (user == null || user.RefreshTokenExpiry < DateTime.UtcNow)
            {
                _logger.LogWarning("Token refresh failed: Invalid or expired refresh token");
                return Unauthorized(new { message = "Invalid or expired refresh token" });
            }

            var newAccessToken = _tokenService.GenerateAccessToken(user);
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
            await _dbContext.SaveChangesAsync();

            _tokenService.SetRefreshToken(Response, Request, newRefreshToken, user.RefreshTokenExpiry.Value);

            _logger.LogInformation("Access token refreshed for user {UserId}", user.Id);
            return Ok(new { accessToken = newAccessToken });
        }

        [HttpGet("get-token")]
        public async Task<IActionResult> ValidateToken()
        {
            if (string.IsNullOrEmpty(Request.Headers.Authorization))
            {
                _logger.LogWarning("Token validation failed: No token provided");
                return Unauthorized(new { message = "No token provided" });
            }

            var authHeader = Request.Headers.Authorization.ToString();

            if (!authHeader.StartsWith("Bearer "))
            {
                _logger.LogWarning("Token validation failed: No valid token provided");
                return Unauthorized(new { message = "Invalid token format" });
            }

            var token = authHeader["Bearer ".Length..].Trim();
            var principal = _tokenService.ValidateAccessToken(token);

            if (principal == null)
            {
                _logger.LogWarning("Token validation failed: Invalid token");
                return Unauthorized(new { message = "Invalid token" });
            }

            var userId = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Token validation failed: No user ID found in token");
                return Unauthorized(new { message = "Invalid token" });
            }

            var user = await _dbContext.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Token validation failed: User not found for ID {UserId}", userId);
                return Unauthorized(new { message = "Invalid token" });
            }

            _logger.LogInformation("Token validation successful for user {UserId}", userId);
            return Ok(new
            {
                message = "Token is valid",
                userId,
                userName = user.UserName,
                email = user.Email,
                role = user.Role
            });
        }


        [HttpPost("sign-out")]
        public async Task<IActionResult> Logout()
        {
            var refreshToken = _tokenService.GetRefreshToken(Request);
            if (refreshToken == null)
            {
                _logger.LogWarning("Logout failed: No refresh token provided");
                return BadRequest(new { message = "No refresh token provided" });
            }

            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);
            if (user == null || user.RefreshTokenExpiry < DateTime.UtcNow)
            {
                _logger.LogWarning("Logout failed: Invalid or expired refresh token {RefreshToken}", refreshToken);
                return BadRequest(new { message = "Invalid refresh token" });
            }

            user.RefreshToken = null;
            user.RefreshTokenExpiry = null;
            await _dbContext.SaveChangesAsync();

            _tokenService.RemoveRefreshToken(Response, Request);

            _logger.LogInformation("User {UserId} successfully logged out", user.Id);
            return Ok(new { message = "Logged out successfully" });
        }

    }
}
