using System.Data;
using SuperAppModels.Mos;
using DbDataReaderMapper;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs;
using System.Collections.Generic;
using System.Data.SqlClient;
using SuperAppDataRepositories.Extensions;

namespace SuperAppDataRepositories.Repositories
{
    public class StandardRegistryRepository : IStandardRegistryRepository


    {
        public async Task<List<StandardRegistry>> GetStandardRegistries(string? type)
        {
            try
            {
                List<StandardRegistry> list = new();

                using (var conn = await OpenedConnection.Create(ApplicationSettings.SuperAppConnectionString))
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = StoredProcedures.spSelectStandardRegistries;
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
                            var standardRegistry = reader.MapToObject<StandardRegistry>();
                            list.Add(standardRegistry);
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
