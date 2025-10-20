using System.Data;
using Microsoft.Data.SqlClient;
using DbDataReaderMapper;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Data;

namespace SuperAppDataRepositories.Repositories
{
    public abstract class BaseRepository
    {
        protected readonly IConnectionFactory _connectionFactory;
        protected readonly ILogger _logger;
        private const int DefaultCommandTimeout = 1200; // 20 minutes

        protected BaseRepository(IConnectionFactory connectionFactory, ILogger logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        /// <summary>
        /// Executes a stored procedure and returns a single result mapped by the provided function
        /// </summary>
        protected async Task<T> ExecuteStoredProcedureAsync<T>(
            string storedProcedure,
            Func<SqlCommand, Task> addParameters,
            Func<SqlDataReader, Task<T>> mapResult,
            bool useSuperAppConnection = true)
        {
            try
            {
                using var conn = useSuperAppConnection
                    ? await _connectionFactory.CreateSuperAppConnectionAsync()
                    : await _connectionFactory.CreateUserProfileConnectionAsync();

                using var command = conn.CreateCommand();
                command.CommandText = storedProcedure;
                command.CommandType = CommandType.StoredProcedure;
                command.CommandTimeout = DefaultCommandTimeout;

                await addParameters(command);

                // Debug logging for parameters
                _logger.LogInformation("Executing stored procedure {StoredProcedure} with parameters: {Parameters}",
                    storedProcedure,
                    string.Join(", ", command.Parameters.Cast<SqlParameter>().Select(p => $"{p.ParameterName}={p.Value}")));

                using var reader = await command.ExecuteReaderAsync();
                var result = await mapResult(reader);

                // Debug logging for result
                if (result is System.Collections.ICollection collection)
                {
                    _logger.LogInformation("Stored procedure {StoredProcedure} returned {ResultCount} items", 
                        storedProcedure, collection.Count);
                }

                return result;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Database error executing stored procedure {StoredProcedure}", storedProcedure);
                throw;
            }
        }

        /// <summary>
        /// Executes a stored procedure with output parameters
        /// </summary>
        protected async Task<(T Result, SqlParameter[] OutputParams)> ExecuteStoredProcedureWithOutputAsync<T>(
            string storedProcedure,
            Func<SqlCommand, Task<SqlParameter[]>> addParametersAndGetOutputs,
            Func<SqlDataReader, Task<T>> mapResult,
            bool useSuperAppConnection = true)
        {
            try
            {
                using var conn = useSuperAppConnection
                    ? await _connectionFactory.CreateSuperAppConnectionAsync()
                    : await _connectionFactory.CreateUserProfileConnectionAsync();

                using var command = conn.CreateCommand();
                command.CommandText = storedProcedure;
                command.CommandType = CommandType.StoredProcedure;
                command.CommandTimeout = DefaultCommandTimeout;

                var outputParams = await addParametersAndGetOutputs(command);

                using var reader = await command.ExecuteReaderAsync();
                var result = await mapResult(reader);

                return (result, outputParams);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Database error executing stored procedure {StoredProcedure}", storedProcedure);
                throw;
            }
        }

        /// <summary>
        /// Adds a parameter to the command, handling null values by converting to DBNull.Value
        /// </summary>
        protected void AddParameterIfNotNull(SqlCommand command, string paramName, object? value)
        {
            command.Parameters.Add(new SqlParameter(paramName, value ?? DBNull.Value));
        }

        /// <summary>
        /// Adds a structured (table-valued) parameter to the command
        /// </summary>
        protected void AddStructuredParameter(SqlCommand command, string paramName, DataTable dataTable)
        {
            command.Parameters.Add(new SqlParameter(paramName, SqlDbType.Structured)
            {
                Value = dataTable
            });
        }

        /// <summary>
        /// Adds an output parameter to the command
        /// </summary>
        protected SqlParameter AddOutputParameter(SqlCommand command, string paramName, SqlDbType sqlDbType, int size = -1)
        {
            var param = new SqlParameter(paramName, sqlDbType, size)
            {
                Direction = ParameterDirection.Output
            };
            command.Parameters.Add(param);
            return param;
        }

        /// <summary>
        /// Maps a SqlDataReader to a list of objects using DbDataReaderMapper
        /// </summary>
        protected async Task<List<T>> MapToListAsync<T>(SqlDataReader reader) where T : class, new()
        {
            var list = new List<T>();
            while (await reader.ReadAsync())
            {
                list.Add(reader.MapToObject<T>());
            }
            return list;
        }

        /// <summary>
        /// Maps a SqlDataReader to a list of Tag objects with custom column mapping for lowercase database columns
        /// </summary>
        protected async Task<List<SuperAppModels.Models.Tag>> MapToTagListAsync(SqlDataReader reader)
        {
            var list = new List<SuperAppModels.Models.Tag>();
            while (await reader.ReadAsync())
            {
                var tag = MapTagFromReader(reader);
                list.Add(tag);
            }
            return list;
        }

        /// <summary>
        /// Maps a SqlDataReader to a single Tag object with custom column mapping for lowercase database columns
        /// </summary>
        protected async Task<SuperAppModels.Models.Tag?> MapToTagSingleAsync(SqlDataReader reader)
        {
            if (await reader.ReadAsync())
            {
                return MapTagFromReader(reader);
            }
            return null;
        }

        /// <summary>
        /// Helper method to map a single Tag from SqlDataReader, handling both lowercase and PascalCase column names
        /// </summary>
        private SuperAppModels.Models.Tag MapTagFromReader(SqlDataReader reader)
        {
            var tag = new SuperAppModels.Models.Tag();
            
            // Map database columns to properties, try lowercase first (from usp_s_tags), then PascalCase (from other procedures)
            tag.TagId = TryGetInt32(reader, "id") ?? TryGetInt32(reader, "Id") ?? 0;
            tag.UserId = TryGetInt32(reader, "user_id") ?? TryGetInt32(reader, "UserId") ?? 0;
            tag.Name = TryGetString(reader, "name") ?? TryGetString(reader, "Name") ?? string.Empty;
            // tag.ParentId = TryGetNullableInt32(reader, "parent_id") ?? TryGetNullableInt32(reader, "ParentId"); // Not in model
            // tag.Path = TryGetString(reader, "path") ?? TryGetString(reader, "Path"); // Not in model
            tag.Slug = TryGetString(reader, "slug") ?? TryGetString(reader, "Slug") ?? string.Empty;
            tag.Color = TryGetString(reader, "color") ?? TryGetString(reader, "Color");
            tag.Icon = TryGetString(reader, "icon") ?? TryGetString(reader, "Icon");
            tag.Description = TryGetString(reader, "description") ?? TryGetString(reader, "Description");
            // tag.IsPublic = TryGetNullableBoolean(reader, "is_public") ?? TryGetNullableBoolean(reader, "IsPublic"); // Not in model
            // tag.PublicSlug = TryGetString(reader, "public_slug") ?? TryGetString(reader, "PublicSlug"); // Not in model
            tag.CreatedAt = TryGetNullableDateTime(reader, "created_at") ?? TryGetNullableDateTime(reader, "CreatedAt");
            tag.UpdatedAt = TryGetNullableDateTime(reader, "updated_at") ?? TryGetNullableDateTime(reader, "UpdatedAt");
            tag.DeletedAt = TryGetNullableDateTime(reader, "deleted_at") ?? TryGetNullableDateTime(reader, "DeletedAt");
            // tag.CreatedBy = TryGetNullableInt32(reader, "created_by") ?? TryGetNullableInt32(reader, "CreatedBy"); // Not in model
            // tag.Depth = TryGetNullableInt32(reader, "depth") ?? TryGetNullableInt32(reader, "Depth"); // Not in model
            
            return tag;
        }

        /// <summary>
        /// Helper methods for safe column reading
        /// </summary>
        private int? TryGetInt32(SqlDataReader reader, string columnName)
        {
            try
            {
                var ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
            }
            catch
            {
                return null;
            }
        }

        private int? TryGetNullableInt32(SqlDataReader reader, string columnName)
        {
            try
            {
                var ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
            }
            catch
            {
                return null;
            }
        }

        private string? TryGetString(SqlDataReader reader, string columnName)
        {
            try
            {
                var ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
            }
            catch
            {
                return null;
            }
        }

        private DateTime? TryGetNullableDateTime(SqlDataReader reader, string columnName)
        {
            try
            {
                var ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
            }
            catch
            {
                return null;
            }
        }

        private bool? TryGetNullableBoolean(SqlDataReader reader, string columnName)
        {
            try
            {
                var ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? null : reader.GetBoolean(ordinal);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Maps a SqlDataReader to a list of TagTree objects with custom column mapping
        /// </summary>
        protected async Task<List<SuperAppModels.Models.TagTree>> MapToTagTreeListAsync(SqlDataReader reader)
        {
            var list = new List<SuperAppModels.Models.TagTree>();
            while (await reader.ReadAsync())
            {
                var tagTree = MapTagTreeFromReader(reader);
                list.Add(tagTree);
            }
            return list;
        }

        /// <summary>
        /// Helper method to map a single TagTree from SqlDataReader
        /// </summary>
        private SuperAppModels.Models.TagTree MapTagTreeFromReader(SqlDataReader reader)
        {
            var tagTree = new SuperAppModels.Models.TagTree();
            
            // Map database columns to properties, handling both lowercase and PascalCase
            tagTree.Id = TryGetInt32(reader, "id") ?? TryGetInt32(reader, "Id") ?? 0;
            tagTree.UserId = TryGetInt32(reader, "user_id") ?? TryGetInt32(reader, "UserId") ?? 0;
            tagTree.Name = TryGetString(reader, "name") ?? TryGetString(reader, "Name") ?? string.Empty;
            tagTree.ParentId = TryGetNullableInt32(reader, "parent_id") ?? TryGetNullableInt32(reader, "ParentId");
            tagTree.Path = TryGetString(reader, "path") ?? TryGetString(reader, "Path");
            tagTree.Slug = TryGetString(reader, "slug") ?? TryGetString(reader, "Slug");
            tagTree.Color = TryGetString(reader, "color") ?? TryGetString(reader, "Color");
            tagTree.Icon = TryGetString(reader, "icon") ?? TryGetString(reader, "Icon");
            tagTree.AccessType = TryGetString(reader, "access_type") ?? TryGetString(reader, "AccessType") ?? string.Empty;
            tagTree.Level = TryGetInt32(reader, "level") ?? TryGetInt32(reader, "Level") ?? 0;
            tagTree.UsageCount = TryGetInt32(reader, "usage_count") ?? TryGetInt32(reader, "UsageCount") ?? 0;
            tagTree.ChildrenCount = TryGetInt32(reader, "children_count") ?? TryGetInt32(reader, "ChildrenCount") ?? 0;
            
            return tagTree;
        }

        /// <summary>
        /// Maps a SqlDataReader to a single object using DbDataReaderMapper
        /// </summary>
        protected async Task<T?> MapToSingleAsync<T>(SqlDataReader reader) where T : class, new()
        {
            if (await reader.ReadAsync())
            {
                return reader.MapToObject<T>();
            }
            return null;
        }

        /// <summary>
        /// Checks if an output parameter contains an error message
        /// </summary>
        protected bool HasError(SqlParameter errorParam, out string errorMessage)
        {
            errorMessage = errorParam.Value?.ToString() ?? string.Empty;
            return !string.IsNullOrEmpty(errorMessage);
        }
    }
}
