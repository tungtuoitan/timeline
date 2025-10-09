# Database Access Guide

[← Back to Main Documentation](../.copilot-instructions.md.md)

---

## Table of Contents
1. [Connection Management](#connection-management)
2. [BaseRepository Pattern](#baserepository-pattern)
3. [Stored Procedures](#stored-procedures)
4. [Parameter Handling](#parameter-handling)
5. [Data Mapping](#data-mapping)
6. [Error Handling](#error-handling)
7. [Best Practices](#best-practices)

---

## Connection Management

### IConnectionFactory

Never create database connections directly. Always use `IConnectionFactory`:

```csharp
public interface IConnectionFactory
{
    Task<SqlConnection> CreateSuperAppConnectionAsync();
    Task<SqlConnection> CreateUserProfileConnectionAsync();
}
```

### Implementation

```csharp
public class ConnectionFactory : IConnectionFactory
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConnectionFactory> _logger;

    public ConnectionFactory(IConfiguration configuration, ILogger<ConnectionFactory> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<SqlConnection> CreateSuperAppConnectionAsync()
    {
        var connectionString = _configuration.GetConnectionString("SuperAppConnection");
        
        if (string.IsNullOrEmpty(connectionString))
        {
            _logger.LogError("SuperAppConnection string not found in configuration");
            throw new InvalidOperationException("Database connection string not configured");
        }

        var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        return connection;
    }

    public async Task<SqlConnection> CreateUserProfileConnectionAsync()
    {
        var connectionString = _configuration.GetConnectionString("UserProfileConnection");
        
        if (string.IsNullOrEmpty(connectionString))
        {
            _logger.LogError("UserProfileConnection string not found in configuration");
            throw new InvalidOperationException("Database connection string not configured");
        }

        var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        return connection;
    }
}
```

### Configuration

**secrets.json** (Never commit to Git!)
```json
{
  "ConnectionStrings": {
    "SuperAppConnection": "Server=TUNGHOMEPC\\MSSQLSERVER03;Database=Timeline-dev;Trusted_Connection=True;",
    "UserProfileConnection": "Server=TUNGHOMEPC\\MSSQLSERVER05;Database=SuperApp-dev;Trusted_Connection=True;"
  }
}
```

**appsettings.json** (Safe to commit)
```json
{
  "ConnectionStrings": {
    "SuperAppConnection": "",
    "UserProfileConnection": ""
  }
}
```

---

## BaseRepository Pattern

### Purpose
Eliminate 40% code duplication by centralizing common database operations.

### Implementation

```csharp
public abstract class BaseRepository
{
    protected readonly IConnectionFactory _connectionFactory;
    protected readonly ILogger _logger;
    private const int DefaultCommandTimeout = 1200; // 20 minutes

    protected BaseRepository(IConnectionFactory connectionFactory, ILogger logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    /// <summary>
    /// Executes a stored procedure and returns a single result
    /// </summary>
    protected async Task<T> ExecuteStoredProcedure<T>(
        string storedProcedure,
        Func<SqlCommand, Task> addParameters,
        Func<SqlDataReader, Task<T>> mapResult,
        bool useSuperAppConnection = true)
    {
        try
        {
            using var conn = useSuperAppConnection
                ? await _connectionFactory.CreateSuperAppConnectionAsync()
                : await _connectionFactory.CreateUserProfileConnectionAsync();

            using var command = conn.CreateCommand();
            command.CommandText = storedProcedure;
            command.CommandType = CommandType.StoredProcedure;
            command.CommandTimeout = DefaultCommandTimeout;

            await addParameters(command);

            using var reader = await command.ExecuteReaderAsync();
            return await mapResult(reader);
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, 
                "Database error executing {StoredProcedure} with parameters {Parameters}",
                storedProcedure, GetParameterInfo(addParameters));
            throw;
        }
    }

    /// <summary>
    /// Executes a stored procedure for INSERT/UPDATE/DELETE with output parameters
    /// </summary>
    protected async Task<TResult> ExecuteNonQuery<TResult>(
        string storedProcedure,
        Func<SqlCommand, Task> addParameters,
        Func<SqlCommand, TResult> extractResult,
        bool useSuperAppConnection = true)
    {
        try
        {
            using var conn = useSuperAppConnection
                ? await _connectionFactory.CreateSuperAppConnectionAsync()
                : await _connectionFactory.CreateUserProfileConnectionAsync();

            using var command = conn.CreateCommand();
            command.CommandText = storedProcedure;
            command.CommandType = CommandType.StoredProcedure;
            command.CommandTimeout = DefaultCommandTimeout;

            await addParameters(command);
            await command.ExecuteNonQueryAsync();

            return extractResult(command);
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, 
                "Database error executing {StoredProcedure}",
                storedProcedure);
            throw;
        }
    }

    /// <summary>
    /// Adds parameter with null handling
    /// </summary>
    protected void AddParameterIfNotNull(SqlCommand command, string paramName, object? value)
    {
        command.Parameters.Add(new SqlParameter(paramName, value ?? DBNull.Value));
    }

    /// <summary>
    /// Maps SqlDataReader to List<T>
    /// </summary>
    protected async Task<List<T>> MapToList<T>(SqlDataReader reader) where T : new()
    {
        var list = new List<T>();
        while (await reader.ReadAsync())
        {
            list.Add(reader.MapToObject<T>());
        }
        return list;
    }

    /// <summary>
    /// Maps SqlDataReader to single object or null
    /// </summary>
    protected async Task<T?> MapToSingleOrDefault<T>(SqlDataReader reader) where T : class, new()
    {
        if (await reader.ReadAsync())
        {
            return reader.MapToObject<T>();
        }
        return null;
    }

    private string GetParameterInfo(Func<SqlCommand, Task> addParameters)
    {
        // Helper for logging (implementation details)
        return "...";
    }
}
```

---

## Stored Procedures

### Naming Convention

```csharp
public static class StoredProcedures
{
    // Format: [dbo].[usp_{operation}_{entity}]
    // Operations: s (select), i (insert), u (update), d (delete), iu (insert/update)
    
    // Notes
    public const string SelectNotes = "[dbo].[usp_s_Notes]";
    public const string SelectNoteById = "[dbo].[usp_s_NoteById]";
    public const string InsertNote = "[dbo].[usp_i_Note]";
    public const string UpdateNote = "[dbo].[usp_u_Note]";
    public const string DeleteNote = "[dbo].[usp_d_Note]";
    public const string InsertUpdateNote = "[dbo].[usp_iu_Note]";
    
    // Users
    public const string SelectUserByEmail = "[dbo].[usp_s_UserByEmail]";
    public const string InsertUser = "[dbo].[usp_i_User]";
    
    // UserProfile
    public const string SelectUserProfile = "[dbo].[usp_s_UserProfile]";
    public const string InsertUpdateUserProfile = "[dbo].[usp_iu_UserProfile]";
}
```

### Repository Example

```csharp
public class NoteRepository : BaseRepository, INoteRepository
{
    public NoteRepository(
        IConnectionFactory connectionFactory,
        ILogger<NoteRepository> logger)
        : base(connectionFactory, logger)
    {
    }

    public async Task<List<Note>> GetNotesAsync(bool getAll, string? searchText)
    {
        _logger.LogInformation(
            "Retrieving notes with filter: getAll={GetAll}, searchText={SearchText}",
            getAll, searchText);

        return await ExecuteStoredProcedure(
            StoredProcedures.SelectNotes,
            addParameters: (cmd) =>
            {
                cmd.Parameters.Add(new SqlParameter("@ov_NoteId", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                });

                cmd.Parameters.Add(new SqlParameter("@ov_ErrorMsg", SqlDbType.VarChar, -1)
                {
                    Direction = ParameterDirection.Output
                });
            },
            extractResult: (cmd) =>
            {
                var errorMsg = cmd.Parameters["@ov_ErrorMsg"].Value?.ToString();
                if (!string.IsNullOrEmpty(errorMsg))
                {
                    throw new InvalidOperationException($"Database error: {errorMsg}");
                }

                return (int)cmd.Parameters["@ov_NoteId"].Value;
            }
        );
    }

    public async Task UpdateNoteAsync(Note note)
    {
        await ExecuteNonQuery(
            StoredProcedures.UpdateNote,
            addParameters: async (cmd) =>
            {
                var noteTable = note.ToDataTable();
                cmd.Parameters.Add(new SqlParameter("@Note", SqlDbType.Structured)
                {
                    Value = noteTable,
                    TypeName = "dbo.NoteType"
                });

                cmd.Parameters.Add(new SqlParameter("@ov_ErrorMsg", SqlDbType.VarChar, -1)
                {
                    Direction = ParameterDirection.Output
                });
            },
            extractResult: (cmd) =>
            {
                var errorMsg = cmd.Parameters["@ov_ErrorMsg"].Value?.ToString();
                if (!string.IsNullOrEmpty(errorMsg))
                {
                    throw new InvalidOperationException($"Database error: {errorMsg}");
                }
                return true;
            }
        );
    }

    public async Task DeleteNoteAsync(int id)
    {
        await ExecuteNonQuery(
            StoredProcedures.DeleteNote,
            addParameters: async (cmd) =>
            {
                cmd.Parameters.Add(new SqlParameter("@iv_NoteId", id));
                cmd.Parameters.Add(new SqlParameter("@ov_ErrorMsg", SqlDbType.VarChar, -1)
                {
                    Direction = ParameterDirection.Output
                });
            },
            extractResult: (cmd) =>
            {
                var errorMsg = cmd.Parameters["@ov_ErrorMsg"].Value?.ToString();
                if (!string.IsNullOrEmpty(errorMsg))
                {
                    throw new InvalidOperationException($"Database error: {errorMsg}");
                }
                return true;
            }
        );
    }
}
```

---

## Parameter Handling

### Input Parameters

```csharp
// ✅ Good - Simple parameters
cmd.Parameters.Add(new SqlParameter("@iv_NoteId", noteId));
cmd.Parameters.Add(new SqlParameter("@iv_IsActive", isActive));

// ✅ Good - Nullable parameters using helper
AddParameterIfNotNull(cmd, "@iv_SearchText", searchText);
AddParameterIfNotNull(cmd, "@iv_Description", description);

// ❌ Bad - Manual null checking (don't do this!)
if (!string.IsNullOrEmpty(searchText))
    cmd.Parameters.Add(new SqlParameter("@iv_SearchText", searchText));
else
    cmd.Parameters.Add(new SqlParameter("@iv_SearchText", DBNull.Value));
```

### Output Parameters

```csharp
// Output parameter for scalar value
cmd.Parameters.Add(new SqlParameter("@ov_NoteId", SqlDbType.Int)
{
    Direction = ParameterDirection.Output
});

// Output parameter for error messages
cmd.Parameters.Add(new SqlParameter("@ov_ErrorMsg", SqlDbType.VarChar, -1)
{
    Direction = ParameterDirection.Output
});

// Reading output parameters after execution
var noteId = (int)cmd.Parameters["@ov_NoteId"].Value;
var errorMsg = cmd.Parameters["@ov_ErrorMsg"].Value?.ToString();
```

### Table-Valued Parameters (TVP)

```csharp
// Create DataTable from entity
var noteTable = note.ToDataTable();

// Add as structured parameter
cmd.Parameters.Add(new SqlParameter("@Note", SqlDbType.Structured)
{
    Value = noteTable,
    TypeName = "dbo.NoteType"  // SQL Server user-defined table type
});
```

**Extension Method for DataTable Conversion:**

```csharp
public static class DataTableExtensions
{
    public static DataTable ToDataTable<T>(this T entity) where T : class
    {
        var dataTable = new DataTable();
        var properties = typeof(T).GetProperties();

        // Add columns
        foreach (var prop in properties)
        {
            var type = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
            dataTable.Columns.Add(prop.Name, type);
        }

        // Add row
        var row = dataTable.NewRow();
        foreach (var prop in properties)
        {
            row[prop.Name] = prop.GetValue(entity) ?? DBNull.Value;
        }
        dataTable.Rows.Add(row);

        return dataTable;
    }

    public static DataTable ToDataTable<T>(this IEnumerable<T> items) where T : class
    {
        var dataTable = new DataTable();
        var properties = typeof(T).GetProperties();

        // Add columns
        foreach (var prop in properties)
        {
            var type = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
            dataTable.Columns.Add(prop.Name, type);
        }

        // Add rows
        foreach (var item in items)
        {
            var row = dataTable.NewRow();
            foreach (var prop in properties)
            {
                row[prop.Name] = prop.GetValue(item) ?? DBNull.Value;
            }
            dataTable.Rows.Add(row);
        }

        return dataTable;
    }
}
```

---

## Data Mapping

### Automatic Mapping with Extension Method

```csharp
public static class DbDataReaderMapper
{
    public static T MapToObject<T>(this SqlDataReader reader) where T : new()
    {
        var obj = new T();
        var properties = typeof(T).GetProperties();

        foreach (var prop in properties)
        {
            if (!reader.HasColumn(prop.Name))
                continue;

            var value = reader[prop.Name];
            if (value == DBNull.Value)
            {
                prop.SetValue(obj, null);
            }
            else
            {
                // Handle type conversion
                var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                var convertedValue = Convert.ChangeType(value, targetType);
                prop.SetValue(obj, convertedValue);
            }
        }

        return obj;
    }

    public static bool HasColumn(this IDataReader reader, string columnName)
    {
        for (int i = 0; i < reader.FieldCount; i++)
        {
            if (reader.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
```

### Usage Examples

```csharp
// ✅ Good - Use extension method
using var reader = await command.ExecuteReaderAsync();
while (await reader.ReadAsync())
{
    var note = reader.MapToObject<Note>();
    noteList.Add(note);
}

// Or use the BaseRepository helper
var notes = await MapToList<Note>(reader);

// ❌ Bad - Manual mapping (tedious and error-prone)
while (await reader.ReadAsync())
{
    var note = new Note
    {
        NoteId = reader["NoteId"] != DBNull.Value ? (int)reader["NoteId"] : 0,
        Name = reader["Name"] != DBNull.Value ? reader["Name"].ToString() : null,
        Description = reader["Description"] != DBNull.Value ? reader["Description"].ToString() : null,
        // ... 10 more lines
    };
    noteList.Add(note);
}
```

---

## Error Handling

### Let BaseRepository Handle SQL Exceptions

```csharp
// ✅ Good - BaseRepository logs and re-throws
public async Task<Note?> GetNoteByIdAsync(int id)
{
    return await ExecuteStoredProcedure(
        StoredProcedures.SelectNoteById,
        addParameters: (cmd) =>
        {
            cmd.Parameters.Add(new SqlParameter("@iv_NoteId", id));
            return Task.CompletedTask;
        },
        mapResult: MapToSingleOrDefault<Note>
    );
    // No try-catch needed!
}

// ❌ Bad - Useless try-catch
public async Task<Note?> GetNoteByIdAsync(int id)
{
    try
    {
        return await ExecuteStoredProcedure(...);
    }
    catch
    {
        throw; // Adds no value!
    }
}
```

### Handle Business Logic Errors

```csharp
// ✅ Good - Check output parameters for business errors
public async Task<int> CreateNoteAsync(Note note)
{
    return await ExecuteNonQuery(
        StoredProcedures.InsertNote,
        addParameters: async (cmd) => { /* ... */ },
        extractResult: (cmd) =>
        {
            var errorMsg = cmd.Parameters["@ov_ErrorMsg"].Value?.ToString();
            if (!string.IsNullOrEmpty(errorMsg))
            {
                _logger.LogWarning("Business rule violation: {ErrorMessage}", errorMsg);
                throw new BusinessRuleException(errorMsg);
            }

            return (int)cmd.Parameters["@ov_NoteId"].Value;
        }
    );
}
```

### Transaction Handling (When Needed)

```csharp
public async Task<bool> TransferNoteOwnershipAsync(int noteId, int fromUserId, int toUserId)
{
    using var conn = await _connectionFactory.CreateSuperAppConnectionAsync();
    using var transaction = conn.BeginTransaction();

    try
    {
        // Operation 1: Update note owner
        using (var cmd1 = conn.CreateCommand())
        {
            cmd1.Transaction = transaction;
            cmd1.CommandText = StoredProcedures.UpdateNoteOwner;
            cmd1.CommandType = CommandType.StoredProcedure;
            cmd1.Parameters.Add(new SqlParameter("@iv_NoteId", noteId));
            cmd1.Parameters.Add(new SqlParameter("@iv_NewOwnerId", toUserId));
            await cmd1.ExecuteNonQueryAsync();
        }

        // Operation 2: Log ownership change
        using (var cmd2 = conn.CreateCommand())
        {
            cmd2.Transaction = transaction;
            cmd2.CommandText = StoredProcedures.InsertAuditLog;
            cmd2.CommandType = CommandType.StoredProcedure;
            cmd2.Parameters.Add(new SqlParameter("@iv_NoteId", noteId));
            cmd2.Parameters.Add(new SqlParameter("@iv_OldOwnerId", fromUserId));
            cmd2.Parameters.Add(new SqlParameter("@iv_NewOwnerId", toUserId));
            await cmd2.ExecuteNonQueryAsync();
        }

        transaction.Commit();
        return true;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to transfer note {NoteId} ownership", noteId);
        transaction.Rollback();
        throw;
    }
}
```

---

## Best Practices

### ✅ DO

1. **Always use IConnectionFactory**
   ```csharp
   using var conn = await _connectionFactory.CreateSuperAppConnectionAsync();
   ```

2. **Always inherit from BaseRepository**
   ```csharp
   public class NoteRepository : BaseRepository, INoteRepository
   ```

3. **Use helper methods to reduce duplication**
   ```csharp
   AddParameterIfNotNull(cmd, "@iv_SearchText", searchText);
   var notes = await MapToList<Note>(reader);
   ```

4. **Always use parameterized queries**
   ```csharp
   cmd.Parameters.Add(new SqlParameter("@iv_SearchText", searchText));
   ```

5. **Set appropriate command timeouts**
   ```csharp
   command.CommandTimeout = 1200; // 20 minutes for reports
   ```

6. **Log repository operations**
   ```csharp
   _logger.LogInformation("Retrieving notes with filter: getAll={GetAll}", getAll);
   ```

7. **Use async/await consistently**
   ```csharp
   public async Task<List<Note>> GetNotesAsync(bool getAll)
   ```

8. **Dispose resources properly**
   ```csharp
   using var conn = await _connectionFactory.CreateSuperAppConnectionAsync();
   using var command = conn.CreateCommand();
   using var reader = await command.ExecuteReaderAsync();
   ```

### ❌ DON'T

1. **Don't hardcode connection strings**
   ```csharp
   // ❌ Bad
   var conn = new SqlConnection("Server=...;Database=...");
   ```

2. **Don't create connections directly**
   ```csharp
   // ❌ Bad
   using var conn = new SqlConnection(connectionString);
   ```

3. **Don't use string concatenation for SQL**
   ```csharp
   // ❌ Bad - SQL Injection risk!
   command.CommandText = $"SELECT * FROM Notes WHERE Name = '{searchText}'";
   ```

4. **Don't manually map every property**
   ```csharp
   // ❌ Bad - Use MapToObject<T>() instead
   note.Name = reader["Name"]?.ToString();
   note.Description = reader["Description"]?.ToString();
   // ... 20 more lines
   ```

5. **Don't use empty try-catch blocks**
   ```csharp
   // ❌ Bad
   try { /* ... */ }
   catch { throw; } // Useless!
   ```

6. **Don't swallow exceptions**
   ```csharp
   // ❌ Bad
   try { /* ... */ }
   catch (Exception ex)
   {
       _logger.LogError(ex.Message);
       return null; // Hides the error!
   }
   ```

7. **Don't forget to check output parameters**
   ```csharp
   // ❌ Bad - Ignores error messages
   await command.ExecuteNonQueryAsync();
   return (int)command.Parameters["@ov_NoteId"].Value;
   ```

---

## Command Timeout Guidelines

| Operation Type | Timeout (seconds) | Use Case |
|---------------|-------------------|----------|
| Simple SELECT | 30 | Single record lookup |
| Complex SELECT | 300 (5 min) | Joins, aggregations |
| INSERT/UPDATE | 60 | Standard CRUD |
| Batch Operations | 600 (10 min) | Bulk inserts/updates |
| Reports | 1200 (20 min) | Large dataset reports |
| Data Migration | 3600 (1 hour) | One-time migrations |

```csharp
// Set timeout based on operation
command.CommandTimeout = operation switch
{
    "SimpleQuery" => 30,
    "ComplexQuery" => 300,
    "StandardCRUD" => 60,
    "BatchOperation" => 600,
    "Report" => 1200,
    _ => 1200 // Default
};
```

---

## Performance Tips

### 1. Use Stored Procedures
- Pre-compiled execution plans
- Reduced network traffic
- Better security

### 2. Avoid N+1 Queries
```csharp
// ✅ Good - Single query with JOIN
var notesWithUsers = await _repository.GetNotesWithUsersAsync();

// ❌ Bad - N+1 queries
var notes = await _repository.GetNotesAsync();
foreach (var note in notes)
{
    note.User = await _repository.GetUserByIdAsync(note.UserId); // N queries!
}
```

### 3. Use Appropriate Data Types
```csharp
// Match SQL Server types exactly
cmd.Parameters.Add(new SqlParameter("@iv_NoteId", SqlDbType.Int) { Value = noteId });
cmd.Parameters.Add(new SqlParameter("@iv_Description", SqlDbType.NVarChar, 5000) { Value = description });
```

### 4. Limit Result Sets
```csharp
// Add paging parameters
cmd.Parameters.Add(new SqlParameter("@iv_PageNumber", pageNumber));
cmd.Parameters.Add(new SqlParameter("@iv_PageSize", pageSize));
```

---

## Testing Repositories

### Integration Test Example

```csharp
public class NoteRepositoryTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;
    private readonly NoteRepository _repository;

    public NoteRepositoryTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _repository = _fixture.CreateRepository<NoteRepository>();
    }

    [Fact]
    public async Task GetNotesAsync_WithValidFilter_ReturnsNotes()
    {
        // Arrange
        await _fixture.SeedTestDataAsync();

        // Act
        var notes = await _repository.GetNotesAsync(getAll: true, searchText: "test");

        // Assert
        Assert.NotEmpty(notes);
        Assert.All(notes, n => Assert.Contains("test", n.Name, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CreateNoteAsync_WithValidNote_ReturnsNewId()
    {
        // Arrange
        var note = new Note
        {
            Name = "Test Note",
            Description = "Test Description"
        };

        // Act
        var noteId = await _repository.CreateNoteAsync(note);

        // Assert
        Assert.True(noteId > 0);

        // Cleanup
        await _repository.DeleteNoteAsync(noteId);
    }
}
```

---

[← Back to Main Documentation](../.copilot-instructions.md.md) | [Next: Error Handling →](ERROR_HANDLING.md)("@iv_getAll", getAll));
                AddParameterIfNotNull(cmd, "@iv_SearchText", searchText);
                return Task.CompletedTask;
            },
            mapResult: MapToList<Note>
        );
    }

    public async Task<Note?> GetNoteByIdAsync(int id)
    {
        return await ExecuteStoredProcedure(
            StoredProcedures.SelectNoteById,
            addParameters: (cmd) =>
            {
                cmd.Parameters.Add(new SqlParameter("@iv_NoteId", id));
                return Task.CompletedTask;
            },
            mapResult: MapToSingleOrDefault<Note>
        );
    }

    public async Task<int> CreateNoteAsync(Note note)
    {
        return await ExecuteNonQuery(
            StoredProcedures.InsertNote,
            addParameters: async (cmd) =>
            {
                var noteTable = note.ToDataTable();
                cmd.Parameters.Add(new SqlParameter("@Note", SqlDbType.Structured)
                {
                    Value = noteTable,
                    TypeName = "dbo.NoteType"
                });

                cmd.Parameters.Add(new SqlParameter