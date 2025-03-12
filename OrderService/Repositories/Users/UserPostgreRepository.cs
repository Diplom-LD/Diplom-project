using Microsoft.EntityFrameworkCore;
using OrderService.Data.Orders;
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
                    _logger.LogWarning("❌ Пользователь с ID: {UserId} не найден", userId);
                }
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ошибка при получении данных из PostgreSQL.");
                return null;
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

        public async Task<List<Technician>> GetTechniciansAsync()
        {
            try
            {
                var technicians = await _context.Users
                    .OfType<Technician>() 
                    .ToListAsync();

                _logger.LogInformation("📥 [PostgreSQL] Загружено {Count} техников.", technicians.Count);
                return technicians;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ошибка при получении данных о техниках из PostgreSQL.");
                return [];
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

        public async Task DeleteUserAsync(Guid userId)
        {
            try
            {
                var user = await GetUserByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("❌ Пользователь с ID: {UserId} не найден", userId);
                    return;
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                _logger.LogInformation("🗑️ [PostgreSQL] Пользователь удалён с ID: {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ошибка при удалении данных из PostgreSQL.");
            }
        }
    }
}
