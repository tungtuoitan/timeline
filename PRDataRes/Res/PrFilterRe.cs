using System.Data;
using TLMos.Mos;
using DbDataReaderMapper;
using TLMos.DTOs;
using System.Collections.Generic;
using System.Data.SqlClient;
using PRDataRes;
using PRDataRes.Ins;

namespace PRDataRes.Res
{
    public class PrFilterRe : IPrFilterRe


    {
        public async Task<List<int>> GetPrParentIds()
        {
            try
            {
                List<int> list = new();

                using (var conn = await OpenedConnection.Create(ApplicationSettings.TimelineConnectionString))
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = StoredProcedures.spSelectPrParentIds;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandTimeout = 1200; // 20 minutes

                    // Clear any previous parameters
                    command.Parameters.Clear();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var parentId = reader.GetInt32(reader.GetOrdinal("ParentId"));
                            list.Add(parentId);
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
