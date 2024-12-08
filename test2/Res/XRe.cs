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
    public class XRe : IXRe

    {
        public async Task<List<Event>> GetEvents()
        {
            try
            {
                List<Event> list = new();

                using (var conn = await OpenedConnection.Create(ApplicationSettings.ERPConnectionString))
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = StoredProcedures.spSelectEvent;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandTimeout = 1200; // 20 minutes

                    // Clear any previous parameters
                    command.Parameters.Clear();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var Event = reader.MapToObject<Event>();
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

        public async Task<ResultOptions> IuEv(Event ev)
        {
            try
            {
                using (var conn = await OpenedConnection.Create(ApplicationSettings.ERPConnectionString))
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = StoredProcedures.spInsertUpdateEvent;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandTimeout = 1200; // 20 minutes

                    command.Parameters.Clear();

                    DataTable Ev = ev.ToDataTable();

                    command.Parameters.Add(new SqlParameter("@Ev", SqlDbType.Structured)
                    {
                        Value = Ev
                    });

                    SqlParameter insertedIdParam = new SqlParameter("@ov_EvId", SqlDbType.BigInt)
                    {
                        Direction = ParameterDirection.Output,
                    };
                    command.Parameters.Add(insertedIdParam);

                    SqlParameter errorMsg = new SqlParameter("@ov_ErrorMsg", SqlDbType.VarChar, -1)
                    {
                        Direction = ParameterDirection.Output
                    };
                    command.Parameters.Add(errorMsg);

                    var retValue = await command.ExecuteNonQueryAsync();

                    string msg = errorMsg.Value.ToString() ?? "";
                    if (string.IsNullOrEmpty(msg))
                    {

                        return new ResultOptions
                        {
                            Message = "Successfully saved record.",
                            Success = true,
                            Reference = insertedIdParam.Value.ToString(),
                        };
                    }
                    else
                    {
                        return new ResultOptions
                        {
                            Message = "Error saving record",
                            Success = false,
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                throw; // You might want to log the exception before rethrowing
            }
        }
    }
}
