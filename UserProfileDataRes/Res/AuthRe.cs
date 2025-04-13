using System.Data;
using TLMos.Mos;
using DbDataReaderMapper;
using TLMos.DTOs;
using System.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using UserProfileDataRes.Ins;
using UserProfileDataRes;
using PLMModels.DTOs;
using System.Numerics;

namespace UserProfileDataRes.Res
{
    public class AuthRe : IAuthRe
    {
        private readonly ILogger<UserProfileRe> _logger;
        public AuthRe(ILogger<UserProfileRe> logger)
        {
            _logger = logger;
        }
        public async Task<ResultOptions2<UserModel>> IuUser(UserModel model)
        {
            try
            {
                UserModel user = new UserModel();
                using (var conn = await OpenedConnection.Create(ApplicationSettings.TimelineConnectionString))
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = StoredProcedures.spInsertUpdateUser;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandTimeout = 1200; // 20 minutes

                    // Clear any previous parameters
                    command.Parameters.Clear();

                    var table = model.ToDataTable();
                    command.Parameters.Add(new SqlParameter("@User", SqlDbType.Structured){
                        Value = table
                    });
                    var msg = new SqlParameter("@ov_ErrorMsg", SqlDbType.NVarChar, -1)
                        { Direction = ParameterDirection.Output };
                    command.Parameters.Add(msg);


                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if(string.IsNullOrEmpty(msg.Value?.ToString() ?? ""))
                        {
                            while (await reader.ReadAsync())
                            {
                                user.Id = Convert.ToInt32(reader["Id"]);
                                user.Email = reader["Email"] != DBNull.Value ? reader["Email"].ToString() : null;
                                user.Phone = reader["Phone"] != DBNull.Value ? reader["Phone"].ToString() : null;
                                user.FirstName = reader["FirstName"] != DBNull.Value ? reader["FirstName"].ToString() : null;
                                user.LastName = reader["LastName"] != DBNull.Value ? reader["LastName"].ToString() : null;
                                user.Birthday = reader["Birthday"] != DBNull.Value ? DateOnly.FromDateTime((DateTime)reader["Birthday"]) : null;
                                user.Password = reader["Password"] != DBNull.Value ? reader["Password"].ToString() : null;
                            }
                        }
                        else
                        {
                            throw new Exception(msg.Value?.ToString() ?? "");
                        }
                    }
                }
                return new ResultOptions2<UserModel>
                {
                    Message = "Sign up successfully",
                    Success = true,
                    Data = user
                };
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
        public async Task<ResultOptions2<List<UserModel>>> GetUsers()
        {
            try
            {
                List<UserModel> list = new();
                using (var conn = await OpenedConnection.Create(ApplicationSettings.TimelineConnectionString))
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = StoredProcedures.spSelectUsers;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandTimeout = 1200; // 20 minutes

                    // Clear any previous parameters
                    command.Parameters.Clear();

                    //if (!string.IsNullOrEmpty(searchText))
                    //    command.Parameters.Add(new SqlParameter("@iv_SearchText", searchText));
                    using (var reader = await command.ExecuteReaderAsync())
                    {
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
                    }
                    return new ResultOptions2<List<UserModel>>
                    {
                        Success = true,
                        Data = list
                    };
                }
                
            }
            catch (Exception)
            {
                throw;
            }
        }

    }
}
