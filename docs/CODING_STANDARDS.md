# Coding Standards

## General Principles

- **SOLID**: Single Responsibility, Open/Closed, Liskov, Interface Segregation, Dependency Inversion
- **DRY**: Don't Repeat Yourself
- **KISS**: Keep It Simple, Stupid
- **YAGNI**: You Aren't Gonna Need It

## Code Style

### Formatting
- **Indentation**: 4 spaces (not tabs)
- **Line length**: Max 120 characters
- **Braces**: Always use braces, even for single-line blocks
- **Spacing**: Space after commas, before/after operators

### Casing
- **PascalCase**: Classes, interfaces, methods, properties, public members
- **camelCase**: Private fields (with `_` prefix), parameters, local variables
- **SCREAMING_SNAKE_CASE**: Constants

```csharp
// Good
public class NoteRepository
{
    private readonly ApplicationDbContext _context;
    private const int MAX_RETRY_ATTEMPTS = 3;

    public async Task<Note> GetNoteByIdAsync(int noteId)
    {
        var note = await _context.Notes.FindAsync(noteId);
        return note;
    }
}
```

## Naming Conventions

| Element | Convention | Example |
|---------|-----------|---------|
| Class | PascalCase | `NoteRepository` |
| Interface | I + PascalCase | `INoteRepository` |
| Method | PascalCase + Async suffix | `GetNotesAsync` |
| Property | PascalCase | `NoteId`, `Title` |
| Private field | _camelCase | `_logger`, `_context` |
| Parameter | camelCase | `userId`, `noteTitle` |
| Local variable | camelCase | `result`, `noteCount` |
| Constant | PascalCase | `MaxRetryAttempts` |
| Enum | PascalCase | `NoteStatus.Draft` |
| Boolean | is/has/can/was | `IsActive`, `HasPermission` |

## Language Features

### C# 12 Features
```csharp
// Primary constructors (C# 12)
public class NoteService(INoteRepository repository, ILogger<NoteService> logger)
{
    public async Task<Note> GetNoteAsync(int id)
    {
        logger.LogInformation("Getting note {NoteId}", id);
        return await repository.GetByIdAsync(id);
    }
}

// Collection expressions (C# 12)
List<int> numbers = [1, 2, 3, 4, 5];
int[] array = [1, 2, 3];

// Required modifier (C# 11)
public required string Title { get; set; }

// Record types
public record NoteResponse(int NoteId, string Title, DateTime CreatedAt);
```

### Nullable Reference Types
```csharp
// Enable nullable context
#nullable enable

public class Note
{
    public string Title { get; set; } = string.Empty;  // Non-nullable
    public string? Description { get; set; }            // Nullable
}
```

### Async/Await
```csharp
// ✅ Good
public async Task<List<Note>> GetNotesAsync()
{
    return await _context.Notes.ToListAsync();
}

// ❌ Bad - Don't use .Result or .Wait()
public List<Note> GetNotes()
{
    return _context.Notes.ToListAsync().Result;  // DON'T DO THIS
}
```

### Using Statements
```csharp
// ✅ Good - Using declaration (C# 8+)
public async Task ProcessFileAsync(string path)
{
    using var stream = File.OpenRead(path);
    // stream disposed at end of method
}

// Also acceptable
using (var connection = await _factory.CreateConnectionAsync())
{
    // connection disposed at end of block
}
```

## Project Structure

### SuperAppModels (Domain)
```
Models/          → Business entities (User, Note, Workspace)
DTOs/
  Requests/      → Input DTOs (CreateNoteRequest)
  Responses/     → Output DTOs (NoteResponse)
Enums/           → Application enums
```

### SuperAppDataRepositories (Infrastructure)
```
Data/
  ApplicationDbContext.cs
  ConnectionFactory.cs
  Configurations/  → EF configurations
Repositories/      → Repository implementations
Ins/              → Repository interfaces
Migrations/       → EF migrations
```

### SuperAppServices (Application)
```
Services/         → Service implementations
Interfaces/       → Service interfaces
Mappings/         → AutoMapper profiles
```

### SuperAppAPI (Presentation)
```
Controllers/      → API controllers
Middlewares/      → Custom middlewares
Exceptions/       → Custom exceptions
Extensions/       → Extension methods
```

## Models vs DTOs

**Models** (SuperAppModels/Models/):
- Business entities
- Database mappings
- Business logic methods
- Internal use

**DTOs** (SuperAppModels/DTOs/):
- Data transfer only
- API boundaries
- Validation attributes
- No business logic

```csharp
// Model - Internal
public class Note
{
    public int NoteId { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public string PasswordHash { get; set; }  // Sensitive

    public void Archive() => IsArchived = true;
}

// DTO - API
public class NoteResponse
{
    public int NoteId { get; set; }
    public string Title { get; set; }
    public DateTime CreatedAt { get; set; }
    // No PasswordHash - filtered out
}
```

## Error Handling

```csharp
// ✅ Good - Specific exceptions
public async Task<Note> GetNoteAsync(int id)
{
    var note = await _repository.GetByIdAsync(id);
    if (note == null)
        throw new NotFoundException("Note", id);

    return note;
}

// ✅ Good - Structured logging
catch (DbUpdateException ex)
{
    _logger.LogError(ex, "Failed to save note {NoteId}", note.NoteId);
    throw new DataAccessException("Failed to save note", ex);
}

// ❌ Bad - Swallow exceptions
catch (Exception)
{
    // Silent failure - DON'T DO THIS
}
```

## Documentation

**When to document:**
- Complex business logic
- Public APIs
- Non-obvious algorithms
- Controllers (OpenAPI/Swagger)

**When NOT to document:**
- Simple properties
- Basic CRUD operations
- Self-explanatory code
- Private methods (usually)

```csharp
/// <summary>
/// Processes payment with retry logic and fraud detection
/// </summary>
/// <param name="amount">Payment amount in USD</param>
/// <returns>Payment confirmation or throws PaymentException</returns>
public async Task<PaymentResult> ProcessPaymentAsync(decimal amount)
{
    // Complex logic...
}

// Simple property - no documentation needed
public string Title { get; set; }
```

## Database Access

### Use EF Core for:
- Simple CRUD (single table)
- 2-3 JOINs
- Standard queries

### Use Stored Procedures for:
- Complex aggregations (5+ JOINs)
- Recursive queries (CTEs)
- Reporting
- Bulk operations
- Performance-critical queries

## Performance

### Async Operations
```csharp
// ✅ Always use async for I/O
public async Task<List<Note>> GetNotesAsync()
{
    return await _context.Notes.ToListAsync();
}
```

### Memory Efficiency
```csharp
// ✅ Use StringBuilder for string concatenation
var sb = new StringBuilder();
foreach (var note in notes)
{
    sb.AppendLine(note.Title);
}

// ✅ Use AsNoTracking for read-only
var notes = await _context.Notes.AsNoTracking().ToListAsync();
```

## Best Practices Summary

### ✅ DO
- Use meaningful names
- Keep methods under 50 lines
- Keep classes under 500 lines
- Use async/await for I/O
- Use dependency injection
- Log errors with context
- Validate all inputs
- Use DTOs at API boundaries
- Write unit tests
- Use const/readonly where appropriate

### ❌ DON'T
- Use magic numbers/strings
- Catch and swallow exceptions
- Use .Result or .Wait()
- Expose domain models in APIs
- Hardcode connection strings
- Use var when type is unclear
- Skip null checks
- Duplicate code
- Mix concerns (keep SRP)
- Skip documentation for complex logic

## Code Organization

### Class Member Order
1. Constants
2. Static fields
3. Private fields
4. Constructor(s)
5. Public properties
6. Public methods
7. Protected methods
8. Private methods

```csharp
public class NoteRepository
{
    // 1. Constants
    private const int MaxRetries = 3;

    // 2. Private fields
    private readonly ApplicationDbContext _context;
    private readonly ILogger<NoteRepository> _logger;

    // 3. Constructor
    public NoteRepository(ApplicationDbContext context, ILogger<NoteRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    // 4. Public properties
    public bool IsConnected { get; private set; }

    // 5. Public methods
    public async Task<Note> GetByIdAsync(int id) { }

    // 6. Private methods
    private void ValidateNote(Note note) { }
}
```

---

**Version:** 2.0
**Last Updated:** November 2025
**See Also:** ARCHITECTURE.md, API_DESIGN.md
