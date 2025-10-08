using System.Data;
using SuperAppModels.Mos;
using System.Data.SqlClient;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Ins;
using SuperAppDataRepositories.Data;

namespace SuperAppDataRepositories.Repositories
{
    public class StandardRegistryRepository : BaseRepository, IStandardRegistryRepository
    {
        public StandardRegistryRepository(ILogger<StandardRegistryRepository> logger, IConnectionFactory connectionFactory)
            : base(connectionFactory, logger)
        {
        }

        public async Task<List<StandardRegistry>> GetStandardRegistries(string? type)
        {
            return await ExecuteStoredProcedureAsync(
                StoredProcedures.spSelectStandardRegistries,
                addParameters: async (command) =>
                {
                    if (type != null)
                    {
                        command.Parameters.Add(new SqlParameter("@Type", SqlDbType.Structured)
                        {
                            Value = type
                        });
                    }
                    await Task.CompletedTask;
                },
                mapResult: MapToListAsync<StandardRegistry>,
                useSuperAppConnection: true
            );
        }
    }
}
