using System.Data;
using TLMos.Mos;
using Newtonsoft.Json;

namespace FoDataRes.Extensions
{
    public static class DataTableExtensions
    {
        public static DataTable ToDataTable(this Fo fo)
        {
            DataTable dataTable = new DataTable();
            dataTable.Columns.Add(new DataColumn { ColumnName = "Id", DataType = typeof(Int64) });
            dataTable.Columns.Add(new DataColumn { ColumnName = "Name", DataType = typeof(string) });
            dataTable.Columns.Add(new DataColumn { ColumnName = "ShortName", DataType = typeof(string), AllowDBNull = true });
            dataTable.Columns.Add(new DataColumn { ColumnName = "IconId", DataType = typeof(string), AllowDBNull = true });

            dataTable.Columns.Add(new DataColumn { ColumnName = "ParentId", DataType = typeof(int), AllowDBNull = true });
            dataTable.Columns.Add(new DataColumn { ColumnName = "ActiveC", DataType = typeof(string) });
            dataTable.Columns.Add(new DataColumn { ColumnName = "PrioriC", DataType = typeof(string) });
            dataTable.Columns.Add(new DataColumn { ColumnName = "Desc", DataType = typeof(string), AllowDBNull=true });
            dataTable.Columns.Add(new DataColumn { ColumnName = "PinIndex", DataType = typeof(string), AllowDBNull=true });
            
            
            DataRow row = dataTable.NewRow();
            row["Id"] = fo.Id;
            row["Name"] = fo.Name;
            row["ShortName"] = fo.ShortName == null ? DBNull.Value : fo.ShortName;
            row["IconId"] = fo.IconId == null ? DBNull.Value : fo.IconId;

            row["ParentId"] = fo.ParentId == null ? DBNull.Value : fo.ParentId;
            row["ActiveC"] = fo.ActiveC;
            row["PrioriC"] = fo.PrioriC;
            row["Desc"] = fo.Desc == null ? DBNull.Value : fo.Desc;
            row["PinIndex"] = fo.PinIndex == null ? DBNull.Value : fo.PinIndex;

            dataTable.Rows.Add(row);
            return dataTable;
        }
    }
}
