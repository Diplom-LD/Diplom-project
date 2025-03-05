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
    public partial class RegisterController(AuthDbContext _dbContext, GeoCodingService _geoCodingService) : ControllerBase
    {
        private readonly PasswordHasher<User> _passwordHasher = new();

        [HttpPost]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var validationError = await ValidateRegistrationRequest(request);
            if (validationError != null)
                return BadRequest(new { message = validationError });

            return await ProcessRegistration(request);
        }

        [HttpPost("form")]
        public async Task<IActionResult> RegisterForm([FromForm] RegisterRequest request)
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
            User user = new()
            {
                UserName = request.Login.Trim(),
                Email = request.Email.Trim(),
                Role = request.Role.Trim().ToLower(),
                FirstName = request.FirstName?.Trim(),
                LastName = request.LastName?.Trim(),
                Phone = request.Phone?.Trim(),
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

            return Ok(new { Message = $"{user.Role} registered successfully", user.Latitude, user.Longitude });
        }

        private async Task<string?> ValidateRegistrationRequest(RegisterRequest request)
        {
            if (!LoginRegex().IsMatch(request.Login))
                return "Login must be 4-20 characters and can only contain letters, numbers, hyphens, and underscores.";

            if (await FindUserAsync(request.Email, request.Login, request.Phone) != null)
                return "User with this email, login, or phone number already exists.";

            string role = request.Role.Trim().ToLower();
            if (role is not ("manager" or "worker" or "client"))
                return "Invalid role. Allowed roles: manager, worker, client.";

            if (role == "manager" || role == "worker")
            {
                if (string.IsNullOrWhiteSpace(request.RegistrationCode))
                    return $"{role[0].ToString().ToUpper() + role[1..]} registration requires a valid registration code.";

                bool codeExists = role switch
                {
                    "manager" => await _dbContext.ManagerRegistrationCodes.AnyAsync(c => c.Code == request.RegistrationCode),
                    "worker" => await _dbContext.WorkerRegistrationCodes.AnyAsync(c => c.Code == request.RegistrationCode),
                    _ => false
                };

                if (!codeExists)
                    return $"Invalid {role} registration code.";
            }

            return null;
        }


        private async Task<User?> FindUserAsync(string email, string login, string? phone)
        {
            return await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Email == email || u.UserName == login || (phone != null && u.Phone == phone));
        }

        [GeneratedRegex("^[a-zA-Z0-9_-]{4,20}$")]
        private static partial Regex LoginRegex();
    }
}
