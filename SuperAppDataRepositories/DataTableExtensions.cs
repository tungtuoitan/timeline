using System.Data;
using SuperAppModels.Models; 


namespace SuperAppDataRepositories.Extensions
{
    public static class DataTableExtensions
    {
        public static DataTable ToDataTable(this Note note)
        {
            DataTable dataTable = new DataTable();
            dataTable.Columns.Add(new DataColumn { ColumnName = "Id", DataType = typeof(int) });
            dataTable.Columns.Add(new DataColumn { ColumnName = "Name", DataType = typeof(string) });
            dataTable.Columns.Add(new DataColumn { ColumnName = "Description", DataType = typeof(string), AllowDBNull = true });
            dataTable.Columns.Add(new DataColumn { ColumnName = "Type", DataType = typeof(string), AllowDBNull = true });
            dataTable.Columns.Add(new DataColumn { ColumnName = "CreatedBy", DataType = typeof(int), AllowDBNull = true });
            dataTable.Columns.Add(new DataColumn { ColumnName = "IsArchived", DataType = typeof(bool) });

            DataRow row = dataTable.NewRow();
            row["Id"] = note.NoteId;
            row["Name"] = note.Name;
            row["Description"] = (object?)note.Description ?? DBNull.Value;
            row["Type"] = (object?)note.Type ?? DBNull.Value;
            row["CreatedBy"] = (object?)note.CreatedBy ?? DBNull.Value;
            row["IsArchived"] = note.IsArchived;

            dataTable.Rows.Add(row);
            return dataTable;
        }

        public static DataTable ToDataTable(this StandardRegistry standardRegistry)
        {
            DataTable dataTable = new DataTable();
            dataTable.Columns.Add(new DataColumn { ColumnName = "Id", DataType = typeof(int) });
            dataTable.Columns.Add(new DataColumn { ColumnName = "Code", DataType = typeof(string) });
            dataTable.Columns.Add(new DataColumn { ColumnName = "Description", DataType = typeof(string) });
            dataTable.Columns.Add(new DataColumn { ColumnName = "Type", DataType = typeof(string) });
            dataTable.Columns.Add(new DataColumn { ColumnName = "Active", DataType = typeof(int) });
            dataTable.Columns.Add(new DataColumn { ColumnName = "CreatedBy", DataType = typeof(string), AllowDBNull = true });

            DataRow row = dataTable.NewRow();
            row["Id"] = standardRegistry.Id;
            row["Code"] = standardRegistry.Code;
            row["Description"] = standardRegistry.Description;
            row["Type"] = standardRegistry.Type;
            row["Active"] = standardRegistry.Active;
            row["CreatedBy"] = (object?)standardRegistry.CreatedBy ?? DBNull.Value;

            dataTable.Rows.Add(row);
            return dataTable;
        }

        public static DataTable ToDataTable(this UserModel userModel)
        {
            DataTable dataTable = new DataTable();
            dataTable.Columns.Add(new DataColumn { ColumnName = "Id", DataType = typeof(int) });
            dataTable.Columns.Add(new DataColumn { ColumnName = "Email", DataType = typeof(string), AllowDBNull = true });
            dataTable.Columns.Add(new DataColumn { ColumnName = "Phone", DataType = typeof(string), AllowDBNull = true });
            dataTable.Columns.Add(new DataColumn { ColumnName = "FirstName", DataType = typeof(string), AllowDBNull = true });
            dataTable.Columns.Add(new DataColumn { ColumnName = "LastName", DataType = typeof(string), AllowDBNull = true });
            dataTable.Columns.Add(new DataColumn { ColumnName = "Birthday", DataType = typeof(DateTime), AllowDBNull = true });
            dataTable.Columns.Add(new DataColumn { ColumnName = "Password", DataType = typeof(string), AllowDBNull = true });
            dataTable.Columns.Add(new DataColumn { ColumnName = "Type", DataType = typeof(string), AllowDBNull = true });

            DataRow row = dataTable.NewRow();
            row["Id"] = userModel.Id;
            row["Email"] = (object?)userModel.Email ?? DBNull.Value;
            row["Phone"] = (object?)userModel.Phone ?? DBNull.Value;
            row["FirstName"] = (object?)userModel.FirstName ?? DBNull.Value;
            row["LastName"] = (object?)userModel.LastName ?? DBNull.Value;
            row["Birthday"] = userModel.Birthday.HasValue ? userModel.Birthday.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value;
            row["Password"] = (object?)userModel.Password ?? DBNull.Value;
            row["Type"] = (object?)userModel.Type ?? DBNull.Value;

            dataTable.Rows.Add(row);
            return dataTable;
        }

        public static DataTable ToDataTable(this UserProfile userProfile)
        {
            DataTable dataTable = new DataTable();
            dataTable.Columns.Add(new DataColumn { ColumnName = "Email", DataType = typeof(string) });
            dataTable.Columns.Add(new DataColumn { ColumnName = "AppC", DataType = typeof(string), AllowDBNull = true });
            dataTable.Columns.Add(new DataColumn { ColumnName = "Parents", DataType = typeof(string), AllowDBNull = true });
            dataTable.Columns.Add(new DataColumn { ColumnName = "Priorities", DataType = typeof(string), AllowDBNull = true });
            dataTable.Columns.Add(new DataColumn { ColumnName = "Statuses", DataType = typeof(string), AllowDBNull = true });
            dataTable.Columns.Add(new DataColumn { ColumnName = "Types", DataType = typeof(string), AllowDBNull = true });
            dataTable.Columns.Add(new DataColumn { ColumnName = "RepeatTypes", DataType = typeof(string), AllowDBNull = true });
            dataTable.Columns.Add(new DataColumn { ColumnName = "IsUpdatedTodays", DataType = typeof(string), AllowDBNull = true });

            DataRow row = dataTable.NewRow();
            row["Email"] = userProfile.Email;
            row["AppC"] = (object?)userProfile.AppC ?? DBNull.Value;
            row["Parents"] = (object?)userProfile.Parents ?? DBNull.Value;
            row["Priorities"] = (object?)userProfile.Priorities ?? DBNull.Value;
            row["Statuses"] = (object?)userProfile.Statuses ?? DBNull.Value;
            row["Types"] = (object?)userProfile.Types ?? DBNull.Value;
            row["RepeatTypes"] = (object?)userProfile.RepeatTypes ?? DBNull.Value;
            row["IsUpdatedTodays"] = (object?)userProfile.IsUpdatedTodays ?? DBNull.Value;

            dataTable.Rows.Add(row);
            return dataTable;
        }

        public static DataTable ToDataTable(this Tag tag)
        {
            DataTable dataTable = new DataTable();
            dataTable.Columns.Add(new DataColumn { ColumnName = "id", DataType = typeof(int) });
            dataTable.Columns.Add(new DataColumn { ColumnName = "user_id", DataType = typeof(int) });
            dataTable.Columns.Add(new DataColumn { ColumnName = "name", DataType = typeof(string) });
            dataTable.Columns.Add(new DataColumn { ColumnName = "parent_id", DataType = typeof(int), AllowDBNull = true });
            dataTable.Columns.Add(new DataColumn { ColumnName = "path", DataType = typeof(string), AllowDBNull = true });
            dataTable.Columns.Add(new DataColumn { ColumnName = "slug", DataType = typeof(string), AllowDBNull = true });
            dataTable.Columns.Add(new DataColumn { ColumnName = "color", DataType = typeof(string), AllowDBNull = true });
            dataTable.Columns.Add(new DataColumn { ColumnName = "icon", DataType = typeof(string), AllowDBNull = true });
            dataTable.Columns.Add(new DataColumn { ColumnName = "description", DataType = typeof(string), AllowDBNull = true });
            dataTable.Columns.Add(new DataColumn { ColumnName = "is_public", DataType = typeof(bool), AllowDBNull = true });
            dataTable.Columns.Add(new DataColumn { ColumnName = "public_slug", DataType = typeof(string), AllowDBNull = true });
            dataTable.Columns.Add(new DataColumn { ColumnName = "created_by", DataType = typeof(int), AllowDBNull = true });

            DataRow row = dataTable.NewRow();
            row["id"] = tag.Id;
            row["user_id"] = tag.UserId;
            row["name"] = tag.Name;
            row["parent_id"] = (object?)tag.ParentId ?? DBNull.Value;
            row["path"] = (object?)tag.Path ?? DBNull.Value;
            row["slug"] = (object?)tag.Slug ?? DBNull.Value;
            row["color"] = (object?)tag.Color ?? DBNull.Value;
            row["icon"] = (object?)tag.Icon ?? DBNull.Value;
            row["description"] = (object?)tag.Description ?? DBNull.Value;
            row["is_public"] = (object?)tag.IsPublic ?? DBNull.Value;
            row["public_slug"] = (object?)tag.PublicSlug ?? DBNull.Value;
            row["created_by"] = (object?)tag.CreatedBy ?? DBNull.Value;

            dataTable.Rows.Add(row);
            return dataTable;
        }
    }
}
