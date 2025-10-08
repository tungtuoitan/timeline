using System.Data;
using SuperAppModels.Mos;
using SuperAppModels.DTOs;
using System.Data.SqlClient;
using Microsoft.Extensions.Logging;
using UserProfileDataRepositories.Ins;
using SuperAppDataRepositories.Data;
using SuperAppDataRepositories.Repositories;
using PLMModels.DTOs;

namespace UserProfileDataRepositories.Repositories
{
    public class AuthRepository : BaseRepository, IAuthRepository
    {
        public AuthRepository(ILogger<AuthRepository> logger, IConnectionFactory connectionFactory)
            : base(connectionFactory, logger)
        {
        }

        public async Task<ResultOptions2<UserModel>> IuUser(UserModel model)
        {
            var (user, outputParams) = await ExecuteStoredProcedureWithOutputAsync(
                StoredProcedures.spInsertUpdateUser,
                addParametersAndGetOutputs: async (command) =>
                {
                    var table = model.ToDataTable();
                    AddStructuredParameter(command, "@User", table);
                    var msg = AddOutputParameter(command, "@ov_ErrorMsg", SqlDbType.NVarChar, -1);
                    await Task.CompletedTask;
                    return new[] { msg };
                },
                mapResult: async (reader) =>
                {
                    UserModel user = new UserModel();
                    if (await reader.ReadAsync())
                    {
                        user.Id = Convert.ToInt32(reader["Id"]);
                        user.Email = reader["Email"] != DBNull.Value ? reader["Email"].ToString() : null;
                        user.Phone = reader["Phone"] != DBNull.Value ? reader["Phone"].ToString() : null;
                        user.FirstName = reader["FirstName"] != DBNull.Value ? reader["FirstName"].ToString() : null;
                        user.LastName = reader["LastName"] != DBNull.Value ? reader["LastName"].ToString() : null;
                        user.Birthday = reader["Birthday"] != DBNull.Value ? DateOnly.FromDateTime((DateTime)reader["Birthday"]) : null;
                        user.Password = reader["Password"] != DBNull.Value ? reader["Password"].ToString() : null;
                    }
                    return user;
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
                Data = user
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
                        UserModel user = new();
                        user.Id = Convert.ToInt32(reader["Id"]);
                        user.Email = reader["Email"] != DBNull.Value ? reader["Email"].ToString() : null;
                        user.Phone = reader["Phone"] != DBNull.Value ? reader["Phone"].ToString() : null;
                        user.FirstName = reader["FirstName"] != DBNull.Value ? reader["FirstName"].ToString() : null;
                        user.LastName = reader["LastName"] != DBNull.Value ? reader["LastName"].ToString() : null;
                        user.Birthday = reader["Birthday"] != DBNull.Value ? DateOnly.FromDateTime((DateTime)reader["Birthday"]) : null;
                        user.Password = reader["Password"] != DBNull.Value ? reader["Password"].ToString() : null;
                        list.Add(user);
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
    }
}
