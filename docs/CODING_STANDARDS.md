# SuperApp Backend - Coding Standards

This document outlines the coding standards and conventions used in the SuperApp backend project. Following these standards ensures consistency, maintainability, and readability across the codebase.

## Table of Contents

- [General Principles](#general-principles)
- [Naming Conventions](#naming-conventions)
- [Code Organization](#code-organization)
- [Error Handling](#error-handling)
- [Documentation Standards](#documentation-standards)
- [Database Access](#database-access)
- [Testing Standards](#testing-standards)
- [Performance Guidelines](#performance-guidelines)

---

## General Principles

### 1. Clean Code Principles

- **SOLID Principles**: Single Responsibility, Open/Closed, Liskov Substitution, Interface Segregation, Dependency Inversion
- **DRY (Don't Repeat Yourself)**: Eliminate code duplication
- **KISS (Keep It Simple, Stupid)**: Prefer simple solutions over complex ones
- **YAGNI (You Aren't Gonna Need It)**: Don't add functionality until it's needed

### 2. Code Style

- Use **4 spaces** for indentation (never tabs)
- Use **PascalCase** for public members
- Use **camelCase** for private fields and parameters
- Use **SCREAMING_SNAKE_CASE** for constants
- Maximum line length: **120 characters**
- Always use braces `{}` even for single-line statements

### 3. Language Features

- Use **nullable reference types** (`string?` vs `string`)
- Prefer **explicit types** over `var` when type isn't obvious
- Use **expression-bodied members** for simple operations
- Prefer **async/await** over `.Result` or `.Wait()`
- Use **using statements** for IDisposable resources

---

## Naming Conventions

### Classes and Interfaces

```csharp
// ✅ Good
public class NoteRepository : BaseRepository, INoteRepository
public interface INoteService
public class CreateNoteCommand : IRequest<NoteResponse>

// ❌ Bad
public class noterepository
public interface NoteService
public class createNoteCmd
```

### Methods

```csharp
// ✅ Good - Async methods end with Async
public async Task<Note> GetNoteByIdAsync(int id)
public async Task<List<Note>> GetAllNotesAsync()
public bool ValidateNote(Note note)

// ❌ Bad
public async Task<Note> GetNoteById(int id)
public async Task<Note> getNoteAsync(int id)
```

### Properties and Fields

```csharp
// ✅ Good
public class NoteService
{
    private readonly ILogger<NoteService> _logger;
    private readonly INoteRepository _noteRepository;
    
    public string Name { get; set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsActive { get; set; }
}

// ❌ Bad
public class NoteService
{
    private ILogger logger;
    private INoteRepository noteRepo;
    
    public string name { get; set; }
    public DateTime created_at { get; set; }
    public bool active { get; set; }
}
```

### Constants and Enums

```csharp
// ✅ Good
public const int MAX_RETRY_ATTEMPTS = 3;
public const string DEFAULT_CONNECTION_STRING = "Data Source=...";

public enum NoteStatus
{
    Draft,
    Published,
    Archived
}

// ❌ Bad
public const int maxRetryAttempts = 3;
public const string default_connection = "Data Source=...";

public enum noteStatus
{
    draft,
    published,
    archived
}
```

### Boolean Properties

```csharp
// ✅ Good - Use descriptive prefixes
public bool IsActive { get; set; }
public bool HasPermission { get; set; }
public bool CanDelete { get; set; }
public bool WasDeleted { get; set; }

// ❌ Bad
public bool Active { get; set; }
public bool Permission { get; set; }
public bool Delete { get; set; }
```

---

## Code Organization

### File Structure

```
SuperApp.Application/
├── Features/
│   ├── Notes/
│   │   ├── Commands/
│   │   │   ├── CreateNote/
│   │   │   │   ├── CreateNoteCommand.cs
│   │   │   │   ├── CreateNoteCommandHandler.cs
│   │   │   │   └── CreateNoteValidator.cs
│   │   │   └── UpdateNote/
│   │   └── Queries/
│   │       └── GetNotes/
│   └── Authentication/
└── Common/
    ├── Mappings/
    ├── Exceptions/
    └── Interfaces/
```

### Class Organization

Order class members in this sequence:

1. **Constants** (public, then private)
2. **Static fields** (public, then private)
3. **Private fields** (grouped by type)
4. **Constructor(s)**
5. **Public properties**
6. **Public methods** (grouped by functionality)
7. **Private methods** (grouped by functionality)

```csharp
public class NoteService
{
    // 1. Constants
    private const int DEFAULT_PAGE_SIZE = 20;
    public const int MAX_NOTE_LENGTH = 5000;
    
    // 2. Static fields
    private static readonly JsonSerializerOptions JsonOptions = new();
    
    // 3. Private fields
    private readonly ILogger<NoteService> _logger;
    private readonly INoteRepository _repository;
    private readonly IMapper _mapper;
    
    // 4. Constructor
    public NoteService(ILogger<NoteService> logger, INoteRepository repository, IMapper mapper)
    {
        _logger = logger;
        _repository = repository;
        _mapper = mapper;
    }
    
    // 5. Public properties
    public string ServiceName { get; } = "NoteService";
    
    // 6. Public methods
    public async Task<Note> CreateNoteAsync(Note note)
    {
        ValidateNote(note);
        return await _repository.CreateAsync(note);
    }
    
    // 7. Private methods
    private void ValidateNote(Note note)
    {
        if (string.IsNullOrWhiteSpace(note.Name))
            throw new ValidationException("Note name is required");
    }
}
```

---

## Error Handling

### Exception Types

Use specific exception types and provide meaningful messages:

```csharp
// ✅ Good
public async Task<Note> GetNoteByIdAsync(int id)
{
    if (id <= 0)
        throw new ArgumentException("Note ID must be positive", nameof(id));
        
    var note = await _repository.GetByIdAsync(id);
    if (note == null)
        throw new NotFoundException($"Note with ID {id} not found");
        
    return note;
}

// ❌ Bad
public async Task<Note> GetNoteByIdAsync(int id)
{
    var note = await _repository.GetByIdAsync(id);
    if (note == null)
        throw new Exception("Error");
        
    return note;
}
```

### Logging

```csharp
// ✅ Good - Structured logging with context
_logger.LogInformation("Creating note with name: {NoteName} for user: {UserEmail}", 
    note.Name, userEmail);

_logger.LogError(ex, "Failed to create note with name: {NoteName}", note.Name);

// ❌ Bad - String concatenation
_logger.LogInformation("Creating note " + note.Name);
_logger.LogError("Error: " + ex.Message);
```

---

## Documentation Standards

### XML Documentation Guidelines

- **DO use `<summary>` tags for:**
  - Complex classes with business logic
  - Public API methods with non-obvious behavior
  - Methods with multiple parameters or complex return types
  - Controllers and their action methods

- **DON'T use `<summary>` tags for:**
  - Simple properties (getters/setters)
  - DTOs with self-explanatory property names
  - Basic CRUD operations with obvious functionality
  - Private methods with clear names

### Examples

```csharp
// ✅ Good - Complex method with business logic
/// <summary>
/// Processes payment with fraud detection and external gateway integration
/// </summary>
/// <param name="request">Payment details including amount and method</param>
/// <returns>Payment result with transaction ID and status</returns>
/// <exception cref="PaymentException">Thrown when payment processing fails</exception>
public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)

// ✅ Good - Complex API controller method
/// <summary>
/// Retrieves notes with advanced filtering, pagination, and user-specific access control
/// </summary>
/// <param name="request">Search criteria including filters and pagination options</param>
/// <returns>Paginated list of notes matching the search criteria</returns>
[HttpPost("search")]
public async Task<IActionResult> SearchNotes([FromBody] SearchNotesRequest request)

// ✅ Good - Simple properties (no documentation needed)
public string Email { get; set; }
public int NoteId { get; set; }
public DateTime CreatedAt { get; set; }

// ✅ Good - Simple CRUD operations (no documentation needed)
public async Task<Note> GetNoteByIdAsync(int id)
public async Task<List<Note>> GetAllNotesAsync()
public async Task DeleteNoteAsync(int id)

// ✅ Good - Simple DTOs (no class-level documentation needed)
public class CreateNoteRequest
{
    public string Name { get; set; }
    public string? Description { get; set; }
    public string? Tags { get; set; }
}

// ✅ Good - Simple domain model (no property documentation needed)
public class Note
{
    public int NoteId { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}
```

### Repository Example Following Correct Standards

```csharp
using Microsoft.Extensions.Logging;
using SuperApp.Application.Interfaces;
using SuperApp.Domain.Entities;
using SuperApp.Infrastructure.Data;

namespace SuperApp.Infrastructure.Repositories;

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
    /// Retrieves notes with complex filtering logic including user permissions and advanced search
    /// </summary>
    /// <param name="getAll">If true, retrieves all notes; otherwise, user-specific notes</param>
    /// <param name="searchText">Optional search term for filtering by name or description</param>
    /// <returns>List of notes matching the criteria with applied security filters</returns>
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

## Database Access

### Repository Pattern

All repositories must inherit from `BaseRepository`:

```csharp
public class NoteRepository : BaseRepository, INoteRepository
{
    public NoteRepository(IConnectionFactory connectionFactory, ILogger<NoteRepository> logger)
        : base(connectionFactory, logger)
    {
    }
}
```

### Stored Procedures

Always use stored procedures for database operations:

```csharp
// ✅ Good
return await ExecuteStoredProcedure(
    StoredProcedures.SelectNotes,
    addParameters: (cmd) =>
    {
        cmd.Parameters.Add(new SqlParameter("@iv_UserId", userId));
        AddParameterIfNotNull(cmd, "@iv_SearchText", searchText);
    },
    mapResult: MapToList<Note>
);

// ❌ Bad - Raw SQL
var sql = $"SELECT * FROM Notes WHERE UserId = {userId}";
```

### Parameter Safety

```csharp
// ✅ Good - Parameterized
AddParameterIfNotNull(cmd, "@iv_SearchText", searchText);

// ❌ Bad - SQL Injection risk
var sql = $"SELECT * FROM Notes WHERE Name LIKE '%{searchText}%'";
```

---

## Testing Standards

### Test Naming

```csharp
// ✅ Good - Method_Scenario_ExpectedResult
[Test]
public async Task GetNoteByIdAsync_WithValidId_ReturnsNote()

[Test]
public async Task GetNoteByIdAsync_WithInvalidId_ThrowsNotFoundException()

[Test]
public async Task CreateNoteAsync_WithNullName_ThrowsValidationException()
```

### Test Structure

```csharp
[Test]
public async Task CreateNoteAsync_WithValidNote_ReturnsCreatedNote()
{
    // Arrange
    var note = new Note { Name = "Test Note", Description = "Test Description" };
    var mockRepository = new Mock<INoteRepository>();
    mockRepository.Setup(r => r.CreateAsync(It.IsAny<Note>()))
              .ReturnsAsync(note);
    
    var service = new NoteService(mockRepository.Object);
    
    // Act
    var result = await service.CreateNoteAsync(note);
    
    // Assert
    Assert.That(result, Is.Not.Null);
    Assert.That(result.Name, Is.EqualTo("Test Note"));
    mockRepository.Verify(r => r.CreateAsync(It.IsAny<Note>()), Times.Once);
}
```

---

## Performance Guidelines

### Async Best Practices

```csharp
// ✅ Good
public async Task<List<Note>> GetNotesAsync()
{
    return await _repository.GetAllAsync();
}

// ❌ Bad - Blocking async calls
public List<Note> GetNotes()
{
    return _repository.GetAllAsync().Result;
}
```

### Memory Management

```csharp
// ✅ Good - Using statement for IDisposable
public async Task ProcessFileAsync(string filePath)
{
    using var fileStream = new FileStream(filePath, FileMode.Open);
    await ProcessStreamAsync(fileStream);
}

// ✅ Good - StringBuilder for multiple concatenations
var sb = new StringBuilder();
foreach (var item in items)
{
    sb.AppendLine(item.ToString());
}
return sb.ToString();
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

[← Back to Main Documentation](../.github/copilot-instructions.md) | [Next: Architecture Guide →](ARCHITECTURE.md)