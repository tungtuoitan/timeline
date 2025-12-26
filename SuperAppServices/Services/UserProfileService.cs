using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs;
using SuperAppModels.Models;
using SuperAppServices.Interfaces;

namespace SuperAppServices.Services
{
    /// <summary>
    /// Service for user profile operations
    /// </summary>
    public class UserProfileService : IUserProfileService
    {
        private readonly IUserProfileRepository _repository;
        private readonly ILogger<UserProfileService> _logger;

        public UserProfileService(
            IUserProfileRepository repository,
            ILogger<UserProfileService> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Get user profile by userId
        /// </summary>
        public async Task<UserProfile?> GetUserProfileByUserIdAsync(int userId)
        {
            try
            {
                if (userId <= 0)
                {
                    _logger.LogWarning("Invalid userId provided for GetUserProfileByUserIdAsync: {UserId}", userId);
                    return null;
                }

                _logger.LogInformation("Getting user profile for userId: {UserId}", userId);

                var userProfile = await _repository.GetByUserIdAsync(userId);

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
                _logger.LogError(ex, "Error occurred while getting user profile for userId: {UserId}", userId);
                throw;
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

                var result = await _repository.CreateOrUpdateAsync(userProfile);

                _logger.LogInformation("Successfully created/updated user profile for userId: {UserId}", userProfile.UserId);

                return new ResultOptions
                {
                    Success = true,
                    Message = "User profile saved successfully",
                    Object = result,
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

        /// <summary>
        /// Upsert user filters (insert new profile if not exists, update filters if exists)
        /// </summary>
        public async Task<ResultOptions> UpdateUserFiltersAsync(int userId, string filters)
        {
            try
            {
                if (userId <= 0)
                {
                    _logger.LogWarning("Invalid userId provided for UpdateUserFiltersAsync: {UserId}", userId);
                    return new ResultOptions
                    {
                        Success = false,
                        Message = "UserId must be greater than 0",
                        Status = 400
                    };
                }

                if (string.IsNullOrWhiteSpace(filters))
                {
                    _logger.LogWarning("Empty filters provided for UpdateUserFiltersAsync");
                    return new ResultOptions
                    {
                        Success = false,
                        Message = "Filters cannot be empty",
                        Status = 400
                    };
                }

                _logger.LogInformation("Upserting filters for userId: {UserId}", userId);

                var userProfile = await _repository.GetByUserIdAsync(userId);

                if (userProfile == null)
                {
                    // Create new profile with filters
                    _logger.LogInformation("User profile not found, creating new profile with filters for userId: {UserId}", userId);

                    var newProfile = new UserProfile(userId)
                    {
                        Filters = filters
                    };

                    var result = await _repository.CreateOrUpdateAsync(newProfile);

                    _logger.LogInformation("Successfully created user profile with filters for userId: {UserId}", userId);

                    return new ResultOptions
                    {
                        Success = true,
                        Message = "User profile created with filters successfully",
                        Object = result,
                        Status = 200
                    };
                }
                else
                {
                    // Update existing profile filters
                    _logger.LogInformation("Updating filters for existing user profile, userId: {UserId}", userId);

                    userProfile.Update(filters: filters);

                    var result = await _repository.CreateOrUpdateAsync(userProfile);

                    _logger.LogInformation("Successfully updated filters for userId: {UserId}", userId);

                    return new ResultOptions
                    {
                        Success = true,
                        Message = "Filters updated successfully",
                        Object = result,
                        Status = 200
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while upserting filters for userId: {UserId}", userId);

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
