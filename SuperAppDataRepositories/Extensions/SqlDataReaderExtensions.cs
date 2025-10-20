using System.Data;
using Microsoft.Data.SqlClient;

namespace SuperAppDataRepositories.Extensions
{
    public static class SqlDataReaderExtensions
    {
        /// <summary>
        /// Safely get nullable string value from SqlDataReader
        /// </summary>
        public static string? GetNullableString(this SqlDataReader reader, string columnName)
        {
            var ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
        }

        /// <summary>
        /// Safely get nullable int32 value from SqlDataReader
        /// </summary>
        public static int? GetNullableInt32(this SqlDataReader reader, string columnName)
        {
            var ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
        }

        /// <summary>
        /// Safely get nullable DateTime value from SqlDataReader
        /// </summary>
        public static DateTime? GetNullableDateTime(this SqlDataReader reader, string columnName)
        {
            var ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
        }

        /// <summary>
        /// Safely get nullable boolean value from SqlDataReader
        /// </summary>
        public static bool? GetNullableBoolean(this SqlDataReader reader, string columnName)
        {
            var ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetBoolean(ordinal);
        }

        /// <summary>
        /// Safely get nullable decimal value from SqlDataReader
        /// </summary>
        public static decimal? GetNullableDecimal(this SqlDataReader reader, string columnName)
        {
            var ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetDecimal(ordinal);
        }
    }
}
