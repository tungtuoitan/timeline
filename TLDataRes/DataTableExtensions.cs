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
            dataTable.Columns.Add(new DataColumn { ColumnName = "LevelC", DataType = typeof(string) });
            dataTable.Columns.Add(new DataColumn { ColumnName = "TimeStart", DataType = typeof(DateTime) });
            dataTable.Columns.Add(new DataColumn { ColumnName = "TimeEnd", DataType = typeof(DateTime) });
            dataTable.Columns.Add(new DataColumn { ColumnName = "ParentId", DataType = typeof(Int64), AllowDBNull = true });
            dataTable.Columns.Add(new DataColumn { ColumnName = "ActiveC", DataType = typeof(string), AllowDBNull = true });
            dataTable.Columns.Add(new DataColumn { ColumnName = "MainC", DataType = typeof(string), AllowDBNull = true });

            DataRow row = dataTable.NewRow();
            row["Id"] = ev.Id;
            row["Name"] = ev.Name;
            row["Type"] = ev.Type;
            row["LevelC"] = ev.LevelC;
            row["TimeStart"] = ev.TimeStart;
            row["TimeEnd"] = ev.TimeEnd;
            row["ParentId"] = ev.ParentId.HasValue ? ev.ParentId : DBNull.Value;
            row["ActiveC"] = ev.ActiveC;
            row["MainC"] = ev.MainC;


            dataTable.Rows.Add(row);
            return dataTable;
        }
    }
}
