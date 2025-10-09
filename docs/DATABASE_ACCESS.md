# Database Access Guide

[← Back to Main Documentation](../.github/copilot-instructions.md)

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
            throw new InvalidOperationException("SuperApp connection string not configured");
        }

        var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        
        _logger.LogDebug("SuperApp database connection opened");
        return connection;
    }

    public async Task<SqlConnection> CreateUserProfileConnectionAsync()
    {
        var connectionString = _configuration.GetConnectionString("UserProfileConnection");
        
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("UserProfile connection string not configured");
        }

        var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        
        _logger.LogDebug("UserProfile database connection opened");
        return connection;
    }
}
```

---

## BaseRepository Pattern

### Purpose
Eliminate 40% code duplication by centralizing common database operations.

### Implementation

```csharp
/// <summary>
/// Base repository providing common database operation patterns and connection management
/// </summary>
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
    /// Executes a stored procedure with complex parameter mapping and result transformation
    /// </summary>
    /// <typeparam name="T">The type to return from the operation</typeparam>
    /// <param name="storedProcedure">Name of the stored procedure to execute</param>
    /// <param name="addParameters">Function to add parameters to the command</param>
    /// <param name="mapResult">Function to map the result from SqlDataReader</param>
    /// <param name="useSuperAppConnection">True to use SuperApp connection, false for UserProfile</param>
    /// <returns>The mapped result of type T</returns>
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
    /// Executes non-query stored procedures with output parameter extraction and error handling
    /// </summary>
    /// <typeparam name="TResult">Type of result to extract from output parameters</typeparam>
    /// <param name="storedProcedure">Name of the stored procedure to execute</param>
    /// <param name="addParameters">Function to add parameters to the command</param>
    /// <param name="extractResult">Function to extract result from command after execution</param>
    /// <param name="useSuperAppConnection">True to use SuperApp connection, false for UserProfile</param>
    /// <returns>The extracted result from output parameters</returns>
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

    // Simple utility methods - no documentation needed
    protected void AddParameterIfNotNull(SqlCommand command, string paramName, object? value)
    {
        command.Parameters.Add(new SqlParameter(paramName, value ?? DBNull.Value));
    }

    protected async Task<List<T>> MapToList<T>(SqlDataReader reader) where T : new()
    {
        var list = new List<T>();
        while (await reader.ReadAsync())
        {
            list.Add(reader.MapToObject<T>());
        }
        return list;
    }

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
    public const string SelectNotes = "[dbo].[usp_Select_Notes]";
    public const string SelectNoteById = "[dbo].[usp_Select_Note_ById]";
    public const string InsertUpdateNote = "[dbo].[usp_InsertUpdate_Note]";
    public const string DeleteNote = "[dbo].[usp_Delete_Note]";
    
    // UserProfile procedures
    public const string SelectUserProfile = "[dbo].[usp_Select_UserProfile]";
    public const string InsertUpdateUserProfile = "[dbo].[usp_InsertUpdate_UserProfile]";
}
```

### Best Practices

#### ✅ Good - Always use constants

```csharp
await ExecuteStoredProcedure(
    StoredProcedures.SelectNotes,
    addParameters: (cmd) => {
        cmd.Parameters.Add(new SqlParameter("@iv_UserId", userId));
    },
    mapResult: MapToList<Note>
);
```

#### ❌ Bad - Hard-coded strings

```csharp
await ExecuteStoredProcedure(
    "usp_Select_Notes", // Hard to maintain, typo-prone
    // ...
);
```

---

## Parameter Handling

### Input Parameters

```csharp
// ✅ Good - Null-safe parameter handling
addParameters: (cmd) =>
{
    cmd.Parameters.Add(new SqlParameter("@iv_UserId", userId));
    AddParameterIfNotNull(cmd, "@iv_SearchText", searchText);
    AddParameterIfNotNull(cmd, "@iv_CategoryId", categoryId);
}

// ❌ Bad - No null handling
addParameters: (cmd) =>
{
    cmd.Parameters.Add(new SqlParameter("@iv_SearchText", searchText)); // NullReference risk
}
```

### Output Parameters

```csharp
public async Task<ResultOptions> IuNote(Note note)
{
    var (result, outputParams) = await ExecuteStoredProcedureWithOutputAsync<object?>(
        StoredProcedures.InsertUpdateNote,
        addParametersAndGetOutputs: async (command) =>
        {
            // Input parameters
            AddParameterIfNotNull(command, "@iv_Name", note.Name);
            AddParameterIfNotNull(command, "@iv_Description", note.Description);
            AddParameterIfNotNull(command, "@iv_Tags", note.Tags);
            
            // Output parameters
            var successParam = AddOutputParameter(command, "@ov_Success", SqlDbType.Bit);
            var errorMsgParam = AddOutputParameter(command, "@ov_ErrorMsg", SqlDbType.VarChar, 500);
            var noteIdParam = AddOutputParameter(command, "@ov_NoteId", SqlDbType.Int);
            
            await Task.CompletedTask;
            return new[] { successParam, errorMsgParam, noteIdParam };
        },
        mapResult: async (reader) =>
        {
            await Task.CompletedTask;
            return (object?)null;
        }
    );

    // Extract output values
    var success = (bool)(outputParams[0]?.Value ?? false);
    var errorMessage = outputParams[1]?.Value?.ToString();
    var noteId = outputParams[2]?.Value as int?;

    return new ResultOptions
    {
        Success = success,
        ErrorMessage = errorMessage,
        Data = noteId
    };
}
```

---

## Data Mapping

### Automatic Mapping

Use the `MapToObject<T>()` extension method for automatic property mapping:

```csharp
public async Task<List<Note>> GetNotesAsync()
{
    return await ExecuteStoredProcedure(
        StoredProcedures.SelectNotes,
        addParameters: (cmd) => Task.CompletedTask,
        mapResult: MapToList<Note> // Automatic mapping
    );
}
```

### Manual Mapping

For complex scenarios, use manual mapping:

```csharp
mapResult: async (reader) =>
{
    var notes = new List<Note>();
    while (await reader.ReadAsync())
    {
        var note = new Note
        {
            NoteId = reader.GetInt32("NoteId"),
            Name = reader.GetString("Name"),
            Description = reader.IsDBNull("Description") ? null : reader.GetString("Description"),
            CreatedAt = reader.GetDateTime("CreatedAt"),
            // Custom logic for complex fields
            Tags = ParseTagsFromDatabase(reader.GetString("TagsJson"))
        };
        notes.Add(note);
    }
    return notes;
}
```

---

## Error Handling

### Database-Specific Errors

```csharp
try
{
    return await ExecuteStoredProcedure(/* ... */);
}
catch (SqlException ex)
{
    // Handle specific SQL errors
    switch (ex.Number)
    {
        case 2: // Timeout
            _logger.LogWarning("Database timeout for operation: {Operation}", operationName);
            throw new TimeoutException("Database operation timed out", ex);
            
        case 18456: // Login failed
            _logger.LogError("Database authentication failed");
            throw new UnauthorizedAccessException("Database access denied", ex);
            
        default:
            _logger.LogError(ex, "Unexpected database error: {SqlErrorNumber}", ex.Number);
            throw;
    }
}
```

### Business Logic Errors

```csharp
public async Task<ResultOptions> CreateNoteAsync(Note note)
{
    var result = await IuNote(note);
    
    if (!result.Success)
    {
        _logger.LogWarning("Failed to create note: {ErrorMessage}", result.ErrorMessage);
        
        // Convert database error to domain exception
        if (result.ErrorMessage?.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true)
        {
            throw new DuplicateException($"Note with name '{note.Name}' already exists");
        }
        
        throw new InvalidOperationException($"Failed to create note: {result.ErrorMessage}");
    }
    
    return result;
}
```

---

## Best Practices

### 1. Connection Management

```csharp
// ✅ Good - Use IConnectionFactory
public class NoteRepository : BaseRepository
{
    public NoteRepository(IConnectionFactory connectionFactory, ILogger<NoteRepository> logger)
        : base(connectionFactory, logger)
    {
    }
}

// ❌ Bad - Direct connection creation
public class NoteRepository
{
    public async Task<List<Note>> GetNotesAsync()
    {
        using var connection = new SqlConnection("connection string"); // Hard-coded!
    }
}
```

### 2. Parameter Safety

```csharp
// ✅ Good - Parameterized queries
AddParameterIfNotNull(cmd, "@iv_SearchText", searchText);

// ❌ Bad - SQL Injection risk
var sql = $"SELECT * FROM Notes WHERE Name LIKE '%{searchText}%'";
```

### 3. Async Patterns

```csharp
// ✅ Good - Properly async
public async Task<Note> GetNoteAsync(int id)
{
    return await ExecuteStoredProcedure(/* ... */);
}

// ❌ Bad - Blocking async
public Note GetNote(int id)
{
    return ExecuteStoredProcedure(/* ... */).Result; // Deadlock risk!
}
```

### 4. Resource Management

```csharp
// ✅ Good - Using statements for proper disposal
using var connection = await _connectionFactory.CreateSuperAppConnectionAsync();
using var command = connection.CreateCommand();

// ❌ Bad - Manual disposal (error-prone)
var connection = await _connectionFactory.CreateSuperAppConnectionAsync();
try
{
    // ... operations
}
finally
{
    connection.Dispose(); // What if this throws?
}
```

### 5. Logging

```csharp
// ✅ Good - Structured logging
_logger.LogInformation("Retrieving notes for user {UserId} with filter {SearchText}", 
    userId, searchText);

// ❌ Bad - String concatenation
_logger.LogInformation("Retrieving notes for user " + userId + " with filter " + searchText);
```

---

## Repository Implementation Example

```csharp
// No class-level summary needed for standard repositories
public class NoteRepository : BaseRepository, INoteRepository
{
    private readonly ILogger<NoteRepository> _logger;

    public NoteRepository(
        IConnectionFactory connectionFactory, 
        ILogger<NoteRepository> logger)
        : base(connectionFactory, logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Retrieves notes with advanced filtering, user permission checks, and complex business logic
    /// </summary>
    /// <param name="userEmail">Email of the requesting user for permission filtering</param>
    /// <param name="getAll">If true, retrieves all notes; otherwise applies user-specific filters</param>
    /// <param name="searchText">Optional text to search within note names and descriptions</param>
    /// <returns>List of notes matching the criteria with applied security and business filters</returns>
    public async Task<List<Note>> GetNotesAsync(string userEmail, bool getAll = false, string? searchText = null)
    {
        _logger.LogInformation("Retrieving notes for user {UserEmail}, getAll={GetAll}, searchText={SearchText}",
            userEmail, getAll, searchText);

        return await ExecuteStoredProcedure(
            StoredProcedures.SelectNotes,
            addParameters: (cmd) =>
            {
                cmd.Parameters.Add(new SqlParameter("@iv_UserEmail", userEmail));
                cmd.Parameters.Add(new SqlParameter("@iv_GetAll", getAll));
                AddParameterIfNotNull(cmd, "@iv_SearchText", searchText);
                return Task.CompletedTask;
            },
            mapResult: MapToList<Note>
        );
    }

    // Simple CRUD - no documentation needed
    public async Task<Note?> GetNoteByIdAsync(int noteId)
    {
        return await ExecuteStoredProcedure(
            StoredProcedures.SelectNoteById,
            addParameters: (cmd) =>
            {
                cmd.Parameters.Add(new SqlParameter("@iv_NoteId", noteId));
                return Task.CompletedTask;
            },
            mapResult: MapToSingleOrDefault<Note>
        );
    }

    /// <summary>
    /// Creates or updates a note with complex validation, audit logging, and business rule processing
    /// </summary>
    /// <param name="note">Note entity with all required fields for creation or update</param>
    /// <returns>Operation result with success status, error messages, and created/updated note ID</returns>
    public async Task<ResultOptions> IuNote(Note note)
    {
        _logger.LogInformation("Saving note with ID {NoteId} and name {NoteName}", 
            note.NoteId, note.Name);

        var (_, outputParams) = await ExecuteStoredProcedureWithOutputAsync<object?>(
            StoredProcedures.InsertUpdateNote,
            addParametersAndGetOutputs: async (command) =>
            {
                // Input parameters
                AddParameterIfNotNull(command, "@iv_NoteId", note.NoteId > 0 ? note.NoteId : null);
                AddParameterIfNotNull(command, "@iv_Name", note.Name);
                AddParameterIfNotNull(command, "@iv_Description", note.Description);
                AddParameterIfNotNull(command, "@iv_Tags", note.Tags);
                AddParameterIfNotNull(command, "@iv_Type", note.Type);
                AddParameterIfNotNull(command, "@iv_CreatedBy", note.CreatedBy);

                // Output parameters
                var successParam = AddOutputParameter(command, "@ov_Success", SqlDbType.Bit);
                var errorMsgParam = AddOutputParameter(command, "@ov_ErrorMsg", SqlDbType.VarChar, 500);
                var noteIdParam = AddOutputParameter(command, "@ov_NoteId", SqlDbType.Int);

                await Task.CompletedTask;
                return new[] { successParam, errorMsgParam, noteIdParam };
            },
            mapResult: async (reader) =>
            {
                await Task.CompletedTask;
                return (object?)null;
            }
        );

        var success = (bool)(outputParams[0]?.Value ?? false);
        var errorMessage = outputParams[1]?.Value?.ToString();
        var noteId = outputParams[2]?.Value as int?;

        if (success && noteId.HasValue)
        {
            note.NoteId = noteId.Value;
        }

        return new ResultOptions
        {
            Success = success,
            ErrorMessage = errorMessage,
            Data = noteId
        };
    }
}
```

---

[← Back to Main Documentation](../.github/copilot-instructions.md) | [Next: Validation Guide →](VALIDATION.md)