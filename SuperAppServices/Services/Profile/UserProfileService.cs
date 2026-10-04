using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs;
using SuperAppModels.Models;
using SuperAppServices.Interfaces;
using SuperAppServices.Time;

namespace SuperAppServices.Services.Profile
{
    /// <summary>
    /// Service for user profile operations
    /// </summary>
    public class UserProfileService : IUserProfileService
    {
        private readonly IUserProfileRepository _repository;
        private readonly ILogger<UserProfileService> _logger;
        private readonly IUserTimeZoneResolver _timeZoneResolver;

        public UserProfileService(
            IUserProfileRepository repository,
            ILogger<UserProfileService> logger,
            IUserTimeZoneResolver timeZoneResolver)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _timeZoneResolver = timeZoneResolver ?? throw new ArgumentNullException(nameof(timeZoneResolver));
        }

        /// <summary>
        /// Get user profile by userId
        /// </summary>
        public async Task<ResultOptions> GetUserProfileByUserIdAsync(int userId)
        {
            try
            {
                if (userId <= 0)
                {
                    _logger.LogWarning("Invalid userId provided for GetUserProfileByUserIdAsync: {UserId}", userId);
                    return new ResultOptions
                    {
                        Success = false,
                        Message = "UserId must be greater than 0",
                        Status = 400
                    };
                }

                _logger.LogInformation("Getting user profile for userId: {UserId}", userId);

                var userProfile = await _repository.GetByUserIdAsync(userId);

                if (userProfile == null)
                {
                    _logger.LogWarning("User profile not found for userId: {UserId}", userId);
                    return new ResultOptions
                    {
                        Success = false,
                        Message = "User profile not found",
                        Status = 404
                    };
                }

                _logger.LogInformation("Successfully retrieved user profile for userId: {UserId}", userId);
                return new ResultOptions
                {
                    Success = true,
                    Message = "User profile retrieved successfully",
                    Object = userProfile,
                    Status = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting user profile for userId: {UserId}", userId);
                return new ResultOptions
                {
                    Success = false,
                    Message = ex.Message,
                    Status = 500
                };
            }
        }

        /// <summary>
        /// Create or update user profile
        /// </summary>
        public async Task<ResultOptions> UpsertUserProfileAsync(UserProfile userProfile)
        {
            try
            {
                if (userProfile == null)
                {
                    _logger.LogWarning("Null user profile provided for UpsertUserProfileAsync");
                    return new ResultOptions
                    {
                        Success = false,
                        Message = "User profile cannot be null",
                        Status = 400
                    };
                }

                _logger.LogInformation("Creating or updating user profile for userId: {UserId}",
                    userProfile.UserId);

                var result = await _repository.UpsertUserProfileAsync(userProfile);
                if (!result.Success)
                {
                    throw new Exception("Fail to create/update user profile."); 
                }
                // Timezone may have changed — next request re-reads it.
                _timeZoneResolver.Invalidate(userProfile.UserId);
                var res = await _repository.GetByUserIdAsync(userProfile.UserId);

                _logger.LogInformation("Successfully created/updated user profile for userId: {UserId}", userProfile.UserId);

                return new ResultOptions
                {
                    Success = true,
                    Message = "User profile saved successfully",
                    Object = res,
                    Status = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating/updating user profile for userId: {UserId}",
                    userProfile?.UserId);

                return new ResultOptions
                {
                    Success = false,
                    Message = ex.Message,
                    Status = 500
                };
            }
        }
    }
}
