using SuperAppModels.Models;

namespace SuperAppDataRepositories.Ins
{
    public interface IUserProfileRepository
    {
        Task<UserProfile?> GetByEmailAsync(string email);
        Task<List<UserProfile>> GetAllAsync();
        Task<UserProfile> CreateAsync(UserProfile userProfile);
        Task<UserProfile> UpdateAsync(UserProfile userProfile);
        Task<UserProfile> CreateOrUpdateAsync(UserProfile userProfile);
        Task<bool> DeleteAsync(string email);
        Task<bool> ExistsAsync(string email);
    }
}
