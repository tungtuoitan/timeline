using SuperAppModels.DTOs;
using SuperAppModels.Models;

namespace UserProfileDataRepositories.Ins
{
    public interface IUserProfileRepository
    {
        // Modern methods with full CRUD support
        Task<UserProfile?> GetUserProfileByEmailAsync(string email);
        Task<UserProfile> CreateUserProfileAsync(UserProfile userProfile);
        Task<UserProfile> UpdateUserProfileAsync(UserProfile userProfile);
        Task<bool> DeleteUserProfileAsync(int id);
        
        // Legacy method for backward compatibility
        Task<ResultOptions?> IuUserProfile(string email, string appC, string jsonProfile);
    }
}
