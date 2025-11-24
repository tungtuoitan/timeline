using SuperAppModels.Models;

namespace SuperAppDataRepositories.Ins
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(int userId);
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByUsernameAsync(string username);
        Task<List<User>> GetAllAsync();
        Task<User> CreateAsync(User user);
        Task<User> UpdateAsync(User user);
        Task<bool> DeleteAsync(int userId);
        Task<bool> ExistsAsync(string email);
        Task<bool> ValidateCredentialsAsync(string email, string passwordHash);
    }
}
