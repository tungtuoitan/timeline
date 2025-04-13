using System.Data;
using TLMos.Mos;
using Newtonsoft.Json;
using UserProfileDataRes;

namespace UserProfileDataRes
{
    public static class DataTableExtensions
    {
        public static DataTable ToDataTable(this Pr pr)
        {
            DataTable dataTable = new DataTable();
            dataTable.Columns.Add(new DataColumn { ColumnName = "Id", DataType = typeof(Int64) });
            dataTable.Columns.Add(new DataColumn { ColumnName = "Name", DataType = typeof(string) });
            dataTable.Columns.Add(new DataColumn { ColumnName = "ParentId", DataType = typeof(int), AllowDBNull = true });

            dataTable.Columns.Add(new DataColumn { ColumnName = "Types", DataType = typeof(string) });
            dataTable.Columns.Add(new DataColumn { ColumnName = "RepeatType", DataType = typeof(string) });

            dataTable.Columns.Add(new DataColumn { ColumnName = "TimeStart", DataType = typeof(DateTime) });
            dataTable.Columns.Add(new DataColumn { ColumnName = "TimeEnd", DataType = typeof(DateTime), AllowDBNull = true });

            dataTable.Columns.Add(new DataColumn { ColumnName = "ActiveC", DataType = typeof(string) });
            dataTable.Columns.Add(new DataColumn { ColumnName = "StatusC", DataType = typeof(string) });
            dataTable.Columns.Add(new DataColumn { ColumnName = "PrioriC", DataType = typeof(string) });

            dataTable.Columns.Add(new DataColumn { ColumnName = "Fink", DataType = typeof(string), AllowDBNull = true });
            dataTable.Columns.Add(new DataColumn { ColumnName = "Desc", DataType = typeof(string), AllowDBNull = true });

            dataTable.Columns.Add(new DataColumn { ColumnName = "Pesults", DataType = typeof(string) });
            dataTable.Columns.Add(new DataColumn { ColumnName = "KnowC", DataType = typeof(string) });
            dataTable.Columns.Add(new DataColumn { ColumnName = "KnowLevelC", DataType = typeof(string) });

            DataRow row = dataTable.NewRow();
            row["Id"] = pr.Id;
            row["Name"] = pr.Name;
            row["ParentId"] = pr.ParentId == null ? DBNull.Value : pr.ParentId;

            row["Types"] = pr.Types;
            row["RepeatType"] = pr.RepeatType;

            row["TimeStart"] = pr.TimeStart;
            row["TimeEnd"] = pr.TimeEnd == null ? DBNull.Value : pr.TimeEnd;

            row["ActiveC"] = pr.ActiveC;
            row["StatusC"] = pr.StatusC;
            row["PrioriC"] = pr.PrioriC;

            row["Fink"] = pr.Fink == null ? DBNull.Value : pr.Fink;
            row["Desc"] = pr.Desc == null ? DBNull.Value : pr.Desc;

            row["Pesults"] = pr.Pesults;
            row["KnowC"] = pr.KnowC;
            row["KnowLevelC"] = pr.KnowLevelC;


            dataTable.Rows.Add(row);
            return dataTable;
        }

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
