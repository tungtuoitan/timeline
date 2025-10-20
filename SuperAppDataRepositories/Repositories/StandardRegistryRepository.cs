using System.Data;
using SuperAppModels.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Ins;
using SuperAppDataRepositories.Data;
using SuperAppDataRepositories.Extensions;

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
            try
            {
                _logger.LogInformation("Getting standard registries with type filter: {Type}", type ?? "all");

                return await ExecuteStoredProcedureAsync(
                    StoredProcedures.spSelectStandardRegistries,
                    addParameters: async (command) =>
                    {
                        if (!string.IsNullOrEmpty(type))
                        {
                            command.Parameters.Add(new SqlParameter("@Type", type));
                        }
                        await Task.CompletedTask;
                    },
                    mapResult: MapToListAsync<StandardRegistry>,
                    useSuperAppConnection: true
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting standard registries with type: {Type}", type);
                throw;
            }
        }

        public async Task<List<StandardRegistry>> GetAllStandardRegistry()
        {
            try
            {
                _logger.LogInformation("Getting all standard registry entries");

                return await ExecuteStoredProcedureAsync(
                    StoredProcedures.spSelectStandardRegistries,
                    addParameters: async (command) =>
                    {
                        // No filter parameter for getting all entries
                        await Task.CompletedTask;
                    },
                    mapResult: MapToListAsync<StandardRegistry>,
                    useSuperAppConnection: true
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting all standard registry entries");
                throw;
            }
        }

        public async Task<StandardRegistry?> GetStandardRegistryById(int id)
        {
            try
            {
                _logger.LogInformation("Getting standard registry by ID: {Id}", id);

                if (id <= 0)
                {
                    throw new ArgumentException("ID must be greater than 0", nameof(id));
                }

                return await ExecuteStoredProcedureAsync(
                    StoredProcedures.spSelectStandardRegistryById,
                    addParameters: async (command) =>
                    {
                        command.Parameters.Add(new SqlParameter("@iv_Id", id));
                        await Task.CompletedTask;
                    },
                    mapResult: MapToSingleAsync<StandardRegistry>,
                    useSuperAppConnection: true
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting standard registry by ID: {Id}", id);
                throw;
            }
        }

        public async Task<List<StandardRegistry>> GetStandardRegistryByType(string type)
        {
            try
            {
                _logger.LogInformation("Getting standard registries by type: {Type}", type);

                if (string.IsNullOrEmpty(type))
                {
                    throw new ArgumentException("Type cannot be null or empty", nameof(type));
                }

                return await ExecuteStoredProcedureAsync(
                    StoredProcedures.spSelectStandardRegistries,
                    addParameters: async (command) =>
                    {
                        command.Parameters.Add(new SqlParameter("@Type", type));
                        await Task.CompletedTask;
                    },
                    mapResult: MapToListAsync<StandardRegistry>,
                    useSuperAppConnection: true
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting standard registries by type: {Type}", type);
                throw;
            }
        }

        public async Task<StandardRegistry?> GetStandardRegistryByKey(string key)
        {
            try
            {
                _logger.LogInformation("Getting standard registry by key: {Key}", key);

                if (string.IsNullOrEmpty(key))
                {
                    throw new ArgumentException("Key cannot be null or empty", nameof(key));
                }

                return await ExecuteStoredProcedureAsync(
                    StoredProcedures.spSelectStandardRegistryByKey,
                    addParameters: async (command) =>
                    {
                        command.Parameters.Add(new SqlParameter("@iv_Code", key)); // Changed from @iv_Key to @iv_Code
                        await Task.CompletedTask;
                    },
                    mapResult: MapToSingleAsync<StandardRegistry>,
                    useSuperAppConnection: true
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting standard registry by key: {Key}", key);
                throw;
            }
        }

        public async Task<StandardRegistry> CreateStandardRegistryAsync(StandardRegistry standardRegistry)
        {
            try
            {
                _logger.LogInformation("Creating new standard registry entry with code: {Code}", standardRegistry.Code);

                if (string.IsNullOrEmpty(standardRegistry.Code))
                {
                    throw new ArgumentException("StandardRegistry code cannot be null or empty", nameof(standardRegistry));
                }

                (List<StandardRegistry> entries, SqlParameter[] outputParams) = await ExecuteStoredProcedureWithOutputAsync(
                    StoredProcedures.spInsertUpdateStandardRegistry,
                    addParametersAndGetOutputs: async (command) =>
                    {
                        // Set ID to 0 for new entries
                        standardRegistry.Id = 0;
                        DataTable registryTable = standardRegistry.ToDataTable();
                        AddStructuredParameter(command, "@StandardRegistry", registryTable);
                        var errorMsg = AddOutputParameter(command, "@ov_ErrorMsg", SqlDbType.VarChar, -1);
                        await Task.CompletedTask;
                        return new[] { errorMsg };
                    },
                    mapResult: MapToListAsync<StandardRegistry>,
                    useSuperAppConnection: true
                );

                var errorParam = outputParams[0];
                if (HasError(errorParam, out string errorMessage))
                {
                    _logger.LogError("Error creating standard registry: {ErrorMessage}", errorMessage);
                    throw new InvalidOperationException($"Failed to create standard registry: {errorMessage}");
                }

                var createdEntry = entries.FirstOrDefault();
                if (createdEntry == null)
                {
                    throw new InvalidOperationException("Failed to create standard registry: No entry returned from database");
                }

                _logger.LogInformation("Successfully created standard registry with ID: {Id}", createdEntry.Id);
                return createdEntry;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating standard registry");
                throw;
            }
        }

        public async Task<StandardRegistry> UpdateStandardRegistryAsync(StandardRegistry standardRegistry)
        {
            try
            {
                _logger.LogInformation("Updating standard registry with ID: {Id}", standardRegistry.Id);

                if (standardRegistry.Id <= 0)
                {
                    throw new ArgumentException("StandardRegistry ID must be greater than 0 for updates", nameof(standardRegistry));
                }

                if (string.IsNullOrEmpty(standardRegistry.Code)) // Changed from Key to Code
                {
                    throw new ArgumentException("StandardRegistry code cannot be null or empty", nameof(standardRegistry));
                }

                (List<StandardRegistry> entries, SqlParameter[] outputParams) = await ExecuteStoredProcedureWithOutputAsync(
                    StoredProcedures.spInsertUpdateStandardRegistry,
                    addParametersAndGetOutputs: async (command) =>
                    {
                        DataTable registryTable = standardRegistry.ToDataTable();
                        AddStructuredParameter(command, "@StandardRegistry", registryTable);
                        var errorMsg = AddOutputParameter(command, "@ov_ErrorMsg", SqlDbType.VarChar, -1);
                        await Task.CompletedTask;
                        return new[] { errorMsg };
                    },
                    mapResult: MapToListAsync<StandardRegistry>,
                    useSuperAppConnection: true
                );

                var errorParam = outputParams[0];
                if (HasError(errorParam, out string errorMessage))
                {
                    _logger.LogError("Error updating standard registry: {ErrorMessage}", errorMessage);
                    throw new InvalidOperationException($"Failed to update standard registry: {errorMessage}");
                }

                var updatedEntry = entries.FirstOrDefault();
                if (updatedEntry == null)
                {
                    throw new InvalidOperationException("Failed to update standard registry: No entry returned from database");
                }

                _logger.LogInformation("Successfully updated standard registry with ID: {Id}", updatedEntry.Id);
                return updatedEntry;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating standard registry with ID: {Id}", standardRegistry.Id);
                throw;
            }
        }

        public async Task<bool> DeleteStandardRegistryAsync(int id)
        {
            try
            {
                _logger.LogInformation("Deleting standard registry with ID: {Id}", id);

                if (id <= 0)
                {
                    throw new ArgumentException("StandardRegistry ID must be greater than 0", nameof(id));
                }

                var (_, outputParams) = await ExecuteStoredProcedureWithOutputAsync<object?>(
                    StoredProcedures.spDeleteStandardRegistry,
                    addParametersAndGetOutputs: async (command) =>
                    {
                        command.Parameters.Add(new SqlParameter("@iv_Id", id));
                        var errorMsg = AddOutputParameter(command, "@ov_ErrorMsg", SqlDbType.VarChar, -1);
                        await Task.CompletedTask;
                        return new[] { errorMsg };
                    },
                    mapResult: async (reader) =>
                    {
                        await Task.CompletedTask;
                        return (object?)null;
                    },
                    useSuperAppConnection: true
                );

                var errorParam = outputParams[0];
                if (HasError(errorParam, out string errorMessage))
                {
                    _logger.LogError("Error deleting standard registry: {ErrorMessage}", errorMessage);
                    throw new InvalidOperationException($"Failed to delete standard registry: {errorMessage}");
                }

                _logger.LogInformation("Successfully deleted standard registry with ID: {Id}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting standard registry with ID: {Id}", id);
                throw;
            }
        }
    }
}
