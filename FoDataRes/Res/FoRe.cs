using System.Data;
using TLMos.Mos;
using DbDataReaderMapper;
using TLMos.DTOs;
using System.Data.SqlClient;
using Microsoft.Extensions.Logging;
using FoDataRes.Ins;
using FoDataRes.Extensions;

namespace FoDataRes.Res
{
    public class FoRe : IFoRe
    {
        private readonly ILogger<FoRe> _logger;
        public FoRe(ILogger<FoRe> logger)
        {
            _logger = logger;
        }
        public async Task<List<Fo>> GetFos()
        {
            try
            {
                List<Fo> list = new();
                using (var conn = await OpenedConnection.Create(ApplicationSettings.TimelineConnectionString))
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = StoredProcedures.spSelectFos;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandTimeout = 1200; // 20 minutes

                    // Clear any previous parameters
                    command.Parameters.Clear();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var fo = reader.MapToObject<Fo>();
                            list.Add(fo);
                        }
                    }
                }
                return list;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<FosResult> IuFo(Fo fo)
        {
            try
            {
                List<Fo> fos = new List<Fo>();
                ResultOptions options;
                using (var conn = await OpenedConnection.Create(ApplicationSettings.TimelineConnectionString))
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = StoredProcedures.spInsertUpdateFos;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandTimeout = 1200; // 20 minutes

                    command.Parameters.Clear();

                    DataTable Fo = fo.ToDataTable();

                    command.Parameters.Add(new SqlParameter("@Fos", SqlDbType.Structured){
                        Value = Fo
                    });

                    SqlParameter ov_Id = new SqlParameter("@ov_Id", SqlDbType.VarChar, -1){
                        Direction = ParameterDirection.Output
                    };
                    command.Parameters.Add(ov_Id);

                    SqlParameter errorMsg = new SqlParameter("@ov_ErrorMsg", SqlDbType.VarChar, -1) {
                        Direction = ParameterDirection.Output
                    };
                    command.Parameters.Add(errorMsg);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var item = reader.MapToObject<Fo>();
                            fos.Add(item);
                        }
                    }

                    string msg = errorMsg.Value.ToString() ?? "";
                    

                    if (string.IsNullOrEmpty(msg))
                    {
                        options = new ResultOptions
                        {
                            Message = fo.Id == 0 ? "Successfully saved record." : "Successfully updated record",
                            Success = true,
                            Reference = fos.ToString(), 
                        };
                    }
                    else
                    {
                        options = new ResultOptions
                        {
                            Message = "Error saving record",
                            Success = false,
                        };
                    }

                    return new FosResult { Fos = fos, Options = options };
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
