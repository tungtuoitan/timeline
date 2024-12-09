using System.Data;
using TLMos.Mos;
using DbDataReaderMapper;
using TLDataRes.Ins;
using TLMos.DTOs;
using System.Collections.Generic;
using System.Data.SqlClient;
using TLDataRes.Extensions;

namespace TLDataRes.Res
{
    public class EvRe : IEvRe

    {
        public async Task<List<Ev>> GetEvs()
        {
            try
            {
                List<Ev> list = new();

                using (var conn = await OpenedConnection.Create(ApplicationSettings.ERPConnectionString))
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = StoredProcedures.spSelectEvs;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandTimeout = 1200; // 20 minutes

                    // Clear any previous parameters
                    command.Parameters.Clear();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var Event = reader.MapToObject<Ev>();
                            list.Add(Event);
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

        public async Task<EvsResult> IuEv(Ev ev)
        {
            try
            {
                List<Ev> evs = new List<Ev>();
                ResultOptions options;
                using (var conn = await OpenedConnection.Create(ApplicationSettings.ERPConnectionString))
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = StoredProcedures.spInsertUpdateEv;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandTimeout = 1200; // 20 minutes

                    command.Parameters.Clear();

                    DataTable Ev = ev.ToDataTable();

                    command.Parameters.Add(new SqlParameter("@Ev", SqlDbType.Structured)
                    {
                        Value = Ev
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
                            var item = reader.MapToObject<Ev>();
                            evs.Add(item);
                        }
                    }

                    string msg = errorMsg.Value.ToString() ?? "";
                    

                    if (string.IsNullOrEmpty(msg))
                    {
                        options = new ResultOptions
                        {
                            Message = ev.Id == 0 ? "Successfully saved record." : "Successfully updated record",
                            Success = true,
                            Reference = evs.ToString(), 
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

                    return new EvsResult { Evs = evs, Options = options };
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
