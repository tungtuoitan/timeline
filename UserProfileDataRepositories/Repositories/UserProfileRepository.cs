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

        public async Task<UserProfile> GetUserProfileJson(string email, string appC)
        {
            try
            {
                _logger.LogInformation("Getting user profile JSON for email: {Email}, appC: {AppC}", email, appC);

                var (userProfile, outputParams) = await ExecuteStoredProcedureWithOutputAsync(
                    StoredProcedures.spSelectUserProfileJson,
                    addParametersAndGetOutputs: async (command) =>
                    {
                        AddParameterIfNotNull(command, "@iv_Email", email);
                        AddParameterIfNotNull(command, "@iv_AppC", appC);
                        var userProfileJson = AddOutputParameter(command, "@ov_Json", SqlDbType.NVarChar, -1);
                        await Task.CompletedTask;
                        return new[] { userProfileJson };
                    },
                    mapResult: async (reader) =>
                    {
                        await Task.CompletedTask;
                        return new UserProfile();
                    },
                    useSuperAppConnection: false
                );

                var jsonParam = outputParams[0];
                var jsonString = jsonParam.Value?.ToString() ?? "";
                if (!string.IsNullOrEmpty(jsonString))
                {
                    var up = JsonConvert.DeserializeObject<UserProfile>(jsonString);
                    return up ?? new UserProfile();
                }

                return new UserProfile();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting user profile JSON for email: {Email}", email);
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

                var (profiles, outputParams) = await ExecuteStoredProcedureWithOutputAsync(
                    StoredProcedures.spInsertUserProfile,
                    addParametersAndGetOutputs: async (command) =>
                    {
                        // Set ID to 0 for new profiles
                        userProfile.Id = 0;
                        DataTable profileTable = userProfile.ToDataTable();
                        AddStructuredParameter(command, "@UserProfile", profileTable);
                        var errorMsg = AddOutputParameter(command, "@ov_ErrorMsg", SqlDbType.VarChar, -1);
                        await Task.CompletedTask;
                        return new[] { errorMsg };
                    },
                    mapResult: MapToListAsync<UserProfile>,
                    useSuperAppConnection: false
                );

                var errorParam = outputParams[0];
                if (HasError(errorParam, out string errorMessage))
                {
                    _logger.LogError("Error creating user profile: {ErrorMessage}", errorMessage);
                    throw new InvalidOperationException($"Failed to create user profile: {errorMessage}");
                }

                var createdProfile = profiles.FirstOrDefault();
                if (createdProfile == null)
                {
                    throw new InvalidOperationException("Failed to create user profile: No profile returned from database");
                }

                _logger.LogInformation("Successfully created user profile with ID: {Id}", createdProfile.Id);
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
                _logger.LogInformation("Updating user profile with ID: {Id}", userProfile.Id);

                if (userProfile.Id <= 0)
                {
                    throw new ArgumentException("UserProfile ID must be greater than 0 for updates", nameof(userProfile));
                }

                if (string.IsNullOrEmpty(userProfile.Email))
                {
                    throw new ArgumentException("UserProfile email cannot be null or empty", nameof(userProfile));
                }

                var (profiles, outputParams) = await ExecuteStoredProcedureWithOutputAsync(
                    StoredProcedures.spUpdateUserProfile,
                    addParametersAndGetOutputs: async (command) =>
                    {
                        DataTable profileTable = userProfile.ToDataTable();
                        AddStructuredParameter(command, "@UserProfile", profileTable);
                        var errorMsg = AddOutputParameter(command, "@ov_ErrorMsg", SqlDbType.VarChar, -1);
                        await Task.CompletedTask;
                        return new[] { errorMsg };
                    },
                    mapResult: MapToListAsync<UserProfile>,
                    useSuperAppConnection: false
                );

                var errorParam = outputParams[0];
                if (HasError(errorParam, out string errorMessage))
                {
                    _logger.LogError("Error updating user profile: {ErrorMessage}", errorMessage);
                    throw new InvalidOperationException($"Failed to update user profile: {errorMessage}");
                }

                var updatedProfile = profiles.FirstOrDefault();
                if (updatedProfile == null)
                {
                    throw new InvalidOperationException("Failed to update user profile: No profile returned from database");
                }

                _logger.LogInformation("Successfully updated user profile with ID: {Id}", updatedProfile.Id);
                return updatedProfile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating user profile with ID: {Id}", userProfile.Id);
                throw;
            }
        }

        public async Task<ResultOptions> IuUserProfile(string email, string appC, string json)
        {
            try
            {
                _logger.LogInformation("Insert/Update user profile JSON for email: {Email}, appC: {AppC}", email, appC);

                var (_, outputParams) = await ExecuteStoredProcedureWithOutputAsync<object?>(
                    StoredProcedures.spInsertUpdateUserProfile,
                    addParametersAndGetOutputs: async (command) =>
                    {
                        AddParameterIfNotNull(command, "@iv_Email", email);
                        AddParameterIfNotNull(command, "@iv_AppC", appC);
                        AddParameterIfNotNull(command, "@iv_Json", json);
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
                if (!HasError(errorParam, out string errorMessage))
                {
                    _logger.LogInformation("Successfully updated user profile for email: {Email}", email);
                    return new ResultOptions
                    {
                        Message = "Successfully update user profile",
                        Success = true,
                    };
                }
                else
                {
                    _logger.LogError("Error updating user profile: {ErrorMessage}", errorMessage);
                    return new ResultOptions
                    {
                        Message = $"Error update user profile: {errorMessage}",
                        Success = false,
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while insert/update user profile for email: {Email}", email);
                return new ResultOptions
                {
                    Message = $"Error update user profile: {ex.Message}",
                    Success = false,
                };
            }
        }

        public async Task<bool> DeleteUserProfileAsync(int id)
        {
            try
            {
                _logger.LogInformation("Deleting user profile with ID: {Id}", id);

                if (id <= 0)
                {
                    throw new ArgumentException("UserProfile ID must be greater than 0", nameof(id));
                }

                var (_, outputParams) = await ExecuteStoredProcedureWithOutputAsync<object?>(
                    StoredProcedures.spDeleteUserProfile,
                    addParametersAndGetOutputs: async (command) =>
                    {
                        command.Parameters.Add(new SqlParameter("@iv_Id", id));
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
                    _logger.LogError("Error deleting user profile: {ErrorMessage}", errorMessage);
                    throw new InvalidOperationException($"Failed to delete user profile: {errorMessage}");
                }

                _logger.LogInformation("Successfully deleted user profile with ID: {Id}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting user profile with ID: {Id}", id);
                throw;
            }
        }
    }
}
