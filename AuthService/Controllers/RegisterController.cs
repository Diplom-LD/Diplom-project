using AuthService.Models.Register;
using AuthService.Models.User;
using AuthService.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AuthService.Controllers
{
    [Route("AuthService/register")]
    [ApiController]
    public class RegisterController : ControllerBase
    {
        private readonly AuthDbContext _dbContext;
        private readonly PasswordHasher<User> _passwordHasher = new();

        public RegisterController(AuthDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpPost]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            return await ProcessRegistration(request);
        }

        [HttpPost("form")]
        public async Task<IActionResult> RegisterForm([FromForm] RegisterRequest request)
        {
            return await ProcessRegistration(request);
        }

        private async Task<IActionResult> ProcessRegistration(RegisterRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!Regex.IsMatch(request.Login, "^[a-zA-Z0-9_-]{4,20}$"))
                return BadRequest("Login must be 4-20 characters and can only contain letters, numbers, hyphens, and underscores.");

            if (await _dbContext.Users.AnyAsync(u => u.Email == request.Email))
                return BadRequest("User with this email already exists");

            if (await _dbContext.Users.AnyAsync(u => u.UserName == request.Login))
                return BadRequest("User with this login already exists");

            if (!string.IsNullOrWhiteSpace(request.Phone) && await _dbContext.Users.AnyAsync(u => u.Phone == request.Phone))
                return BadRequest("A user with this phone number already exists.");

            if (!string.IsNullOrWhiteSpace(request.Address) && request.Address.Length < 6)
                return BadRequest("Address must be at least 6 characters long.");

            if (request.Password.Length < 6)
                return BadRequest("Password must be at least 6 characters long");

            string role = request.Role.ToLower();
            if (role != "manager" && role != "worker" && role != "client")
            {
                return BadRequest("Invalid role. Allowed roles: manager, worker, client");
            }

            User user = role switch
            {
                "manager" => new Manager
                {
                    UserName = request.Login,
                    Email = request.Email,
                    Role = role,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Phone = request.Phone,
                    Address = request.Address
                },
                "worker" => new Worker
                {
                    UserName = request.Login,
                    Email = request.Email,
                    Role = role,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Phone = request.Phone,
                    Address = request.Address
                },
                "client" => new Client
                {
                    UserName = request.Login,
                    Email = request.Email,
                    Role = role,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Phone = request.Phone,
                    Address = request.Address
                },
                _ => throw new InvalidOperationException("Unexpected role value")
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

            if (user is Manager)
                _dbContext.Managers.Add((Manager)user);
            else if (user is Worker)
                _dbContext.Workers.Add((Worker)user);
            else
                _dbContext.Clients.Add((Client)user);

            await _dbContext.SaveChangesAsync();

            return Ok(new { Message = $"{role} registered successfully" });
        }
    }
}
