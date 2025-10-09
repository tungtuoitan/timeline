using System.Data;
using SuperAppModels.Models;
using System.Data.SqlClient;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Ins;
using SuperAppDataRepositories.Data;

namespace SuperAppDataRepositories.Repositories
{
    public class AuthRepository : BaseRepository, IAuthRepository
    {
        public AuthRepository(ILogger<AuthRepository> logger, IConnectionFactory connectionFactory)
            : base(connectionFactory, logger)
        {
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            try
            {
                _logger.LogInformation("Getting user by email: {Email}", email);

                if (string.IsNullOrEmpty(email))
                {
                    throw new ArgumentException("Email cannot be null or empty", nameof(email));
                }

                return await ExecuteStoredProcedureAsync(
                    StoredProcedures.spSelectUserByEmail,
                    addParameters: async (command) =>
                    {
                        command.Parameters.Add(new SqlParameter("@iv_Email", email));
                        await Task.CompletedTask;
                    },
                    mapResult: MapToSingleAsync<User>,
                    useSuperAppConnection: true
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting user by email: {Email}", email);
                throw;
            }
        }

        public async Task<User?> GetUserByPhoneAsync(string phone)
        {
            try
            {
                _logger.LogInformation("Getting user by phone: {Phone}", phone);

                if (string.IsNullOrEmpty(phone))
                {
                    throw new ArgumentException("Phone cannot be null or empty", nameof(phone));
                }

                return await ExecuteStoredProcedureAsync(
                    StoredProcedures.spSelectUserByPhone,
                    addParameters: async (command) =>
                    {
                        command.Parameters.Add(new SqlParameter("@iv_Phone", phone));
                        await Task.CompletedTask;
                    },
                    mapResult: MapToSingleAsync<User>,
                    useSuperAppConnection: true
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting user by phone: {Phone}", phone);
                throw;
            }
        }

        public async Task<User> CreateUserAsync(User user)
        {
            try
            {
                _logger.LogInformation("Creating new user with email: {Email}", user.Email);

                if (string.IsNullOrEmpty(user.Email))
                {
                    throw new ArgumentException("User email cannot be null or empty", nameof(user));
                }

                var (users, outputParams) = await ExecuteStoredProcedureWithOutputAsync(
                    StoredProcedures.spInsertUser,
                    addParametersAndGetOutputs: async (command) =>
                    {
                        // Set ID to 0 for new users
                        user.Id = 0;
                        DataTable userTable = user.ToDataTable();
                        AddStructuredParameter(command, "@User", userTable);
                        var errorMsg = AddOutputParameter(command, "@ov_ErrorMsg", SqlDbType.VarChar, -1);
                        await Task.CompletedTask;
                        return new[] { errorMsg };
                    },
                    mapResult: MapToListAsync<User>,
                    useSuperAppConnection: true
                );

                var errorParam = outputParams[0];
                if (HasError(errorParam, out string errorMessage))
                {
                    _logger.LogError("Error creating user: {ErrorMessage}", errorMessage);
                    throw new InvalidOperationException($"Failed to create user: {errorMessage}");
                }

                var createdUser = users.FirstOrDefault();
                if (createdUser == null)
                {
                    throw new InvalidOperationException("Failed to create user: No user returned from database");
                }

                _logger.LogInformation("Successfully created user with ID: {Id}", createdUser.Id);
                return createdUser;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating user");
                throw;
            }
        }

        public async Task<User> UpdateUserAsync(User user)
        {
            try
            {
                _logger.LogInformation("Updating user with ID: {Id}", user.Id);

                if (user.Id <= 0)
                {
                    throw new ArgumentException("User ID must be greater than 0 for updates", nameof(user));
                }

                if (string.IsNullOrEmpty(user.Email))
                {
                    throw new ArgumentException("User email cannot be null or empty", nameof(user));
                }

                var (users, outputParams) = await ExecuteStoredProcedureWithOutputAsync(
                    StoredProcedures.spUpdateUser,
                    addParametersAndGetOutputs: async (command) =>
                    {
                        DataTable userTable = user.ToDataTable();
                        AddStructuredParameter(command, "@User", userTable);
                        var errorMsg = AddOutputParameter(command, "@ov_ErrorMsg", SqlDbType.VarChar, -1);
                        await Task.CompletedTask;
                        return new[] { errorMsg };
                    },
                    mapResult: MapToListAsync<User>,
                    useSuperAppConnection: true
                );

                var errorParam = outputParams[0];
                if (HasError(errorParam, out string errorMessage))
                {
                    _logger.LogError("Error updating user: {ErrorMessage}", errorMessage);
                    throw new InvalidOperationException($"Failed to update user: {errorMessage}");
                }

                var updatedUser = users.FirstOrDefault();
                if (updatedUser == null)
                {
                    throw new InvalidOperationException("Failed to update user: No user returned from database");
                }

                _logger.LogInformation("Successfully updated user with ID: {Id}", updatedUser.Id);
                return updatedUser;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating user with ID: {Id}", user.Id);
                throw;
            }
        }

        public async Task<bool> ValidateCredentialsAsync(string email, string password)
        {
            try
            {
                _logger.LogInformation("Validating credentials for email: {Email}", email);

                if (string.IsNullOrEmpty(email))
                {
                    throw new ArgumentException("Email cannot be null or empty", nameof(email));
                }

                if (string.IsNullOrEmpty(password))
                {
                    throw new ArgumentException("Password cannot be null or empty", nameof(password));
                }

                var (result, outputParams) = await ExecuteStoredProcedureWithOutputAsync<bool>(
                    StoredProcedures.spValidateUserCredentials,
                    addParametersAndGetOutputs: async (command) =>
                    {
                        command.Parameters.Add(new SqlParameter("@iv_Email", email));
                        command.Parameters.Add(new SqlParameter("@iv_Password", password));
                        var isValid = AddOutputParameter(command, "@ov_IsValid", SqlDbType.Bit);
                        var errorMsg = AddOutputParameter(command, "@ov_ErrorMsg", SqlDbType.VarChar, -1);
                        await Task.CompletedTask;
                        return new[] { isValid, errorMsg };
                    },
                    mapResult: async (reader) =>
                    {
                        await Task.CompletedTask;
                        return false; // Default value, actual result comes from output parameter
                    },
                    useSuperAppConnection: true
                );

                var isValidParam = outputParams[0];
                var errorParam = outputParams[1];

                if (HasError(errorParam, out string errorMessage))
                {
                    _logger.LogError("Error validating credentials: {ErrorMessage}", errorMessage);
                    throw new InvalidOperationException($"Failed to validate credentials: {errorMessage}");
                }

                bool isValid = isValidParam.Value != DBNull.Value && (bool)isValidParam.Value;
                
                if (isValid)
                {
                    _logger.LogInformation("Credentials validated successfully for email: {Email}", email);
                }
                else
                {
                    _logger.LogWarning("Invalid credentials for email: {Email}", email);
                }

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while validating credentials for email: {Email}", email);
                throw;
            }
        }

        public async Task<User?> AuthenticateUserAsync(string email, string password)
        {
            try
            {
                _logger.LogInformation("Authenticating user with email: {Email}", email);

                if (string.IsNullOrEmpty(email))
                {
                    throw new ArgumentException("Email cannot be null or empty", nameof(email));
                }

                if (string.IsNullOrEmpty(password))
                {
                    throw new ArgumentException("Password cannot be null or empty", nameof(password));
                }

                // First validate credentials
                bool isValid = await ValidateCredentialsAsync(email, password);
                if (!isValid)
                {
                    _logger.LogWarning("Authentication failed for email: {Email} - invalid credentials", email);
                    return null;
                }

                // If credentials are valid, get the user
                var user = await GetUserByEmailAsync(email);
                if (user != null)
                {
                    _logger.LogInformation("User authenticated successfully with email: {Email}", email);
                }

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while authenticating user with email: {Email}", email);
                throw;
            }
        }
    }
}