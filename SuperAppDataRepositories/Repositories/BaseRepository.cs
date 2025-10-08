using System.Data;
using System.Data.SqlClient;
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

                using var reader = await command.ExecuteReaderAsync();
                return await mapResult(reader);
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
