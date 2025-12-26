using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Data;
using SuperAppDataRepositories.Ins;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Repositories
{
    /// <summary>
    /// Repository for user profile data access with proper error handling
    /// </summary>
    public class UserProfileRepository : IUserProfileRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserProfileRepository> _logger;

        public UserProfileRepository(
            ApplicationDbContext context,
            ILogger<UserProfileRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Get user profile by userId
        /// </summary>
        public async Task<UserProfile?> GetByUserIdAsync(int userId)
        {
            try
            {
                if (userId <= 0)
                {
                    _logger.LogWarning("Invalid userId provided for GetByUserIdAsync: {UserId}", userId);
                    return null;
                }

                _logger.LogInformation("Getting user profile by userId: {UserId}", userId);

                var userProfile = await _context.UserProfiles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (userProfile == null)
                {
                    _logger.LogWarning("User profile not found for userId: {UserId}", userId);
                    return null;
                }

                _logger.LogInformation("Successfully retrieved user profile for userId: {UserId}", userId);
                return userProfile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user profile by userId: {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Create or update user profile
        /// </summary>
        public async Task<UserProfile> CreateOrUpdateAsync(UserProfile profile)
        {
            try
            {
                if (profile == null)
                {
                    throw new ArgumentNullException(nameof(profile));
                }

                _logger.LogInformation("Creating or updating user profile for userId: {UserId}", profile.UserId);

                // Check if profile exists
                var existingProfile = await _context.UserProfiles
                    .FirstOrDefaultAsync(p => p.UserId == profile.UserId);

                if (existingProfile != null)
                {
                    // Update existing profile
                    _logger.LogInformation("Updating existing user profile ID: {ProfileId} for userId: {UserId}",
                        existingProfile.Id, profile.UserId);

                    existingProfile.FirstName = profile.FirstName;
                    existingProfile.LastName = profile.LastName;
                    existingProfile.AvatarUrl = profile.AvatarUrl;
                    existingProfile.Bio = profile.Bio;
                    existingProfile.DateOfBirth = profile.DateOfBirth;
                    existingProfile.Gender = profile.Gender;
                    existingProfile.Country = profile.Country;
                    existingProfile.City = profile.City;
                    existingProfile.Timezone = profile.Timezone;
                    existingProfile.Language = profile.Language;
                    existingProfile.Filters = profile.Filters;
                    existingProfile.UpdatedAt = DateTime.UtcNow;

                    _context.UserProfiles.Update(existingProfile);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Successfully updated user profile ID: {ProfileId}", existingProfile.Id);
                    return existingProfile;
                }
                else
                {
                    // Create new profile
                    _logger.LogInformation("Creating new user profile for userId: {UserId}", profile.UserId);

                    profile.CreatedAt = DateTime.UtcNow;
                    profile.UpdatedAt = DateTime.UtcNow;

                    _context.UserProfiles.Add(profile);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Successfully created user profile ID: {ProfileId}", profile.Id);
                    return profile;
                }
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while creating/updating user profile for userId: {UserId}",
                    profile?.UserId);
                throw new InvalidOperationException("Database error while saving user profile", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating/updating user profile for userId: {UserId}",
                    profile?.UserId);
                throw;
            }
        }

        /// <summary>
        /// Create or update user profile (alias for CreateOrUpdateAsync)
        /// </summary>
        public Task<UserProfile> UpsertUserProfileAsync(UserProfile profile)
        {
            return CreateOrUpdateAsync(profile);
        }
    }
}
