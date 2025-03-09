using AuthService.Models.Register;
using AuthService.Models.User;
using AuthService.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using AuthService.Services;
using System.Text.RegularExpressions;

namespace AuthService.Controllers
{
    [Route("auth/sign-up")]
    [ApiController]
    public partial class RegisterController(AuthDbContext _dbContext, GeoCodingService _geoCodingService, RabbitMqProducerService _rabbitMqProducer) : ControllerBase
    {
        private readonly PasswordHasher<User> _passwordHasher = new();

        [HttpPost]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request) => await HandleRegistration(request);

        [HttpPost("form")]
        public async Task<IActionResult> RegisterForm([FromForm] RegisterRequest request) => await HandleRegistration(request);

        private async Task<IActionResult> HandleRegistration(RegisterRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var validationError = await ValidateRegistrationRequest(request);
            if (validationError != null)
                return BadRequest(new { message = validationError });

            return await ProcessRegistration(request);
        }

        private async Task<IActionResult> ProcessRegistration(RegisterRequest request)
        {
            var user = new User
            {
                UserName = request.Login.Trim(),
                Email = request.Email.Trim(),
                Role = request.Role?.Trim().ToLowerInvariant() ?? string.Empty,
                FirstName = request.FirstName?.Trim(),
                LastName = request.LastName?.Trim(),
                PhoneNumber = request.Phone?.Trim(),
                Address = request.Address?.Trim()
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

            if (!string.IsNullOrWhiteSpace(request.Address))
            {
                var coordinates = await _geoCodingService.GetCoordinatesAsync(request.Address);
                if (coordinates.HasValue)
                {
                    user.Latitude = coordinates.Value.Latitude;
                    user.Longitude = coordinates.Value.Longitude;
                }
            }

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            if (user.Role == "worker")
            {
                _rabbitMqProducer.PublishTechnicianUpdate([user]);
                Console.WriteLine($"📤 [RabbitMQ] Новый рабочий отправлен: {user.Id} ({user.FirstName} {user.LastName})");
            }

            return Ok(new { Message = $"{user.Role} registered successfully", user.Latitude, user.Longitude });
        }

        private async Task<string?> ValidateRegistrationRequest(RegisterRequest request)
        {
            if (!LoginRegex().IsMatch(request.Login))
                return "Login must be 4-20 characters and can only contain letters, numbers, hyphens, and underscores.";

            if (await FindUserAsync(request.Email, request.Login, request.Phone) != null)
                return "User with this email, login, or phone number already exists.";

            string role = request.Role?.Trim().ToLowerInvariant() ?? string.Empty;
            if (role is not ("manager" or "worker" or "client"))
                return "Invalid role. Allowed roles: manager, worker, client.";

            if (role is "manager" or "worker" && string.IsNullOrWhiteSpace(request.RegistrationCode))
                return $"{char.ToUpper(role[0]) + role[1..]} registration requires a valid registration code.";

            if (role == "manager")
            {
                bool codeExists = await _dbContext.ManagerRegistrationCodes
                    .AnyAsync(c => c.Code == request.RegistrationCode);

                if (!codeExists)
                    return "Invalid manager registration code.";
            }
            else if (role == "worker")
            {
                bool codeExists = await _dbContext.WorkerRegistrationCodes
                    .AnyAsync(c => c.Code == request.RegistrationCode);

                if (!codeExists)
                    return "Invalid worker registration code.";
            }

            return null;
        }

        private async Task<User?> FindUserAsync(string email, string login, string? phone)
        {
            return await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Email == email || u.UserName == login || (phone != null && u.PhoneNumber == phone));
        }

        [GeneratedRegex("^[a-zA-Z0-9_-]{4,20}$")]
        private static partial Regex LoginRegex();
    }
}
