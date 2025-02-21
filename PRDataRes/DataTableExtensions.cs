using System.Data;
using TLMos.Mos;
using Newtonsoft.Json;

namespace PRDataRes.Extensions
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
            dataTable.Columns.Add(new DataColumn { ColumnName = "TimeEnd", DataType = typeof(DateTime), AllowDBNull=true });

            dataTable.Columns.Add(new DataColumn { ColumnName = "ActiveC", DataType = typeof(string) });
            dataTable.Columns.Add(new DataColumn { ColumnName = "StatusC", DataType = typeof(string) });
            dataTable.Columns.Add(new DataColumn { ColumnName = "PrioriC", DataType = typeof(string) });

            dataTable.Columns.Add(new DataColumn { ColumnName = "Fink", DataType = typeof(string), AllowDBNull=true });
            dataTable.Columns.Add(new DataColumn { ColumnName = "Desc", DataType = typeof(string), AllowDBNull=true });

            dataTable.Columns.Add(new DataColumn { ColumnName = "Pesults", DataType = typeof(string) });
            dataTable.Columns.Add(new DataColumn { ColumnName = "KnowC", DataType = typeof(string) });

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


            dataTable.Rows.Add(row);
            return dataTable;
        }
    }
}
