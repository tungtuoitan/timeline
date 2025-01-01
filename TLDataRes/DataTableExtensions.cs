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
            dataTable.Columns.Add(new DataColumn { ColumnName = "Type", DataType = typeof(string) });
            dataTable.Columns.Add(new DataColumn { ColumnName = "LevelC", DataType = typeof(string) });

            dataTable.Columns.Add(new DataColumn { ColumnName = "TimeStart", DataType = typeof(DateTime) });
            dataTable.Columns.Add(new DataColumn { ColumnName = "TimeEnd", DataType = typeof(DateTime) });
            dataTable.Columns.Add(new DataColumn { ColumnName = "ParentId", DataType = typeof(Int64), AllowDBNull = true });

            dataTable.Columns.Add(new DataColumn { ColumnName = "ActiveC", DataType = typeof(string)  });
            dataTable.Columns.Add(new DataColumn { ColumnName = "StatusC", DataType = typeof(string) });
            dataTable.Columns.Add(new DataColumn { ColumnName = "PrioriC", DataType = typeof(string) });
            dataTable.Columns.Add(new DataColumn { ColumnName = "Fink", DataType = typeof(string) });
            dataTable.Columns.Add(new DataColumn { ColumnName = "Desc", DataType = typeof(string) });
            dataTable.Columns.Add(new DataColumn { ColumnName = "SubType", DataType = typeof(string) });
            dataTable.Columns.Add(new DataColumn { ColumnName = "EvelC", DataType = typeof(string) });

            DataRow row = dataTable.NewRow();
            row["Id"] = ev.Id;
            row["Name"] = ev.Name;
            row["Type"] = ev.Type;
            row["LevelC"] = ev.LevelC;

            row["TimeStart"] = ev.TimeStart;
            row["TimeEnd"] = ev.TimeEnd;
            row["ParentId"] = ev.ParentId.HasValue ? ev.ParentId : DBNull.Value;

            row["ActiveC"] = ev.ActiveC;
            row["StatusC"] = ev.StatusC;
            row["PrioriC"] = ev.PrioriC;
            row["Fink"] = ev.Fink != null ? ev.Fink : DBNull.Value;
            row["Desc"] = ev.Desc == null ? "" : ev.Desc;
            row["SubType"] = ev.SubType;
            row["EvelC"] = ev.EvelC;


            dataTable.Rows.Add(row);
            return dataTable;
        }
    }
}
