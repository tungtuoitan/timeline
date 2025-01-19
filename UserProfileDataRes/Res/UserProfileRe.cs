using System.Data;
using TLMos.Mos;
using DbDataReaderMapper;
using TLMos.DTOs;
using System.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using UserProfileDataRes.Ins;

namespace UserProfileDataRes.Res
{
    public class UserProfileRe : IUserProfileRe
    {
        private readonly ILogger<UserProfileRe> _logger;
        public UserProfileRe(ILogger<UserProfileRe> logger)
        {
            _logger = logger;
        }
        public async Task<UserProfile> GetUserProfileJson(string email, string appC)
        {
            try
            {
                UserProfile userProfile = new UserProfile();
                using (var conn = await OpenedConnection.Create(ApplicationSettings.TimelineConnectionString))
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = StoredProcedures.spSelectUserProfileJson;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandTimeout = 1200; // 20 minutes

                    // Clear any previous parameters
                    command.Parameters.Clear();
                    if (!string.IsNullOrEmpty(email))
                        command.Parameters.Add(new SqlParameter("@iv_Email", email));
                    if (!string.IsNullOrEmpty(appC))
                        command.Parameters.Add(new SqlParameter("@iv_AppC", appC));

                    var userProfileJson = new SqlParameter("@ov_Json", SqlDbType.NVarChar, -1)
                    { Direction = ParameterDirection.Output };
                    command.Parameters.Add(userProfileJson);    


                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        var jsonString = userProfileJson.Value?.ToString() ?? "";
                        if (!string.IsNullOrEmpty(jsonString))
                        {
                            var up = JsonConvert.DeserializeObject<UserProfile>(jsonString);
                            userProfile = up;
                        }
                    }
                }
                return userProfile ?? new UserProfile();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<ResultOptions> IuUserProfile(string email, string appC, string json)
        {
            try
            {
                using (var conn = await OpenedConnection.Create(ApplicationSettings.TimelineConnectionString))
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = StoredProcedures.spInsertUpdateUserProfile;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandTimeout = 1200; // 20 minutes

                    command.Parameters.Clear();

                    if (!string.IsNullOrEmpty(email))
                        command.Parameters.Add(new SqlParameter("@iv_Email", email));
                    if (!string.IsNullOrEmpty(appC))
                        command.Parameters.Add(new SqlParameter("@iv_AppC", appC));
                    if (!string.IsNullOrEmpty(appC))
                        command.Parameters.Add(new SqlParameter("@iv_Json", json));

                    SqlParameter errorMsg = new SqlParameter("@ov_ErrorMsg", SqlDbType.VarChar, -1)
                        {   Direction = ParameterDirection.Output   };
                    command.Parameters.Add(errorMsg);

                    await command.ExecuteReaderAsync();

                    if (string.IsNullOrEmpty(errorMsg.Value.ToString() ?? ""))
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
            catch (Exception ex)
            {
                // Log lỗi nếu cần thiết trước khi ném lại
                throw;
            }
        }

    }
}
