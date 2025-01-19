using System.Data;
using TLMos.Mos;
using DbDataReaderMapper;
using TLMos.DTOs;
using System.Data.SqlClient;
using Microsoft.Extensions.Logging;
using PRDataRes.Ins;
using PRDataRes.Extensions;
using Newtonsoft.Json;

namespace PRDataRes.Res
{
    public class PrRe : IPrRe
    {
        private readonly ILogger<PrRe> _logger;
        public PrRe(ILogger<PrRe> logger)
        {
            _logger = logger;
        }
        public async Task<List<Pr>> GetPrs(string searchText)
        {
            try
            {
                List<Pr> list = new();
                using (var conn = await OpenedConnection.Create(ApplicationSettings.TimelineConnectionString))
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = StoredProcedures.spSelectPrs;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandTimeout = 1200; // 20 minutes

                    // Clear any previous parameters
                    command.Parameters.Clear();

                    if (!string.IsNullOrEmpty(searchText))
                        command.Parameters.Add(new SqlParameter("@iv_SearchText", searchText));
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var pr = reader.MapToObject<Pr>();
                            pr.TimeStart = pr.TimeStart.Date;
                            list.Add(pr);
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

        public async Task<PrsResult> IuPr(Pr pr)
        {
            try
            {
                List<Pr> prs = new List<Pr>();
                ResultOptions options;
                using (var conn = await OpenedConnection.Create(ApplicationSettings.TimelineConnectionString))
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = StoredProcedures.spInsertUpdatePr;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandTimeout = 1200; // 20 minutes

                    command.Parameters.Clear();

                    DataTable Pr = pr.ToDataTable();

                    command.Parameters.Add(new SqlParameter("@Pr", SqlDbType.Structured)
                    {
                        Value = Pr
                    });

                    SqlParameter errorMsg = new SqlParameter("@ov_ErrorMsg", SqlDbType.VarChar, -1)
                    {
                        Direction = ParameterDirection.Output
                    };
                    command.Parameters.Add(errorMsg);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var item = reader.MapToObject<Pr>();
                            prs.Add(item);
                        }
                    }

                    string msg = errorMsg.Value.ToString() ?? "";
                    

                    if (string.IsNullOrEmpty(msg))
                    {
                        options = new ResultOptions
                        {
                            Message = pr.Id == 0 ? "Successfully saved record." : "Successfully updated record",
                            Success = true,
                            Reference = prs.ToString(), 
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

                    return new PrsResult { Prs = prs, Options = options };
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
