using System.Data;
using SuperAppModels.Mos;


namespace SuperAppDataRepositories.Extensions
{
    public static class DataTableExtensions
    {
        public static DataTable ToDataTable(this Note note)
        {
            DataTable dataTable = new DataTable();
            dataTable.Columns.Add(new DataColumn { ColumnName = "NoteId", DataType = typeof(int) });
            dataTable.Columns.Add(new DataColumn { ColumnName = "Name", DataType = typeof(string) });
            dataTable.Columns.Add(new DataColumn { ColumnName = "Description", DataType = typeof(string), AllowDBNull = true });
            dataTable.Columns.Add(new DataColumn { ColumnName = "Tags", DataType = typeof(string), AllowDBNull = true });
            dataTable.Columns.Add(new DataColumn { ColumnName = "Type", DataType = typeof(string), AllowDBNull = true });
            dataTable.Columns.Add(new DataColumn { ColumnName = "CreatedBy", DataType = typeof(string), AllowDBNull = true });
            dataTable.Columns.Add(new DataColumn { ColumnName = "IsArchived", DataType = typeof(bool) });

            DataRow row = dataTable.NewRow();
            row["NoteId"] = note.NoteId;
            row["Name"] = note.Name;
            row["Description"] = (object?)note.Description ?? DBNull.Value;
            row["Tags"] = (object?)note.Tags ?? DBNull.Value;
            row["Type"] = (object?)note.Type ?? DBNull.Value;
            row["CreatedBy"] = (object?)note.CreatedBy ?? DBNull.Value;
            row["IsArchived"] = note.IsArchived;

            dataTable.Rows.Add(row);
            return dataTable;
        }
    }
}
