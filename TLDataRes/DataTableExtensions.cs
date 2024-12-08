using System.Data;
using TLMos.Mos;


namespace TLDataRes.Extensions
{
    public static class DataTableExtensions
    {
        //Name, Type, Level, TimeStart, TimeEnd, ParentId
        public static DataTable ToDataTable(this Ev ev)
        {
            DataTable dataTable = new DataTable();
            dataTable.Columns.Add(new DataColumn { ColumnName = "Id", DataType = typeof(Int64) });
            dataTable.Columns.Add(new DataColumn { ColumnName = "Name", DataType = typeof(string) });
            dataTable.Columns.Add(new DataColumn { ColumnName = "Type", DataType = typeof(string), AllowDBNull = true });
            dataTable.Columns.Add(new DataColumn { ColumnName = "Level", DataType = typeof(string) });
            dataTable.Columns.Add(new DataColumn { ColumnName = "TimeStart", DataType = typeof(DateTime) });
            dataTable.Columns.Add(new DataColumn { ColumnName = "TimeEnd", DataType = typeof(DateTime) });
            dataTable.Columns.Add(new DataColumn { ColumnName = "ParentId", DataType = typeof(Int64), AllowDBNull = true });

            DataRow row = dataTable.NewRow();
            row["Id"] = ev.Id == 0 ? DBNull.Value : ev.Id;
            row["Name"] = ev.Name;
            row["Type"] = ev.Type;
            row["Level"] = ev.Level;
            row["TimeStart"] = ev.TimeStart;
            row["TimeEnd"] = ev.TimeEnd;
            row["ParentId"] = ev.ParentId.HasValue ? ev.ParentId : DBNull.Value;

            dataTable.Rows.Add(row);
            return dataTable;
        }
    }
}
