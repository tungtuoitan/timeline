using SuperAppDataRepositories.Ins;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Repositories
{
    public class UserProfileRepository : IUserProfileRepository
    {
        public Task<UserProfile?> GetUserProfileAsync(string email)
        {
            throw new NotImplementedException("UserProfileRepository chưa được implement - cần migrate từ code cũ");
        }

        public Task<UserProfile?> GetByEmailAsync(string email)
        {
            throw new NotImplementedException("UserProfileRepository chưa được implement - cần migrate từ code cũ");
        }

        public Task<UserProfile> CreateOrUpdateUserProfileAsync(UserProfile profile)
        {
            throw new NotImplementedException("UserProfileRepository chưa được implement - cần migrate từ code cũ");
        }

        public Task<UserProfile> CreateOrUpdateAsync(UserProfile profile)
        {
            throw new NotImplementedException("UserProfileRepository chưa được implement - cần migrate từ code cũ");
        }
    }
}
