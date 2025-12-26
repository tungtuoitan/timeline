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
        /// <returns>UserProfile or null if not found</returns>
        Task<UserProfile?> GetUserProfileByUserIdAsync(int userId);

        /// <summary>
        /// Create or update user profile
        /// </summary>
        /// <param name="userProfile">User profile to save</param>
        /// <returns>ResultOptions with success status</returns>
        Task<ResultOptions> UpsertUserProfileAsync(UserProfile userProfile);

        /// <summary>
        /// Update user filters
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="filters">Filter settings JSON</param>
        /// <returns>ResultOptions with success status</returns>
        Task<ResultOptions> UpdateUserFiltersAsync(int userId, string filters);
    }
}
