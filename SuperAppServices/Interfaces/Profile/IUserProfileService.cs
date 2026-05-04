using SuperAppModels.DTOs;
using SuperAppModels.Models;

namespace SuperAppServices.Interfaces
{
    /// <summary>
    /// Service interface for user profile operations
    /// </summary>
    public interface IUserProfileService
    {
        /// <summary>
        /// Get user profile by userId
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>ResultOptions with UserProfile in Object field</returns>
        Task<ResultOptions> GetUserProfileByUserIdAsync(int userId);

        /// <summary>
        /// Create or update user profile
        /// </summary>
        /// <param name="userProfile">User profile to save</param>
        /// <returns>ResultOptions with success status</returns>
        Task<ResultOptions> UpsertUserProfileAsync(UserProfile userProfile);
    }
}
