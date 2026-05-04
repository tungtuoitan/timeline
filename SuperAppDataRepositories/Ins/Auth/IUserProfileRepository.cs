using SuperAppModels.DTOs;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Ins
{
    public interface IUserProfileRepository
    {
        Task<UserProfile?> GetByUserIdAsync(int userId);
        Task<ResultOptions> UpsertUserProfileAsync(UserProfile profile);
    }
}
