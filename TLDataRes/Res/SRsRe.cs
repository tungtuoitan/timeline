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
    public class SRsRe : ISRsRe


    {
        public async Task<List<SR>> GetSRs(string? type)
        {
            try
            {
                List<SR> list = new();

                using (var conn = await OpenedConnection.Create(ApplicationSettings.TimelineConnectionString))
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = StoredProcedures.spSelectSRs;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandTimeout = 1200; // 20 minutes

                    // Clear any previous parameters
                    command.Parameters.Clear();

                    if(type != null)
                    {
                        command.Parameters.Add(new SqlParameter("@Type", SqlDbType.Structured)
                        {
                            Value = type
                        });

                    }

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var SR = reader.MapToObject<SR>();
                            list.Add(SR);
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
    }
}
