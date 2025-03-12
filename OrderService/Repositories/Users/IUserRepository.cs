using OrderService.Models.Users;

namespace OrderService.Repositories.Users
{
    public interface IUserRepository
    {
        Task<User?> GetUserByIdAsync(Guid id);
        Task<List<User>> GetAllUsersAsync();
        Task SaveUserAsync(User user);
        Task UpdateUserAsync(User user);
    }
}
