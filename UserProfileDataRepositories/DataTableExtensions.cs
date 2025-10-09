using System.Data;
using SuperAppModels.Models;
using Newtonsoft.Json;
using UserProfileDataRepositories;

namespace UserProfileDataRepositories
{
    public static class DataTableExtensions
    {
       public static DataTable ToDataTable(this UserModel user)
        {
            DataTable dataTable = new DataTable();
            dataTable.Columns.Add(new DataColumn { ColumnName = "Id", DataType = typeof(Int64) });
            dataTable.Columns.Add(new DataColumn { ColumnName = "Email", DataType = typeof(string), AllowDBNull = true });
            dataTable.Columns.Add(new DataColumn { ColumnName = "Phone", DataType = typeof(string), AllowDBNull = true });
            dataTable.Columns.Add(new DataColumn { ColumnName = "FirstName", DataType = typeof(string), AllowDBNull = true });
            dataTable.Columns.Add(new DataColumn { ColumnName = "LastName", DataType = typeof(string), AllowDBNull = true });
            dataTable.Columns.Add(new DataColumn { ColumnName = "Birthday", DataType = typeof(DateTime), AllowDBNull = true });
            dataTable.Columns.Add(new DataColumn { ColumnName = "Password", DataType = typeof(string), AllowDBNull = true });

            DataRow row = dataTable.NewRow();
            row["Id"] = user.Id;
            row["Email"] = user.Email != null ? user.Email : DBNull.Value;
            row["Phone"] = user.Phone != null ? user.Phone : DBNull.Value;
            row["FirstName"] = user.FirstName != null ? user.FirstName : DBNull.Value;
            row["LastName"] = user.LastName != null ? user.LastName : DBNull.Value;
            row["Birthday"] = user.Birthday != null ? Helpers.DateOnlyToDateTime(user.Birthday.Value) : DBNull.Value;
            row["Password"] = user.Password != null ? Helpers.HashPassword(user.Password) : DBNull.Value;

            dataTable.Rows.Add(row);
            return dataTable;
        }
    }
}
