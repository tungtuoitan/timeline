using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserProfileDataRepositories
{
    public static class OpenedConnection
    {
        public static async Task<SqlConnection> Create(string connectionString)
        {
            var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();
            return conn;
        }


        public static SqlCommand CreateCommand(this SqlConnection conn, string commandText)
            => new SqlCommand(commandText, conn);
    }
}
