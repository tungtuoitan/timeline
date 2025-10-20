using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SuperAppModels.Models;
using Microsoft.Data.SqlClient;

namespace SuperAppDataRepositories.Data
{
    public class ConnectionFactory : IConnectionFactory
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ConnectionFactory> _logger;

        public ConnectionFactory(IConfiguration configuration, ILogger<ConnectionFactory> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<SqlConnection> CreateSuperAppConnectionAsync()
        {
            var connString = _configuration.GetConnectionString("SuperAppConnection");
            if (string.IsNullOrEmpty(connString))
            {
                throw new InvalidOperationException("SuperAppConnection connection string is not configured.");
            }

            _logger.LogInformation("Attempting to create SuperApp database connection to {Server}/{Database}", 
                GetServerFromConnectionString(connString), GetDatabaseFromConnectionString(connString));
            
            var connection = new SqlConnection(connString);
            
            try
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)); // 30 second timeout
                
                _logger.LogInformation("Opening connection...");
                await connection.OpenAsync(cts.Token);
                stopwatch.Stop();
                
                _logger.LogInformation("SuperApp database connection opened successfully in {ElapsedMs}ms. State: {State}", 
                    stopwatch.ElapsedMilliseconds, connection.State);
                return connection;
            }
            catch (OperationCanceledException)
            {
                _logger.LogError("Connection timeout after 30 seconds when connecting to SuperApp database");
                connection?.Dispose();
                throw new InvalidOperationException("Database connection timeout. Please check if SQL Server is accessible.");
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "SQL error opening SuperApp database connection. Error: {ErrorNumber} - {Message}", 
                    sqlEx.Number, sqlEx.Message);
                connection?.Dispose();
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error opening SuperApp database connection to {Server}/{Database}", 
                    GetServerFromConnectionString(connString), GetDatabaseFromConnectionString(connString));
                connection?.Dispose();
                throw;
            }
        }

        public async Task<SqlConnection> CreateUserProfileConnectionAsync()
        {
            var connString = _configuration.GetConnectionString("UserProfileConnection");
            if (string.IsNullOrEmpty(connString))
            {
                throw new InvalidOperationException("UserProfileConnection connection string is not configured.");
            }

            _logger.LogInformation("Attempting to create UserProfile database connection to {Server}/{Database}", 
                GetServerFromConnectionString(connString), GetDatabaseFromConnectionString(connString));
            
            var connection = new SqlConnection(connString);
            
            try
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)); // 30 second timeout
                
                _logger.LogInformation("Opening connection...");
                await connection.OpenAsync(cts.Token);
                stopwatch.Stop();
                
                _logger.LogInformation("UserProfile database connection opened successfully in {ElapsedMs}ms. State: {State}", 
                    stopwatch.ElapsedMilliseconds, connection.State);
                return connection;
            }
            catch (OperationCanceledException)
            {
                _logger.LogError("Connection timeout after 30 seconds when connecting to UserProfile database");
                connection?.Dispose();
                throw new InvalidOperationException("Database connection timeout. Please check if SQL Server is accessible.");
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "SQL error opening UserProfile database connection. Error: {ErrorNumber} - {Message}", 
                    sqlEx.Number, sqlEx.Message);
                connection?.Dispose();
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error opening UserProfile database connection to {Server}/{Database}", 
                    GetServerFromConnectionString(connString), GetDatabaseFromConnectionString(connString));
                connection?.Dispose();
                throw;
            }
        }

        private static string GetServerFromConnectionString(string connectionString)
        {
            try
            {
                var builder = new SqlConnectionStringBuilder(connectionString);
                return builder.DataSource ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        private static string GetDatabaseFromConnectionString(string connectionString)
        {
            try
            {
                var builder = new SqlConnectionStringBuilder(connectionString);
                return builder.InitialCatalog ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }
    }
}
