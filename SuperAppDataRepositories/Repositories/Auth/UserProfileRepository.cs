using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Data;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs;
using SuperAppModels.Models;
using SuperAppModels.Utils;

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
        public async Task<ResultOptions> UpsertUserProfileAsync(UserProfile profile)
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
                    // Update existing profile - only update fields that are not null
                    _logger.LogInformation("Updating existing user profile ID: {ProfileId} for userId: {UserId}",
                        existingProfile.Id, profile.UserId);

                    // Update each field only if it has a value (not null)
                    if (profile.FirstName != null) existingProfile.FirstName = profile.FirstName;
                    if (profile.LastName != null) existingProfile.LastName = profile.LastName;
                    if (profile.AvatarUrl != null) existingProfile.AvatarUrl = profile.AvatarUrl;
                    if (profile.Bio != null) existingProfile.Bio = profile.Bio;
                    if (profile.DateOfBirth.HasValue) existingProfile.DateOfBirth = profile.DateOfBirth;
                    if (profile.Gender != null) existingProfile.Gender = profile.Gender;
                    if (profile.Country != null) existingProfile.Country = profile.Country;
                    if (profile.City != null) existingProfile.City = profile.City;
                    if (profile.Timezone != null) existingProfile.Timezone = profile.Timezone;
                    if (profile.Language != null) existingProfile.Language = profile.Language;
                    if (profile.Filters != null) existingProfile.Filters = profile.Filters;

                    // K Repo Sync fields
                    if (profile.KRepoUrl       != null) existingProfile.KRepoUrl           = profile.KRepoUrl;
                    if (profile.KRepoBranch     != null) existingProfile.KRepoBranch        = profile.KRepoBranch;
                    if (profile.KRepoPat        != null) existingProfile.KRepoPat           = profile.KRepoPat;
                    if (profile.KRepoStatusCode != null) existingProfile.KRepoStatusCode    = profile.KRepoStatusCode;
                    if (profile.KRepoContentHash   != null) existingProfile.KRepoContentHash   = profile.KRepoContentHash;
                    if (profile.KRepoLastPushSha   != null) existingProfile.KRepoLastPushSha   = profile.KRepoLastPushSha;
                    if (profile.KRepoLastPushAt    != null) existingProfile.KRepoLastPushAt    = profile.KRepoLastPushAt;
                    if (profile.KRepoLastCheckAt   != null) existingProfile.KRepoLastCheckAt   = profile.KRepoLastCheckAt;
                    if (profile.KRepoLastRemoteSha != null) existingProfile.KRepoLastRemoteSha = profile.KRepoLastRemoteSha;

                    existingProfile.UpdatedAt = VietnamDateTime.Now();

                    _context.UserProfiles.Update(existingProfile);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Successfully updated user profile ID: {ProfileId}", existingProfile.Id);
                    return new ResultOptions
                    {
                        Success = true,
                        Message = "Update User Profile successfully."
                    };
                }
                else
                {
                    // Create new profile
                    _logger.LogInformation("Creating new user profile for userId: {UserId}", profile.UserId);

                    profile.CreatedAt = VietnamDateTime.Now();
                    profile.UpdatedAt = VietnamDateTime.Now();

                    _context.UserProfiles.Add(profile);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Successfully created user profile ID: {ProfileId}", profile.Id);
                    return new ResultOptions
                    {
                        Success = true,
                        Message = "Insert User Profile successfully."
                    };
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
    }
}
