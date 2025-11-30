using SuperAppModels.Models;

namespace SuperAppDataRepositories.Ins
{
    public interface IUserProfileRepository
    {
        Task<UserProfile?> GetUserProfileAsync(string email);
        Task<UserProfile?> GetByEmailAsync(string email);
        Task<UserProfile> CreateOrUpdateUserProfileAsync(UserProfile profile);
        Task<UserProfile> CreateOrUpdateAsync(UserProfile profile);
    }
}
