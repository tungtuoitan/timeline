using SuperAppModels.DTOs;
using SuperAppModels.Models;

namespace UserProfileDataServices.Ins
{
    public interface IUserProfileSe
    {
        // Modern methods with full CRUD support
        Task<UserProfile?> GetUserProfileByEmailAsync(string email);
        Task<UserProfile> CreateUserProfileAsync(UserProfile userProfile);
        Task<UserProfile> UpdateUserProfileAsync(UserProfile userProfile);
        Task<bool> DeleteUserProfileAsync(int id);
    }
}