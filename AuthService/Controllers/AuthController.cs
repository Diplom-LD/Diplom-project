using AuthService.Models.Auth;
using AuthService.Models.User;
using AuthService.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace AuthService.Controllers
{
    [Route("AuthService/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AuthDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;
        private readonly PasswordHasher<User> _passwordHasher = new();

        public AuthController(AuthDbContext dbContext, IConfiguration configuration, ILogger<AuthController> logger)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            bool isEmail = new EmailAddressAttribute().IsValid(request.Identifier);

            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => (isEmail && u.Email == request.Identifier) || (!isEmail && u.UserName == request.Identifier));

            if (user == null || string.IsNullOrEmpty(user.PasswordHash))
            {
                _logger.LogWarning("Неудачная попытка входа: пользователь {Identifier} не найден", request.Identifier);
                return Unauthorized(new { message = "Invalid login or password" });
            }

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
            if (result != PasswordVerificationResult.Success)
            {
                _logger.LogWarning("Неудачная попытка входа: неверный пароль для пользователя {Identifier}", request.Identifier);
                return Unauthorized(new { message = "Invalid login or password" });
            }

            var token = GenerateJwtToken(user);
            _logger.LogInformation("Успешный вход: {Identifier}", request.Identifier);
            return Ok(new { token });
        }

        private string GenerateJwtToken(User user)
        {
            string? jwtKey = _configuration["Jwt:Key"];
            if (string.IsNullOrEmpty(jwtKey))
            {
                throw new InvalidOperationException("JWT ключ не найден в конфигурации.");
            }

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName!),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(ClaimTypes.Role, user.Role!)
            };

            var token = new JwtSecurityToken(
                _configuration["Jwt:Issuer"],
                _configuration["Jwt:Audience"],
                claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
