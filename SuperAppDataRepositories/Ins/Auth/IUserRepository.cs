using SuperAppModels.Models;

namespace SuperAppDataRepositories.Ins
{
    public interface IUserRepository
    {
        Task<User?> GetUserByEmailAsync(string email);
        Task<User?> GetByEmailAsync(string email);
        Task<User> CreateUserAsync(User user);
        Task<User> CreateAsync(User user);
        Task UpdateLastLoginAsync(int userId);
        Task UpdateAsync(User user);
    }
}
