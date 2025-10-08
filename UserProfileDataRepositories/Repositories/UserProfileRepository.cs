using System.Data;
using SuperAppModels.Mos;
using SuperAppModels.DTOs;
using System.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using UserProfileDataRepositories.Ins;
using SuperAppDataRepositories.Data;
using SuperAppDataRepositories.Repositories;

namespace UserProfileDataRepositories.Repositories
{
    public class UserProfileRepository : BaseRepository, IUserProfileRepositoy
    {
        public UserProfileRepository(ILogger<UserProfileRepository> logger, IConnectionFactory connectionFactory)
            : base(connectionFactory, logger)
        {
        }

        public async Task<UserProfile> GetUserProfileJson(string email, string appC)
        {
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
