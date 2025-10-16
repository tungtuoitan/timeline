# Database Access Guide

[← Back to Main Documentation](../.github/copilot-instructions.md)

---

## 🎯 Hybrid Approach: EF Core + Stored Procedures

**Strategy:** Use **Entity Framework Core (ORM)** for 80% of operations, and **Stored Procedures** for complex queries (20%).

| Use Case | Technology | Reason |
|----------|-----------|---------|
| **Simple CRUD** | ✅ EF Core | Type-safe, clean code, easy maintenance |
| **Queries with 2-3 JOINs** | ✅ EF Core | LINQ expressions are readable |
| **Navigation properties** | ✅ EF Core | Eager/lazy loading built-in |
| **Complex aggregations** | 🟡 Stored Procedure | Better performance, optimized execution plan |
| **Reporting queries** | 🟡 Stored Procedure | Complex business logic |
| **Bulk operations** | 🟡 Stored Procedure | Better performance for large datasets |

**Quick Links:**
- **[EF Core Guide](EF_CORE_GUIDE.md)** - Complete Entity Framework Core guide
- **[Current Database Schema](DATABASE-CURRENT/INDEX.md)** - Production database documentation

---

## Table of Contents
1. [Database Configuration](#database-configuration)
2. [Hybrid Strategy Overview](#hybrid-strategy-overview)
3. [EF Core Operations](#ef-core-operations)
4. [Stored Procedures](#stored-procedures)
5. [BaseRepository Pattern](#baserepository-pattern)
6. [Connection Management](#connection-management)
7. [Parameter Handling](#parameter-handling)
8. [Data Mapping](#data-mapping)
9. [Error Handling](#error-handling)
10. [Best Practices](#best-practices)

---

## Hybrid Strategy Overview

### When to Use EF Core vs Stored Procedures

```
📊 Decision Tree:

Is it a simple CRUD operation?
├─ YES → Use EF Core
└─ NO → Continue...

Does it involve 2-3 table JOINs with standard relationships?
├─ YES → Use EF Core (with Include/ThenInclude)
└─ NO → Continue...

Is it a complex query with:
  - 5+ table JOINs
  - Complex aggregations (GROUP BY, HAVING)
  - Recursive CTEs
  - Performance-critical operations
  - Bulk updates/deletes
├─ YES → Use Stored Procedure
└─ NO → Use EF Core (try first, optimize later if needed)
```

### Examples by Category

#### ✅ Use EF Core For:

```csharp
// 1. Simple CRUD
var note = await _context.Notes.FindAsync(id);

// 2. Standard queries with filters
var notes = await _context.Notes
    .Where(n => n.UserId == userId && !n.IsArchived)
    .OrderByDescending(n => n.CreatedAt)
    .ToListAsync();

// 3. Queries with navigation properties
var workspace = await _context.Workspaces
    .Include(w => w.Members)
    .Include(w => w.Items)
        .ThenInclude(i => i.ChildTag)
    .FirstOrDefaultAsync(w => w.WorkspaceId == id);

// 4. Dynamic search
var query = _context.Notes.AsQueryable();
if (!string.IsNullOrEmpty(searchText))
    query = query.Where(n => n.Name.Contains(searchText));
if (tagId.HasValue)
    query = query.Where(n => n.Items.Any(i => i.ParentTagId == tagId));
var results = await query.ToListAsync();
```

#### 🟡 Use Stored Procedures For:

```csharp
// 1. Complex reporting with multiple aggregations
var report = await ExecuteStoredProcedure(
    "usp_get_workspace_statistics_report",
    // Complex GROUP BY, multiple CTEs, window functions
);

// 2. Recursive hierarchy queries
var tagTree = await ExecuteStoredProcedure(
    "usp_get_tag_hierarchy_with_counts",
    // Recursive CTE to build full tree with statistics
);

// 3. Bulk operations
await ExecuteStoredProcedure(
    "usp_archive_old_notes",
    // Bulk update with complex conditions
);

// 4. Cross-database queries (if using multiple databases)
var result = await ExecuteStoredProcedure(
    "usp_sync_user_profile_data",
    // Joins across SuperApp and UserProfile databases
);
```

---

## EF Core Operations

### Basic Setup

For complete EF Core guide, see **[EF_CORE_GUIDE.md](EF_CORE_GUIDE.md)**.

```csharp
// Repository with EF Core
public class NoteRepository : BaseRepository, INoteRepository
{
    private readonly ApplicationDbContext _context;
    private readonly IConnectionFactory _connectionFactory; // For SPs only

    public NoteRepository(
        ApplicationDbContext context,
        IConnectionFactory connectionFactory,
        ILogger<NoteRepository> logger)
        : base(connectionFactory, logger)
    {
        _context = context;
    }

    // EF Core: Simple CRUD
    public async Task<Note?> GetNoteByIdAsync(int id)
    {
        return await _context.Notes
            .Include(n => n.Members)
            .Include(n => n.Versions.OrderByDescending(v => v.CreatedAt).Take(5))
            .FirstOrDefaultAsync(n => n.NoteId == id);
    }

    // EF Core: Create
    public async Task<Note> CreateNoteAsync(Note note)
    {
        _context.Notes.Add(note);
        await _context.SaveChangesAsync();
        return note;
    }

    // EF Core: Update
    public async Task<Note> UpdateNoteAsync(Note note)
    {
        _context.Notes.Update(note);
        await _context.SaveChangesAsync();
        return note;
    }

    // Stored Procedure: Complex query
    public async Task<NotesReportDto> GetNotesReportAsync()
    {
        return await ExecuteStoredProcedure(
            StoredProcedures.spGetNotesReport,
            addParameters: (cmd) => Task.CompletedTask,
            mapResult: MapToSingle<NotesReportDto>
        );
    }
}
```

### EF Core Query Patterns

```csharp
// ✅ Projection to DTO (better performance)
var noteDtos = await _context.Notes
    .Where(n => n.UserId == userId)
    .Select(n => new NoteDto
    {
        NoteId = n.NoteId,
        Name = n.Name,
        Description = n.Description,
        MemberCount = n.Members.Count,
        LatestVersion = n.Versions
            .OrderByDescending(v => v.CreatedAt)
            .Select(v => v.VersionNumber)
            .FirstOrDefault()
    })
    .ToListAsync();

// ✅ Pagination
var notes = await _context.Notes
    .Where(n => n.UserId == userId)
    .OrderByDescending(n => n.CreatedAt)
    .Skip((pageNumber - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();

// ✅ Eager loading (prevent N+1)
var workspaces = await _context.Workspaces
    .Include(w => w.Members.Where(m => m.Role == "owner"))
    .Include(w => w.Items)
        .ThenInclude(i => i.ChildTag)
    .Where(w => w.UserId == userId)
    .ToListAsync();

// ✅ Tracking vs No-Tracking
// Read-only queries (better performance)
var notes = await _context.Notes
    .AsNoTracking()
    .Where(n => n.UserId == userId)
    .ToListAsync();

// For updates (use tracking)
var note = await _context.Notes
    .FirstOrDefaultAsync(n => n.NoteId == id);
note.Name = "Updated Name";
await _context.SaveChangesAsync();
```

---

## Database Configuration

### Expected Database Names

The SuperApp backend uses two separate databases:

| Environment | SuperApp Database | UserProfile Database |
|-------------|-------------------|----------------------|
| **Development** | `SuperApp-dev` | `UserProfile-dev` |
| **Production** | `SuperApp-prod` | `UserProfile-prod` |
| **Testing** | `SuperApp_Test` | `UserProfile_Test` |

### Connection String Configuration

Connection strings are configured in `appsettings.json` but should **never** contain actual credentials:

```json
{
  "ConnectionStrings": {
    "SuperAppConnection": "",
    "UserProfileConnection": ""
  }
}
```

#### Setting Up Connection Strings

**For Development (User Secrets):**
```bash
# SuperApp database
dotnet user-secrets set "ConnectionStrings:SuperAppConnection" "Server=(localdb)\\mssqllocaldb;Database=SuperApp-dev;Integrated Security=true;MultipleActiveResultSets=true;TrustServerCertificate=true" --project SuperAppAPI

# UserProfile database  
dotnet user-secrets set "ConnectionStrings:UserProfileConnection" "Server=(localdb)\\mssqllocaldb;Database=UserProfile-dev;Integrated Security=true;MultipleActiveResultSets=true;TrustServerCertificate=true" --project SuperAppAPI
```

**For Production (Environment Variables):**
```bash
# Set these as environment variables on your production server
CONNECTIONSTRINGS__SUPERAPPCONNECTION="Server=your-server;Database=SuperApp-prod;User Id=your-user;Password=your-password;TrustServerCertificate=true"
CONNECTIONSTRINGS__USERPROFILECONNECTION="Server=your-server;Database=UserProfile-prod;User Id=your-user;Password=your-password;TrustServerCertificate=true"
```

### Database Schema Requirements

Each database should contain the necessary stored procedures and tables:

**SuperApp-dev/SuperApp-prod:**
- Notes-related tables and procedures
- Standard registry tables and procedures
- Audit and logging tables

**UserProfile-dev/UserProfile-prod:**
- User authentication tables and procedures
- User profile management tables and procedures
- OAuth and security-related tables

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
Provide unified access to **both EF Core and Stored Procedures** in a single base class.

### Hybrid Implementation

```csharp
/// <summary>
/// Base repository providing BOTH EF Core operations and Stored Procedure execution
/// </summary>
public abstract class BaseRepository
{
    protected readonly ApplicationDbContext _context;
    protected readonly IConnectionFactory _connectionFactory; // For SPs only
    protected readonly ILogger _logger;
    private const int DefaultCommandTimeout = 1200; // 20 minutes

    protected BaseRepository(
        ApplicationDbContext context,
        IConnectionFactory connectionFactory,
        ILogger logger)
    {
        _context = context;
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    // ===============================================
    // EF CORE OPERATIONS (Use these 80% of the time)
    // ===============================================

    /// <summary>
    /// Get DbSet for entity type (EF Core)
    /// </summary>
    protected DbSet<T> Set<T>() where T : class
        => _context.Set<T>();

    /// <summary>
    /// Create queryable for complex LINQ queries (EF Core)
    /// </summary>
    protected IQueryable<T> Query<T>() where T : class
        => _context.Set<T>().AsQueryable();

    /// <summary>
    /// Find entity by ID (EF Core)
    /// </summary>
    protected async Task<T?> FindByIdAsync<T>(params object[] keyValues) where T : class
        => await _context.Set<T>().FindAsync(keyValues);

    /// <summary>
    /// Add entity (EF Core)
    /// </summary>
    protected async Task AddAsync<T>(T entity) where T : class
    {
        await _context.Set<T>().AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Update entity (EF Core)
    /// </summary>
    protected async Task UpdateAsync<T>(T entity) where T : class
    {
        _context.Set<T>().Update(entity);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Delete entity (EF Core)
    /// </summary>
    protected async Task DeleteAsync<T>(T entity) where T : class
    {
        _context.Set<T>().Remove(entity);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Save all changes (EF Core)
    /// </summary>
    protected async Task<int> SaveChangesAsync()
        => await _context.SaveChangesAsync();

    // ===============================================
    // STORED PROCEDURE OPERATIONS (Use for complex queries)
    // ===============================================

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

### Comprehensive Documentation

For complete stored procedure documentation, see: **[Stored Procedures Guide](DATABASE/STORED_PROCEDURES.md)**

The documentation covers:
- **Advanced Tag System** - Hierarchical tagging with sharing and collaboration
- **Notes Management** - CRUD operations with complex business logic
- **User Profile Management** - Profile and authentication procedures
- **Legacy Systems** - Backward compatibility procedures
- **Performance Guidelines** - Optimization and best practices

### Current Naming Convention

```csharp
public static class StoredProcedures
{
    // Core entity operations
    public static string spSelectNotes => "[dbo].[usp_s_Notes]";
    public static string spInsertUpdateNote => "[dbo].[usp_iu_Note]";
    public static string spDeleteNote => "[dbo].[usp_d_Note]";
    
    // Advanced tag system
    public static string spSelectUserTags => "[dbo].[usp_s_user_tags]";
    public static string spInsertTag => "[dbo].[usp_i_tag]";
    public static string spUpdateTag => "[dbo].[usp_u_tag]";
    public static string spDeleteTag => "[dbo].[usp_d_tag]";
    public static string spMoveTag => "[dbo].[usp_move_tag]";
    public static string spShareTag => "[dbo].[usp_share_tag]";
    
    // Universal item tagging
    public static string spTagItem => "[dbo].[usp_tag_item]";
    public static string spUntagItem => "[dbo].[usp_untag_item]";
    public static string spSelectTaggedItems => "[dbo].[sp_s_tagged_items]";
    
    // User profile management
    public static string spSelectUserProfileJson => "[dbo].[usp_s_UserProfileJson]";
    public static string spInsertUpdateUserProfile => "[dbo].[usp_iu_UserProfile]";
}
```

### Operation Prefixes

| Prefix | Purpose | Examples |
|--------|---------|----------|
| `s_` | **Select** operations | `usp_s_Notes`, `usp_s_user_tags` |
| `i_` | **Insert** operations | `usp_i_tag`, `usp_i_Taggable` |
| `u_` | **Update** operations | `usp_u_tag`, `usp_u_tag_share` |
| `d_` | **Delete** operations | `usp_d_tag`, `usp_d_Note` |
| `iu_` | **Insert/Update** operations | `usp_iu_Note`, `usp_iu_UserProfile` |

### Best Practices

#### ✅ Good - Always use constants

```csharp
await ExecuteStoredProcedure(
    StoredProcedures.spSelectNotes,
    addParameters: (cmd) => {
        cmd.Parameters.Add(new SqlParameter("@iv_UserId", userId));
    },
    mapResult: MapToList<Note>
);
```

#### ❌ Bad - Hard-coded strings

```csharp
await ExecuteStoredProcedure(
    "usp_s_Notes", // Hard to maintain, typo-prone
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
        StoredProcedures.spInsertUpdateNote,
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

## Models vs DTOs in Repository Results

### Rule: Models Must Match Database Schema Exactly

**Domain models should match database table columns exactly.** If the stored procedure returns results that don't match 100% with the model properties, use DTOs instead.

#### ✅ Good - Model matches database schema exactly

```csharp
// Database table: Notes
// Columns: NoteId, Name, Description, Type, CreatedBy, CreatedAt, UpdatedAt, IsArchived

public class Note
{
    // These match database columns exactly
    public int NoteId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Type { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsArchived { get; set; }
    
    // Navigation property - not a database column
    public List<Tag> Tags { get; set; } = new List<Tag>();
}

// Repository using model directly (columns match)
public async Task<List<Note>> GetNotesAsync()
{
    return await ExecuteStoredProcedure(
        StoredProcedures.spSelectNotes,
        addParameters: (cmd) => Task.CompletedTask,
        mapResult: MapToList<Note> // ✅ Safe - columns match exactly
    );
}
```

#### ❌ Bad - Using model when columns don't match

```csharp
// If stored procedure returns: NoteId, Title, Content, AuthorName, CreateDate
// But Note model has: NoteId, Name, Description, CreatedBy, CreatedAt

public async Task<List<Note>> GetNotesWithCustomColumnsAsync()
{
    return await ExecuteStoredProcedure(
        StoredProcedures.spSelectNotesCustom,
        addParameters: (cmd) => Task.CompletedTask,
        mapResult: MapToList<Note> // ❌ Will fail - column names don't match
    );
}
```

#### ✅ Good - Using DTO when columns don't match

```csharp
// Create specific DTO for custom query results
public class NoteSearchResultDto
{
    public int NoteId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public DateTime CreateDate { get; set; }
}

// Repository using DTO for custom results
public async Task<List<NoteSearchResultDto>> SearchNotesAsync(string searchTerm)
{
    return await ExecuteStoredProcedure(
        StoredProcedures.spSearchNotesCustom,
        addParameters: (cmd) =>
        {
            cmd.Parameters.Add(new SqlParameter("@SearchTerm", searchTerm));
            return Task.CompletedTask;
        },
        mapResult: MapToList<NoteSearchResultDto> // ✅ Safe - DTO matches result columns
    );
}
```

### When to Use Models vs DTOs in Repositories

| Scenario | Use | Reason |
|----------|-----|---------|
| **Standard CRUD operations** | Domain Model | Database columns match model properties exactly |
| **Simple SELECT statements** | Domain Model | Returns all standard table columns |
| **Custom queries with JOINs** | DTO | Result columns different from single table |
| **Aggregated data** | DTO | Calculated fields, counts, summaries |
| **Reporting queries** | DTO | Flattened data across multiple tables |
| **Search results** | DTO | Custom column names and computed fields |

### Examples by Scenario

#### Standard CRUD - Use Models

```csharp
// ✅ Standard operations - use domain models
public async Task<Note> GetNoteByIdAsync(int id)
{
    return await ExecuteStoredProcedure(
        StoredProcedures.spSelectNoteById,
        addParameters: (cmd) => cmd.Parameters.Add(new SqlParameter("@NoteId", id)),
        mapResult: MapToSingle<Note> // Model matches table exactly
    );
}
```

#### Custom Queries - Use DTOs

```csharp
// ✅ Custom query with JOINs - use DTO
public class NoteWithTagsDto
{
    public int NoteId { get; set; }
    public string NoteName { get; set; } = string.Empty;
    public string TagNames { get; set; } = string.Empty;  // Comma-separated
    public int TagCount { get; set; }                    // Calculated field
}

public async Task<List<NoteWithTagsDto>> GetNotesWithTagsSummaryAsync()
{
    return await ExecuteStoredProcedure(
        StoredProcedures.spSelectNotesWithTagsSummary,
        addParameters: (cmd) => Task.CompletedTask,
        mapResult: MapToList<NoteWithTagsDto> // DTO for custom result structure
    );
}
```

#### Reporting Queries - Use DTOs

```csharp
// ✅ Reporting/Analytics - use specialized DTOs
public class NotesReportDto
{
    public int TotalNotes { get; set; }
    public int ArchivedNotes { get; set; }
    public int ActiveNotes { get; set; }
    public DateTime ReportDate { get; set; }
    public string TopTag { get; set; } = string.Empty;
}

public async Task<NotesReportDto> GetNotesReportAsync()
{
    return await ExecuteStoredProcedure(
        StoredProcedures.spGetNotesReport,
        addParameters: (cmd) => Task.CompletedTask,
        mapResult: MapToSingle<NotesReportDto> // Specialized DTO for report data
    );
}
```

### Database Schema Alignment

Keep your domain models synchronized with database schemas:

```csharp
// ✅ Model exactly matches database table
// Database: [dbo].[Notes]
public class Note
{
    public int NoteId { get; set; }        // [NoteId] [int] IDENTITY(1,1) NOT NULL
    public string Name { get; set; }       // [Name] [nvarchar](200) NOT NULL
    public string? Description { get; set; }// [Description] [nvarchar](max) NULL
    public string? Type { get; set; }      // [Type] [nvarchar](100) NULL
    public string? CreatedBy { get; set; } // [CreatedBy] [nvarchar](100) NULL
    public DateTime CreatedAt { get; set; }// [CreatedAt] [datetime2](7) NOT NULL
    public DateTime? UpdatedAt { get; set; }// [UpdatedAt] [datetime2](7) NULL
    public bool IsArchived { get; set; }   // [IsArchived] [bit] NOT NULL
}

// ✅ Model matches tags table schema
// Database: [dbo].[tags]  
public class Tag
{
    public int Id { get; set; }            // [id] [int] IDENTITY(1,1) NOT NULL
    public int UserId { get; set; }        // [user_id] [int] NOT NULL
    public string Name { get; set; }       // [name] [nvarchar](255) NOT NULL
    public int? ParentId { get; set; }     // [parent_id] [int] NULL
    public string? Path { get; set; }      // [path] [nvarchar](4000) NULL
    public string? Slug { get; set; }      // [slug] [nvarchar](255) NULL
    public string? Color { get; set; }     // [color] [nvarchar](7) NULL
    public string? Icon { get; set; }      // [icon] [nvarchar](50) NULL
    public string? Description { get; set; }// [description] [nvarchar](max) NULL
    public bool? IsPublic { get; set; }    // [is_public] [bit] NULL
    public string? PublicSlug { get; set; }// [public_slug] [nvarchar](255) NULL
    public DateTime? CreatedAt { get; set; }// [created_at] [datetime] NULL
    public DateTime? UpdatedAt { get; set; }// [updated_at] [datetime] NULL
    public DateTime? DeletedAt { get; set; }// [deleted_at] [datetime] NULL
    public int? CreatedBy { get; set; }    // [created_by] [int] NULL
}
```

### AutoMapper Considerations

When using DTOs in repositories, you may need different AutoMapper profiles:

```csharp
// Repository-specific mapping profile
public class RepositoryMappingProfile : Profile
{
    public RepositoryMappingProfile()
    {
        // DTO to Domain Model mapping
        CreateMap<NoteSearchResultDto, Note>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Title))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Content))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreateDate))
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.AuthorName));
    }
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
        StoredProcedures.spSelectNotes,
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
            StoredProcedures.spSelectNotes,
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
            StoredProcedures.spSelectNoteById,
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
            StoredProcedures.spInsertUpdateNote,
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