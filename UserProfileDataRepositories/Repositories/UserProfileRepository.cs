using System.Data;
using SuperAppModels.Models;
using SuperAppModels.DTOs;
using System.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using UserProfileDataRepositories.Ins;
using SuperAppDataRepositories.Data;
using SuperAppDataRepositories.Repositories;

namespace UserProfileDataRepositories.Repositories
{
    public class UserProfileRepository : BaseRepository, IUserProfileRepository
    {
        public UserProfileRepository(ILogger<UserProfileRepository> logger, IConnectionFactory connectionFactory)
            : base(connectionFactory, logger)
        {
        }

        public async Task<UserProfile?> GetUserProfileByEmailAsync(string email)
        {
            try
            {
                _logger.LogInformation("Getting user profile by email: {Email}", email);

                if (string.IsNullOrEmpty(email))
                {
                    throw new ArgumentException("Email cannot be null or empty", nameof(email));
                }

                return await ExecuteStoredProcedureAsync(
                    StoredProcedures.spSelectUserProfileByEmail,
                    addParameters: async (command) =>
                    {
                        command.Parameters.Add(new SqlParameter("@iv_Email", email));
                        await Task.CompletedTask;
                    },
                    mapResult: MapToSingleAsync<UserProfile>,
                    useSuperAppConnection: false
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting user profile by email: {Email}", email);
                throw;
            }
        }

        public async Task<UserProfile> CreateUserProfileAsync(UserProfile userProfile)
        {
            try
            {
                _logger.LogInformation("Creating user profile for email: {Email}", userProfile.Email);

                if (string.IsNullOrEmpty(userProfile.Email))
                {
                    throw new ArgumentException("UserProfile email cannot be null or empty", nameof(userProfile));
                }

                // Convert UserProfile to JSON string and use existing stored procedure
                string jsonProfile = JsonConvert.SerializeObject(userProfile);
                
                var (_, outputParams) = await ExecuteStoredProcedureWithOutputAsync<object?>(
                    StoredProcedures.spInsertUpdateUserProfile,
                    addParametersAndGetOutputs: async (command) =>
                    {
                        AddParameterIfNotNull(command, "@iv_Email", userProfile.Email);
                        AddParameterIfNotNull(command, "@iv_AppC", userProfile.AppC);
                        AddParameterIfNotNull(command, "@iv_Json", jsonProfile);
                        var errorMsg = AddOutputParameter(command, "@ov_ErrorMsg", SqlDbType.VarChar, -1);
                        await Task.CompletedTask;
                        return new[] { errorMsg };
                    },
                    mapResult: async (reader) =>
                    {
                        await Task.CompletedTask;
                        return (object?)null;
                    },
                    useSuperAppConnection: false
                );

                var errorParam = outputParams[0];
                if (HasError(errorParam, out string errorMessage))
                {
                    _logger.LogError("Error creating user profile: {ErrorMessage}", errorMessage);
                    throw new InvalidOperationException($"Failed to create user profile: {errorMessage}");
                }

                // Return the updated profile by fetching it
                var createdProfile = await GetUserProfileByEmailAsync(userProfile.Email);
                if (createdProfile == null)
                {
                    throw new InvalidOperationException("Failed to create user profile: Profile not found after creation");
                }

                _logger.LogInformation("Successfully created user profile for email: {Email}", userProfile.Email);
                return createdProfile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating user profile");
                throw;
            }
        }

        public async Task<UserProfile> UpdateUserProfileAsync(UserProfile userProfile)
        {
            try
            {
                _logger.LogInformation("Updating user profile for email: {Email}", userProfile.Email);

                if (string.IsNullOrEmpty(userProfile.Email))
                {
                    throw new ArgumentException("UserProfile email cannot be null or empty", nameof(userProfile));
                }

                // Convert UserProfile to JSON string and use existing stored procedure
                string jsonProfile = JsonConvert.SerializeObject(userProfile);
                
                var (_, outputParams) = await ExecuteStoredProcedureWithOutputAsync<object?>(
                    StoredProcedures.spInsertUpdateUserProfile,
                    addParametersAndGetOutputs: async (command) =>
                    {
                        AddParameterIfNotNull(command, "@iv_Email", userProfile.Email);
                        AddParameterIfNotNull(command, "@iv_AppC", userProfile.AppC);
                        AddParameterIfNotNull(command, "@iv_Json", jsonProfile);
                        var errorMsg = AddOutputParameter(command, "@ov_ErrorMsg", SqlDbType.VarChar, -1);
                        await Task.CompletedTask;
                        return new[] { errorMsg };
                    },
                    mapResult: async (reader) =>
                    {
                        await Task.CompletedTask;
                        return (object?)null;
                    },
                    useSuperAppConnection: false
                );

                var errorParam = outputParams[0];
                if (HasError(errorParam, out string errorMessage))
                {
                    _logger.LogError("Error updating user profile: {ErrorMessage}", errorMessage);
                    throw new InvalidOperationException($"Failed to update user profile: {errorMessage}");
                }

                // Return the updated profile by fetching it
                var updatedProfile = await GetUserProfileByEmailAsync(userProfile.Email);
                if (updatedProfile == null)
                {
                    throw new InvalidOperationException("Failed to update user profile: Profile not found after update");
                }

                _logger.LogInformation("Successfully updated user profile for email: {Email}", userProfile.Email);
                return updatedProfile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating user profile for email: {Email}", userProfile.Email);
                throw;
            }
        }

        public async Task<bool> DeleteUserProfileAsync(int id)
        {
            try
            {
                _logger.LogInformation("Delete operation not supported for user profiles with ID: {Id}", id);
                
                // Since there's no delete stored procedure and UserProfile doesn't have ID,
                // we'll throw a not supported exception or return false
                throw new NotSupportedException("Delete operation is not supported for user profiles. Use email-based operations instead.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while attempting to delete user profile with ID: {Id}", id);
                throw;
            }
        }

        // Legacy method for backward compatibility
        public async Task<ResultOptions?> IuUserProfile(string email, string appC, string jsonProfile)
        {
            try
            {
                _logger.LogInformation("Legacy IuUserProfile called for email: {Email}", email);

                if (string.IsNullOrEmpty(email))
                {
                    return new ResultOptions
                    {
                        Success = false,
                        ErrorMessage = "Email cannot be null or empty"
                    };
                }

                var (_, outputParams) = await ExecuteStoredProcedureWithOutputAsync<object?>(
                    StoredProcedures.spInsertUpdateUserProfile,
                    addParametersAndGetOutputs: async (command) =>
                    {
                        AddParameterIfNotNull(command, "@iv_Email", email);
                        AddParameterIfNotNull(command, "@iv_AppC", appC);
                        AddParameterIfNotNull(command, "@iv_Json", jsonProfile);
                        var errorMsg = AddOutputParameter(command, "@ov_ErrorMsg", SqlDbType.VarChar, -1);
                        await Task.CompletedTask;
                        return new[] { errorMsg };
                    },
                    mapResult: async (reader) =>
                    {
                        await Task.CompletedTask;
                        return (object?)null;
                    },
                    useSuperAppConnection: false
                );

                var errorParam = outputParams[0];
                if (HasError(errorParam, out string errorMessage))
                {
                    _logger.LogError("Error in legacy IuUserProfile: {ErrorMessage}", errorMessage);
                    return new ResultOptions
                    {
                        Success = false,
                        ErrorMessage = errorMessage
                    };
                }

                _logger.LogInformation("Successfully executed legacy IuUserProfile for email: {Email}", email);
                return new ResultOptions
                {
                    Success = true,
                    Message = "User profile updated successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in legacy IuUserProfile for email: {Email}", email);
                return new ResultOptions
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }
    }
}
