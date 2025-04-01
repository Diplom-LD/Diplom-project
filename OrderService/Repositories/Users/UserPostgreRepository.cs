using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderService.Data.Orders;
using OrderService.DTO.GeoLocation;
using OrderService.Models.Users;

namespace OrderService.Repositories.Users
{
    public class UserPostgreRepository(OrderDbContext context, ILogger<UserPostgreRepository> logger)
    {
        private readonly OrderDbContext _context = context;
        private readonly ILogger<UserPostgreRepository> _logger = logger;

        public async Task<List<User>> GetAllUsersAsync()
        {
            try
            {
                var users = await _context.Users.ToListAsync();
                _logger.LogInformation("📥 [PostgreSQL] Загружено {Count} пользователей.", users.Count);
                return users;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ошибка при получении данных из PostgreSQL.");
                return [];
            }
        }

        public async Task<User?> GetUserByIdAsync(Guid userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("⚠️ [PostgreSQL] Пользователь с ID: {UserId} не найден.", userId);
                }
                else
                {
                    _logger.LogInformation("✅ [PostgreSQL] Пользователь с ID: {UserId} загружен.", userId);
                }
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [PostgreSQL] Ошибка при получении пользователя с ID: {UserId}.", userId);
                return null;
            }
        }

        public async Task<List<Technician>> GetTechniciansAsync()
        {
            try
            {
                var technicians = await _context.Users
                    .OfType<Technician>()
                    .ToListAsync();

                if (technicians.Count == 0)
                {
                    _logger.LogWarning("⚠️ [PostgreSQL] В системе нет зарегистрированных техников.");
                }
                else
                {
                    _logger.LogInformation("✅ [PostgreSQL] Загружено {Count} техников.", technicians.Count);
                }

                return technicians;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [PostgreSQL] Ошибка при получении данных о техниках.");
                return [];
            }
        }

       
        public async Task SaveUserAsync(User user)
        {
            try
            {
                if (user is Technician technician)
                {
                    _context.Technicians.Add(technician);
                }
                else
                {
                    _context.Users.Add(user);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("✅ [PostgreSQL] Пользователь сохранён с ID: {UserId}", user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ошибка при сохранении данных в PostgreSQL.");
            }
        }

        public async Task UpdateUserAsync(User user)
        {
            try
            {
                if (user is Technician technician)
                {
                    _context.Technicians.Update(technician);
                }
                else
                {
                    _context.Users.Update(user);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("✅ [PostgreSQL] Пользователь обновлён с ID: {UserId}", user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ошибка при обновлении данных в PostgreSQL.");
            }
        }

        public async Task<Technician?> GetTechnicianByIdAsync(Guid technicianId)
        {
            return await _context.Technicians
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == technicianId);
        }

        public async Task<List<TechnicianCoordinateDTO>> GetTechnicianCoordinatesAsync()
        {
            try
            {
                var technicians = await _context.Technicians.AsNoTracking().ToListAsync();

                var result = technicians.Select(t => new TechnicianCoordinateDTO
                {
                    TechnicianId = t.Id,
                    FullName = t.FullName,
                    Email = t.Email,
                    Address = t.Address,
                    PhoneNumber = t.PhoneNumber,
                    Latitude = t.Latitude,
                    Longitude = t.Longitude
                }).ToList();

                _logger.LogInformation("📍 [PostgreSQL] Получены координаты {Count} техников.", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [PostgreSQL] Ошибка при получении координат техников.");
                return [];
            }
        }

    }
}