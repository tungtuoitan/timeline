# Database Access Guide

## Hybrid Approach

Strategy: EF Core (80%) for simple ops; Stored Procedures (20%) for complex.

| Use Case | Tech | Reason |
|----------|------|--------|
| Simple CRUD | EF Core | Type-safe, maintainable |
| 2-3 JOINs | EF Core | Readable LINQ |
| Complex aggregations | SP | Performance |
| Reporting/Bulk | SP | Optimized |

Decision Tree:
- Simple CRUD? → EF Core
- 2-3 JOINs? → EF Core
- Complex (5+ JOINs, CTEs, bulk)? → SP

Examples:
- EF: FindAsync, Where/OrderBy, Include.
- SP: Statistics report, hierarchy, bulk archive.

## Table of Contents
- [Database Access Guide](#database-access-guide)
  - [Hybrid Approach](#hybrid-approach)
  - [Table of Contents](#table-of-contents)
  - [EF Core Operations](#ef-core-operations)
  - [Database Configuration](#database-configuration)
  - [Connection Management](#connection-management)
  - [BaseRepository Pattern](#baserepository-pattern)
  - [Stored Procedures](#stored-procedures)
  - [Parameter Handling](#parameter-handling)
  - [Models vs DTOs](#models-vs-dtos)
  - [Data Mapping](#data-mapping)
  - [Error Handling](#error-handling)
  - [Best Practices](#best-practices)
  - [Repository Example](#repository-example)

## EF Core Operations

Setup: See EF_CORE_GUIDE.md.

```csharp
public class NoteRepository : BaseRepository, INoteRepository
{
    private readonly ApplicationDbContext _context;

    public NoteRepository(ApplicationDbContext context, IConnectionFactory connFactory, ILogger<NoteRepository> logger)
        : base(context, connFactory, logger) { _context = context; }

    public async Task<Note?> GetNoteByIdAsync(int id) => await _context.Notes.Include(n => n.Members).FirstOrDefaultAsync(n => n.NoteId == id);
    public async Task<Note> CreateNoteAsync(Note note) { _context.Notes.Add(note); await _context.SaveChangesAsync(); return note; }
}
```

Query Patterns:
- Projection: Select to DTO.
- Pagination: Skip/Take.
- Eager: Include/ThenInclude.
- NoTracking: For read-only.

## Database Configuration

DB Names:
- Dev: SuperApp-dev, UserProfile-dev
- Prod: SuperApp-prod, UserProfile-prod
- Test: SuperApp_Test, UserProfile_Test

Connections: appsettings.json (empty), user secrets/env vars for creds.

## Connection Management

IConnectionFactory:
```csharp
public interface IConnectionFactory
{
    Task<SqlConnection> CreateSuperAppConnectionAsync();
    Task<SqlConnection> CreateUserProfileConnectionAsync();
}
```

Impl:
```csharp
public class ConnectionFactory : IConnectionFactory
{
    public async Task<SqlConnection> CreateSuperAppConnectionAsync()
    {
        var conn = new SqlConnection(_configuration.GetConnectionString("SuperAppConnection"));
        await conn.OpenAsync(); return conn;
    }
    // Similar for UserProfile
}
```

## BaseRepository Pattern

Unified for EF/SP.
```csharp
public abstract class BaseRepository
{
    protected readonly ApplicationDbContext _context;
    protected readonly IConnectionFactory _connectionFactory;
    protected readonly ILogger _logger;

    protected BaseRepository(ApplicationDbContext context, IConnectionFactory connFactory, ILogger logger) { /* assign */ }

    // EF: Set<T>(), Query<T>(), FindByIdAsync, AddAsync, UpdateAsync, DeleteAsync, SaveChangesAsync.

    // SP: ExecuteStoredProcedure<T>(string sp, Func<SqlCommand, Task> addParams, Func<SqlDataReader, Task<T>> mapResult, bool useSuperApp = true)
    // ExecuteNonQuery<TResult>(string sp, Func<SqlCommand, Task> addParams, Func<SqlCommand, TResult> extractResult)

    protected async Task<List<T>> MapToList<T>(SqlDataReader reader) where T : new() { /* read and map */ }
    protected async Task<T?> MapToSingleOrDefault<T>(SqlDataReader reader) where T : class, new() { /* read one */ }
}
```

## Stored Procedures

See STORED_PROCEDURES.md.

Naming: usp_[prefix]_[entity], e.g., usp_s_Notes.

Prefixes: s_ (select), i_ (insert), u_ (update), d_ (delete), iu_ (insert/update).

Constants:
```csharp
public static class StoredProcedures
{
    public static string spSelectNotes => "[dbo].[usp_s_Notes]";
    // etc.
}
```

Use: Always via constants.

## Parameter Handling

Input:
```csharp
addParameters: (cmd) => { cmd.Parameters.Add(new SqlParameter("@iv_UserId", userId)); AddParameterIfNotNull(cmd, "@iv_SearchText", searchText); }
```

Output:
```csharp
var (result, outputParams) = await ExecuteStoredProcedureWithOutputAsync(/* ... */);
var success = (bool)outputParams[0].Value;
var errorMsg = outputParams[1].Value?.ToString();
```

## Models vs DTOs

Models: Match DB schema exactly; use for standard CRUD.

DTOs: For custom queries, aggregations, reports.

| Scenario | Use | Reason |
|----------|-----|--------|
| CRUD | Model | Matches table |
| Custom JOINs | DTO | Diff columns |
| Aggregates | DTO | Calculated fields |

Examples: Note model matches Notes table; Use NoteWithTagsDto for summaries.

AutoMapper: Separate profiles for DTO-Model.

## Data Mapping

Automatic: MapToList<T> / MapToSingle<T> (property matching).

Manual: Custom logic in mapResult func.

## Error Handling

SQL Exceptions:
```csharp
catch (SqlException ex)
{
    switch (ex.Number) { case 2: throw TimeoutException; case 18456: throw Unauthorized; default: throw; }
}
```

Business: Check output errorMsg, throw domain exceptions.

## Best Practices

- Use IConnectionFactory.
- Parameterized params.
- Async everywhere.
- Using for disposal.
- Structured logging.

## Repository Example

```csharp
public class NoteRepository : BaseRepository, INoteRepository
{
    public async Task<List<Note>> GetNotesAsync(string userEmail, bool getAll, string? searchText)
    {
        return await ExecuteStoredProcedure(StoredProcedures.spSelectNotes, addParameters: (cmd) => { /* params */ }, mapResult: MapToList<Note>);
    }

    public async Task<ResultOptions> IuNote(Note note)
    {
        // Execute with outputs, extract success/error/noteId
    }
}
```