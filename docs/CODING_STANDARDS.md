# Coding Standards

[← Back to Main Documentation](../.copilot-instructions.md.md)

---

## Table of Contents
1. [Naming Conventions](#naming-conventions)
2. [File Organization](#file-organization)
3. [Code Structure](#code-structure)
4. [Async/Await Guidelines](#asyncawait-guidelines)
5. [Comments and Documentation](#comments-and-documentation)
6. [SOLID Principles](#solid-principles)
7. [Code Examples](#code-examples)

---

## Naming Conventions

### Classes and Interfaces

```csharp
// ✅ Classes: PascalCase
public class NoteRepository { }
public class CreateNoteCommandHandler { }

// ✅ Interfaces: I + PascalCase
public interface INoteRepository { }
public interface IConnectionFactory { }

// ❌ Bad naming
public class noteRepository { }      // Wrong case
public class NRepo { }               // Too abbreviated
public class NoteRepositoryClass { } // Redundant suffix
```

### Methods

```csharp
// ✅ Methods: PascalCase, descriptive verbs
public async Task<Note> GetNoteByIdAsync(int id) { }
public void ProcessPayment(decimal amount) { }
public bool IsValidEmail(string email) { }

// ❌ Bad naming
public async Task<Note> get(int id) { }        // Wrong case
public void Process(decimal amount) { }        // Too vague
public bool Check(string email) { }            // Unclear
```

### Fields and Properties

```csharp
// ✅ Private fields: _camelCase
private readonly ILogger _logger;
private readonly IConnectionFactory _connectionFactory;

// ✅ Public properties: PascalCase
public string Name { get; set; }
public int NoteId { get; init; }

// ✅ Constants: PascalCase
public const int MaxRetryAttempts = 3;
public const string DefaultCulture = "en-US";

// ✅ Static readonly: PascalCase
public static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(20);

// ❌ Bad naming
private ILogger logger;           // Missing underscore
public string name { get; set; }  // Wrong case for public
public const int MAX_RETRIES = 3; // Use PascalCase, not UPPER_CASE
```

### Parameters and Local Variables

```csharp
// ✅ Parameters: camelCase
public void ProcessNote(Note note, string userName, bool isActive) { }

// ✅ Local variables: camelCase
var noteList = new List<Note>();
var userId = GetUserId();
var isValid = ValidateInput(input);

// ❌ Bad naming
public void ProcessNote(Note Note, string UserName) { }  // Wrong case
var NoteList = new List<Note>();                         // Wrong case
var x = GetUserId();                                     // Too vague
```

### Booleans

```csharp
// ✅ Use is/has/can prefix
public bool IsActive { get; set; }
public bool HasPermission { get; set; }
public bool CanDelete { get; set; }

// ✅ In methods
private bool IsValidEmail(string email) { }
private bool HasAccessRights(int userId) { }

// ❌ Bad naming
public bool Active { get; set; }      // Missing prefix
public bool Permission { get; set; }  // Missing prefix
private bool ValidEmail(string email) { } // Missing prefix
```

---

## File Organization

### One Class Per File

```
✅ Good structure:
CreateNoteCommand.cs         → Contains CreateNoteCommand class only
CreateNoteCommandHandler.cs  → Contains CreateNoteCommandHandler class only
CreateNoteValidator.cs       → Contains CreateNoteValidator class only

❌ Bad structure:
NoteCommands.cs → Contains multiple command classes
```

### File Naming

- **File name must match class name exactly**
- Use PascalCase
- Be descriptive

```
✅ Good:
CreateNoteCommandHandler.cs
NoteRepository.cs
GlobalExceptionMiddleware.cs

❌ Bad:
Handler1.cs
NoteRepo.cs
Middleware.cs
```

### Using Statements

```csharp
// ✅ Good - Organized and minimal
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SuperApp.Domain.Entities;

namespace SuperApp.Infrastructure.Repositories;

public class NoteRepository
{
    // Implementation
}

// ❌ Bad - Unused usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
// ... 20 more unused namespaces
```

**Tip:** Use IDE to remove unused usings automatically (Ctrl+R, Ctrl+G in Visual Studio)

---

## Code Structure

### Class Member Order

```csharp
public class NoteRepository : BaseRepository, INoteRepository
{
    // 1. Constants
    private const int MaxRetries = 3;
    
    // 2. Static fields
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(20);
    
    // 3. Private fields
    private readonly ILogger<NoteRepository> _logger;
    private readonly IConnectionFactory _connectionFactory;
    
    // 4. Constructor(s)
    public NoteRepository(
        IConnectionFactory connectionFactory,
        ILogger<NoteRepository> logger)
        : base(connectionFactory, logger)
    {
        _logger = logger;
        _connectionFactory = connectionFactory;
    }
    
    // 5. Public properties
    public int TotalQueries { get; private set; }
    
    // 6. Public methods
    public async Task<List<Note>> GetNotesAsync(bool getAll, string? searchText)
    {
        // Implementation
    }
    
    // 7. Private methods
    private bool IsValidSearchText(string? searchText)
    {
        // Implementation
    }
}
```

### Method Length

- **Maximum:** 50 lines per method
- **Ideal:** 10-20 lines per method
- Extract complex logic into private methods

```csharp
// ✅ Good - Short, focused methods
public async Task<Note> CreateNoteAsync(Note note)
{
    ValidateNote(note);
    await SaveToDatabase(note);
    await SendNotification(note);
    return note;
}

private void ValidateNote(Note note)
{
    if (string.IsNullOrWhiteSpace(note.Name))
        throw new ValidationException("Note name is required");
}

// ❌ Bad - Too long, doing too much
public async Task<Note> CreateNoteAsync(Note note)
{
    // 100 lines of validation, database access, notifications...
}
```

### Line Length

- **Maximum:** 120 characters
- Break long lines logically

```csharp
// ✅ Good - Readable
var result = await ExecuteStoredProcedure(
    StoredProcedures.SelectNotes,
    addParameters: (cmd) => AddParameters(cmd, getAll, searchText),
    mapResult: MapToList<Note>
);

// ❌ Bad - Too long
var result = await ExecuteStoredProcedure(StoredProcedures.SelectNotes, addParameters: (cmd) => AddParameters(cmd, getAll, searchText), mapResult: MapToList<Note>);
```

---

## Async/Await Guidelines

### Async Method Naming

```csharp
// ✅ All async methods end with "Async"
public async Task<Note> GetNoteByIdAsync(int id) { }
public async Task<List<Note>> GetNotesAsync() { }
public async Task SaveNoteAsync(Note note) { }

// ❌ Bad - Missing Async suffix
public async Task<Note> GetNoteById(int id) { }
public async Task<List<Note>> GetNotes() { }
```

### Return Types

```csharp
// ✅ Use Task<T> for methods with return value
public async Task<Note> GetNoteByIdAsync(int id)
{
    return await _repository.GetNoteByIdAsync(id);
}

// ✅ Use Task for methods without return value
public async Task DeleteNoteAsync(int id)
{
    await _repository.DeleteNoteAsync(id);
}

// ❌ Bad - Avoid async void (except event handlers)
public async void DeleteNote(int id)  // Can't be awaited, swallows exceptions
{
    await _repository.DeleteNoteAsync(id);
}
```

### Always Await

```csharp
// ✅ Good - Always await async calls
public async Task<Note> GetNoteAsync(int id)
{
    return await _repository.GetNoteByIdAsync(id);
}

// ❌ Bad - Don't return Task directly unless necessary
public Task<Note> GetNoteAsync(int id)
{
    return _repository.GetNoteByIdAsync(id);  // No async/await
}
```

### ConfigureAwait

```csharp
// In library code (not ASP.NET Core controllers):
var result = await SomeMethodAsync().ConfigureAwait(false);

// In ASP.NET Core controllers:
var result = await SomeMethodAsync();  // ConfigureAwait not needed
```

---

## Comments and Documentation

### XML Documentation

All public APIs must have XML documentation:

```csharp
/// <summary>
/// Retrieves a note by its unique identifier.
/// </summary>
/// <param name="id">The unique identifier of the note.</param>
/// <returns>The note if found, otherwise throws NotFoundException.</returns>
/// <exception cref="NotFoundException">Thrown when note with specified ID doesn't exist.</exception>
public async Task<Note> GetNoteByIdAsync(int id)
{
    var note = await _repository.GetNoteByIdAsync(id);
    return note ?? throw new NotFoundException($"Note with ID {id} not found");
}
```

### Inline Comments

Comment **WHY**, not **WHAT**:

```csharp
// ✅ Good - Explains reasoning
// Using stored procedure for performance on large datasets (100k+ records)
return await ExecuteStoredProcedure(StoredProcedures.SelectNotes, ...);

// ✅ Good - Explains non-obvious logic
// Hash password with PBKDF2 (100k iterations) to meet OWASP recommendations
var hashedPassword = PasswordHelper.HashPassword(plainPassword);

// ❌ Bad - States the obvious
// Create a new list
var noteList = new List<Note>();

// ❌ Bad - Describes what code does (code is self-documenting)
// Loop through each note
foreach (var note in notes)
{
    // Add note to list
    noteList.Add(note);
}
```

### TODO Comments

```csharp
// ✅ Good - Actionable TODO with context
// TODO: Implement caching for frequently accessed notes (ETA: Sprint 12)
public async Task<Note> GetNoteByIdAsync(int id)

// ❌ Bad - Vague TODO
// TODO: fix this
public async Task<Note> GetNoteByIdAsync(int id)
```

### Avoid Over-Commenting

```csharp
// ❌ Bad - Too many obvious comments
public class Note
{
    // The note ID
    public int NoteId { get; set; }
    
    // The note name
    public string Name { get; set; }
    
    // The note description
    public string? Description { get; set; }
}

// ✅ Good - Clean, self-documenting
public class Note
{
    public int NoteId { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
}
```

---

## SOLID Principles

### Single Responsibility Principle (SRP)

Each class should have one reason to change:

```csharp
// ✅ Good - Separated responsibilities
public class NoteRepository
{
    public async Task<Note> GetNoteByIdAsync(int id) { }
}

public class NoteEmailService
{
    public async Task SendNoteCreatedEmailAsync(Note note) { }
}

// ❌ Bad - Multiple responsibilities
public class NoteRepository
{
    public async Task<Note> GetNoteByIdAsync(int id) { }
    public async Task SendNoteCreatedEmailAsync(Note note) { } // Email logic in repository!
}
```

### Open/Closed Principle (OCP)

Open for extension, closed for modification:

```csharp
// ✅ Good - Use inheritance/interfaces
public abstract class BaseRepository
{
    protected async Task<T> ExecuteStoredProcedure<T>(...) { }
}

public class NoteRepository : BaseRepository
{
    // Extends base functionality without modifying it
}

// ❌ Bad - Modifying existing class for every new requirement
public class Repository
{
    public async Task<Note> GetNote() { }
    public async Task<User> GetUser() { }
    public async Task<Product> GetProduct() { }
    // Keeps growing...
}
```

### Dependency Inversion Principle (DIP)

Depend on abstractions, not concrete implementations:

```csharp
// ✅ Good - Depends on interface
public class NotesController : ControllerBase
{
    private readonly IMediator _mediator;  // Interface, not concrete class
    
    public NotesController(IMediator mediator)
    {
        _mediator = mediator;
    }
}

// ❌ Bad - Depends on concrete class
public class NotesController : ControllerBase
{
    private readonly NoteRepository _repository;  // Concrete class!
    
    public NotesController(NoteRepository repository)
    {
        _repository = repository;
    }
}
```

---

## Code Examples

### Complete Example - Well-Structured Class

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SuperApp.Domain.Entities;
using SuperApp.Infrastructure.Data;

namespace SuperApp.Infrastructure.Repositories;

/// <summary>
/// Repository for managing note data access operations.
/// </summary>
public class NoteRepository : BaseRepository, INoteRepository
{
    private const int DefaultPageSize = 50;
    private readonly ILogger<NoteRepository> _logger;

    public NoteRepository(
        IConnectionFactory connectionFactory,
        ILogger<NoteRepository> logger)
        : base(connectionFactory, logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all notes with optional filtering.
    /// </summary>
    /// <param name="getAll">If true, retrieves all notes; otherwise, user-specific notes.</param>
    /// <param name="searchText">Optional search term for filtering by name or description.</param>
    /// <returns>List of notes matching the criteria.</returns>
    public async Task<List<Note>> GetNotesAsync(bool getAll, string? searchText)
    {
        _logger.LogInformation("Retrieving notes with filter: getAll={GetAll}, searchText={SearchText}",
            getAll, searchText);

        return await ExecuteStoredProcedure(
            StoredProcedures.SelectNotes,
            addParameters: (cmd) =>
            {
                cmd.Parameters.Add(new SqlParameter("@iv_getAll", getAll));
                AddParameterIfNotNull(cmd, "@iv_SearchText", searchText);
            },
            mapResult: MapToList<Note>
        );
    }

    /// <summary>
    /// Retrieves a specific note by its unique identifier.
    /// </summary>
    /// <param name="id">The note identifier.</param>
    /// <returns>The note if found.</returns>
    /// <exception cref="NotFoundException">Thrown when note doesn't exist.</exception>
    public async Task<Note> GetNoteByIdAsync(int id)
    {
        var notes = await ExecuteStoredProcedure(
            StoredProcedures.SelectNoteById,
            addParameters: (cmd) => cmd.Parameters.Add(new SqlParameter("@iv_NoteId", id)),
            mapResult: MapToList<Note>
        );

        return notes.FirstOrDefault()
            ?? throw new NotFoundException($"Note with ID {id} not found");
    }

    /// <summary>
    /// Creates a new note.
    /// </summary>
    /// <param name="note">The note to create.</param>
    /// <returns>The created note with generated ID.</returns>
    public async Task<Note> CreateNoteAsync(Note note)
    {
        _logger.LogInformation("Creating new note with name: {NoteName}", note.Name);

        ValidateNote(note);

        // Implementation...
        return note;
    }

    private void ValidateNote(Note note)
    {
        if (string.IsNullOrWhiteSpace(note.Name))
            throw new ValidationException("Note name is required");

        if (note.Name.Length > 200)
            throw new ValidationException("Note name cannot exceed 200 characters");
    }
}
```

---

## Quick Reference

### Naming Summary

| Element | Convention | Example |
|---------|-----------|---------|
| Class | PascalCase | `NoteRepository` |
| Interface | I + PascalCase | `INoteRepository` |
| Method | PascalCase + Async suffix | `GetNotesAsync()` |
| Private field | _camelCase | `_logger` |
| Public property | PascalCase | `NoteId` |
| Parameter | camelCase | `userId` |
| Local variable | camelCase | `noteList` |
| Constant | PascalCase | `MaxRetryAttempts` |
| Boolean | is/has/can prefix | `IsActive`, `HasPermission` |

### File Organization

1. Constants
2. Static fields
3. Private fields
4. Constructor(s)
5. Public properties
6. Public methods
7. Private methods

### Code Metrics

- **Method length:** Max 50 lines (ideal: 10-20)
- **Line length:** Max 120 characters
- **Class size:** Max 500 lines
- **Parameters:** Max 4 per method

---

[← Back to Main Documentation](../.copilot-instructions.md.md) | [Next: Architecture Guide →](ARCHITECTURE.md)