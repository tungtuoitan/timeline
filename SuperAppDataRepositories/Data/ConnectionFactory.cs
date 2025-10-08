using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace SuperAppDataRepositories.Data
{
    public class ConnectionFactory : IConnectionFactory
    {
        private readonly IConfiguration _configuration;

        public ConnectionFactory(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<SqlConnection> CreateSuperAppConnectionAsync()
        {
            var connString = _configuration.GetConnectionString("SuperAppConnection");
            if (string.IsNullOrEmpty(connString))
            {
                throw new InvalidOperationException("SuperAppConnection connection string is not configured.");
            }

            var connection = new SqlConnection(connString);
            await connection.OpenAsync();
            return connection;
        }

        public async Task<SqlConnection> CreateUserProfileConnectionAsync()
        {
            var connString = _configuration.GetConnectionString("UserProfileConnection");
            if (string.IsNullOrEmpty(connString))
            {
                throw new InvalidOperationException("UserProfileConnection connection string is not configured.");
            }

            var connection = new SqlConnection(connString);
            await connection.OpenAsync();
            return connection;
        }
    }
}
