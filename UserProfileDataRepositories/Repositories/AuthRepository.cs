using System.Data;
using SuperAppModels.Models;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.DTOs.Responses;
using System.Data.SqlClient;
using Microsoft.Extensions.Logging;
using UserProfileDataRepositories.Ins;
using SuperAppDataRepositories.Data;
using SuperAppDataRepositories.Repositories;
using SuperAppDataRepositories.Extensions;
using PLMModels.DTOs;

namespace UserProfileDataRepositories.Repositories
{
    public class AuthRepository : BaseRepository, IAuthRepository
    {
        public AuthRepository(ILogger<AuthRepository> logger, IConnectionFactory connectionFactory)
            : base(connectionFactory, logger)
        {
        }

        // Modern interface methods
        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            // Placeholder implementation - convert to use the legacy method for now
            var userModel = new UserModel
            {
                Email = request.Email,
                Password = request.Password,
                Type = "loginDefault"
            };

            // For now, use the legacy method and convert the response
            // This should be refactored to use proper domain models
            return new AuthResponse
            {
                Success = false,
                Message = "Modern authentication not yet implemented - use legacy methods",
                User = null
            };
        }

        public async Task<AuthResponse> SignupAsync(SignupRequest request)
        {
            // Placeholder implementation
            return new AuthResponse
            {
                Success = false,
                Message = "Modern signup not yet implemented - use legacy methods",
                User = null
            };
        }

        public async Task<AuthResponse> GoogleAuthAsync(GoogleCodeRequest request)
        {
            // Placeholder implementation
            return new AuthResponse
            {
                Success = false,
                Message = "Google authentication not yet implemented - use legacy methods",
                User = null
            };
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            try
            {
                var users = await ExecuteStoredProcedureAsync(
                    StoredProcedures.spSelectUsers,
                    addParameters: async (command) =>
                    {
                        await Task.CompletedTask;
                    },
                    mapResult: async (reader) =>
                    {
                        var usersList = new List<UserModel>();
                        while (await reader.ReadAsync())
                        {
                            var userModel = new UserModel
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Email = reader["Email"] != DBNull.Value ? reader["Email"].ToString() : null,
                                Phone = reader["Phone"] != DBNull.Value ? reader["Phone"].ToString() : null,
                                FirstName = reader["FirstName"] != DBNull.Value ? reader["FirstName"].ToString() : null,
                                LastName = reader["LastName"] != DBNull.Value ? reader["LastName"].ToString() : null,
                                Birthday = reader["Birthday"] != DBNull.Value ? DateOnly.FromDateTime((DateTime)reader["Birthday"]) : null,
                                Password = reader["Password"] != DBNull.Value ? reader["Password"].ToString() : null
                            };
                            usersList.Add(userModel);
                        }
                        return usersList;
                    },
                    useSuperAppConnection: false
                );

                var userModel = users?.FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
                if (userModel == null) return null;

                // Convert UserModel to User (mapping to actual User model properties)
                return new User
                {
                    UserId = userModel.Id, // Map Id to UserId
                    Username = userModel.Email ?? string.Empty,
                    Email = userModel.Email ?? string.Empty,
                    PasswordHash = string.Empty, // Set default, should be populated from userModel if available
                    // Removed properties that don't exist in User model: Phone, FirstName, LastName, Birthday
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting user by email: {Email}", email);
                return null;
            }
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            // Placeholder implementation
            await Task.CompletedTask;
            return null;
        }

        public async Task<bool> ValidateUserCredentialsAsync(string email, string password)
        {
            // Placeholder implementation
            await Task.CompletedTask;
            return false;
        }

        // Legacy methods for backward compatibility - fix parameter name to match interface
        public async Task<ResultOptions2<UserModel>> IuUser(UserModel user)
        {
            var (userResult, outputParams) = await ExecuteStoredProcedureWithOutputAsync(
                StoredProcedures.spInsertUpdateUser,
                addParametersAndGetOutputs: async (command) =>
                {
                    var table = user.ToDataTable();
                    AddStructuredParameter(command, "@User", table);
                    var msg = AddOutputParameter(command, "@ov_ErrorMsg", SqlDbType.NVarChar, -1);
                    await Task.CompletedTask;
                    return new[] { msg };
                },
                mapResult: async (reader) =>
                {
                    UserModel userModel = new UserModel();
                    if (await reader.ReadAsync())
                    {
                        userModel.Id = Convert.ToInt32(reader["Id"]);
                        userModel.Email = reader["Email"] != DBNull.Value ? reader["Email"].ToString() : null;
                        userModel.Phone = reader["Phone"] != DBNull.Value ? reader["Phone"].ToString() : null;
                        userModel.FirstName = reader["FirstName"] != DBNull.Value ? reader["FirstName"].ToString() : null;
                        userModel.LastName = reader["LastName"] != DBNull.Value ? reader["LastName"].ToString() : null;
                        userModel.Birthday = reader["Birthday"] != DBNull.Value ? DateOnly.FromDateTime((DateTime)reader["Birthday"]) : null;
                        userModel.Password = reader["Password"] != DBNull.Value ? reader["Password"].ToString() : null;
                    }
                    return userModel;
                },
                useSuperAppConnection: false
            );

            var errorParam = outputParams[0];
            if (HasError(errorParam, out string errorMessage))
            {
                throw new Exception(errorMessage);
            }

            return new ResultOptions2<UserModel>
            {
                Message = "Sign up successfully",
                Success = true,
                Data = userResult
            };
        }

        public async Task<ResultOptions2<List<UserModel>>> GetUsers()
        {
            var users = await ExecuteStoredProcedureAsync(
                StoredProcedures.spSelectUsers,
                addParameters: async (command) =>
                {
                    await Task.CompletedTask;
                },
                mapResult: async (reader) =>
                {
                    List<UserModel> list = new();
                    while (await reader.ReadAsync())
                    {
                        UserModel userModel = new();
                        userModel.Id = Convert.ToInt32(reader["Id"]);
                        userModel.Email = reader["Email"] != DBNull.Value ? reader["Email"].ToString() : null;
                        userModel.Phone = reader["Phone"] != DBNull.Value ? reader["Phone"].ToString() : null;
                        userModel.FirstName = reader["FirstName"] != DBNull.Value ? reader["FirstName"].ToString() : null;
                        userModel.LastName = reader["LastName"] != DBNull.Value ? reader["LastName"].ToString() : null;
                        userModel.Birthday = reader["Birthday"] != DBNull.Value ? DateOnly.FromDateTime((DateTime)reader["Birthday"]) : null;
                        userModel.Password = reader["Password"] != DBNull.Value ? reader["Password"].ToString() : null;
                        list.Add(userModel);
                    }
                    return list;
                },
                useSuperAppConnection: false
            );

            return new ResultOptions2<List<UserModel>>
            {
                Success = true,
                Data = users
            };
        }

        public async Task<ResultOptions> IuUserProfile(string email, string appC, string json)
        {
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
                return new ResultOptions
                {
                    Message = "Successfully update user profile",
                    Success = true,
                };
            }
            else
            {
                return new ResultOptions
                {
                    Message = "Error update user profile",
                    Success = false,
                };
            }
        }
    }
}