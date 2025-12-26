using SuperAppModels.Models;

namespace SuperAppDataRepositories.Ins
{
    public interface IUserProfileRepository
    {
        Task<UserProfile?> GetByUserIdAsync(int userId);
        Task<UserProfile> UpsertUserProfileAsync(UserProfile profile);
        Task<UserProfile> CreateOrUpdateAsync(UserProfile profile);
    }
}
