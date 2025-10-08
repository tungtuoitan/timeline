using System.Data.SqlClient;

namespace SuperAppDataRepositories.Data
{
    public interface IConnectionFactory
    {
        Task<SqlConnection> CreateSuperAppConnectionAsync();
        Task<SqlConnection> CreateUserProfileConnectionAsync();
    }
}
