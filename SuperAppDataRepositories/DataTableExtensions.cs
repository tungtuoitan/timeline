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
            dataTable.Columns.Add(new DataColumn { ColumnName = "UserId", DataType = typeof(int) });

            DataRow row = dataTable.NewRow();
            row["Id"] = note.NoteId;
            row["Name"] = note.Name;
            row["Description"] = (object?)note.Description ?? DBNull.Value;
            row["UserId"] = note.UserId;

            dataTable.Rows.Add(row);
            return dataTable;
        }

        public static DataTable ToDataTable(this StandardRegistry standardRegistry)
        {
            DataTable dataTable = new DataTable();
            dataTable.Columns.Add(new DataColumn { ColumnName = "Id", DataType = typeof(int) });
            dataTable.Columns.Add(new DataColumn { ColumnName = "TypeCode", DataType = typeof(string) });
            dataTable.Columns.Add(new DataColumn { ColumnName = "Description", DataType = typeof(string), AllowDBNull = true });
            dataTable.Columns.Add(new DataColumn { ColumnName = "IsActive", DataType = typeof(bool) });

            DataRow row = dataTable.NewRow();
            row["Id"] = standardRegistry.Id;
            row["TypeCode"] = standardRegistry.TypeCode;
            row["Description"] = (object?)standardRegistry.Description ?? DBNull.Value;
            row["IsActive"] = standardRegistry.IsActive;

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
            dataTable.Columns.Add(new DataColumn { ColumnName = "Id", DataType = typeof(int) });
            dataTable.Columns.Add(new DataColumn { ColumnName = "UserId", DataType = typeof(int) });
            dataTable.Columns.Add(new DataColumn { ColumnName = "FirstName", DataType = typeof(string), AllowDBNull = true });
            dataTable.Columns.Add(new DataColumn { ColumnName = "LastName", DataType = typeof(string), AllowDBNull = true });
            dataTable.Columns.Add(new DataColumn { ColumnName = "AvatarUrl", DataType = typeof(string), AllowDBNull = true });
            dataTable.Columns.Add(new DataColumn { ColumnName = "Bio", DataType = typeof(string), AllowDBNull = true });
            dataTable.Columns.Add(new DataColumn { ColumnName = "DateOfBirth", DataType = typeof(DateTime), AllowDBNull = true });
            dataTable.Columns.Add(new DataColumn { ColumnName = "Gender", DataType = typeof(string), AllowDBNull = true });
            dataTable.Columns.Add(new DataColumn { ColumnName = "Country", DataType = typeof(string), AllowDBNull = true });
            dataTable.Columns.Add(new DataColumn { ColumnName = "City", DataType = typeof(string), AllowDBNull = true });
            dataTable.Columns.Add(new DataColumn { ColumnName = "Timezone", DataType = typeof(string), AllowDBNull = true });
            dataTable.Columns.Add(new DataColumn { ColumnName = "Language", DataType = typeof(string), AllowDBNull = true });

            DataRow row = dataTable.NewRow();
            row["Id"] = userProfile.Id;
            row["UserId"] = userProfile.UserId;
            row["FirstName"] = (object?)userProfile.FirstName ?? DBNull.Value;
            row["LastName"] = (object?)userProfile.LastName ?? DBNull.Value;
            row["AvatarUrl"] = (object?)userProfile.AvatarUrl ?? DBNull.Value;
            row["Bio"] = (object?)userProfile.Bio ?? DBNull.Value;
            row["DateOfBirth"] = (object?)userProfile.DateOfBirth ?? DBNull.Value;
            row["Gender"] = (object?)userProfile.Gender ?? DBNull.Value;
            row["Country"] = (object?)userProfile.Country ?? DBNull.Value;
            row["City"] = (object?)userProfile.City ?? DBNull.Value;
            row["Timezone"] = (object?)userProfile.Timezone ?? DBNull.Value;
            row["Language"] = (object?)userProfile.Language ?? DBNull.Value;

            dataTable.Rows.Add(row);
            return dataTable;
        }

        public static DataTable ToDataTable(this Tag tag)
        {
            DataTable dataTable = new DataTable();
            dataTable.Columns.Add(new DataColumn { ColumnName = "id", DataType = typeof(int) });
            dataTable.Columns.Add(new DataColumn { ColumnName = "user_id", DataType = typeof(int) });
            dataTable.Columns.Add(new DataColumn { ColumnName = "name", DataType = typeof(string) });
            dataTable.Columns.Add(new DataColumn { ColumnName = "slug", DataType = typeof(string), AllowDBNull = true });
            dataTable.Columns.Add(new DataColumn { ColumnName = "color", DataType = typeof(string), AllowDBNull = true });
            dataTable.Columns.Add(new DataColumn { ColumnName = "icon", DataType = typeof(string), AllowDBNull = true });
            dataTable.Columns.Add(new DataColumn { ColumnName = "description", DataType = typeof(string), AllowDBNull = true });
            dataTable.Columns.Add(new DataColumn { ColumnName = "metadata", DataType = typeof(string), AllowDBNull = true });
            dataTable.Columns.Add(new DataColumn { ColumnName = "usage_count", DataType = typeof(int) });
            dataTable.Columns.Add(new DataColumn { ColumnName = "created_at", DataType = typeof(DateTime), AllowDBNull = true });
            dataTable.Columns.Add(new DataColumn { ColumnName = "updated_at", DataType = typeof(DateTime), AllowDBNull = true });

            DataRow row = dataTable.NewRow();
            row["id"] = tag.TagId;
            row["user_id"] = tag.UserId;
            row["name"] = tag.Name;
            row["slug"] = (object?)tag.Slug ?? DBNull.Value;
            row["color"] = (object?)tag.Color ?? DBNull.Value;
            row["icon"] = (object?)tag.Icon ?? DBNull.Value;
            row["description"] = (object?)tag.Description ?? DBNull.Value;
            row["metadata"] = (object?)tag.Metadata ?? DBNull.Value;
            row["usage_count"] = tag.UsageCount;
            row["created_at"] = (object?)tag.CreatedAt ?? DBNull.Value;
            row["updated_at"] = (object?)tag.UpdatedAt ?? DBNull.Value;

            dataTable.Rows.Add(row);
            return dataTable;
        }
    }
}
