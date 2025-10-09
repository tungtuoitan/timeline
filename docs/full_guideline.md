# SuperApp Backend Development Guidelines

## Table of Contents
1. [Project Overview](#project-overview)
2. [Architecture](#architecture)
3. [Folder Structure](#folder-structure)
4. [Coding Standards](#coding-standards)
5. [Database Access Patterns](#database-access-patterns)
6. [Error Handling](#error-handling)
7. [Authentication & Authorization](#authentication--authorization)
8. [API Design](#api-design)
9. [Testing Guidelines](#testing-guidelines)
10. [Security Best Practices](#security-best-practices)

---

## Project Overview

**Technology Stack:**
- .NET 8 Web API
- ADO.NET with Stored Procedures
- JWT Authentication + Google OAuth
- Serilog for Logging
- MediatR for CQRS Pattern
- AutoMapper for Object Mapping
- FluentValidation for Input Validation

**Architecture Pattern:** Clean Architecture with CQRS

---

## Architecture

### Layer Responsibilities

```
┌─────────────────────────────────────┐
│   SuperApp.API (Presentation)       │  ← Controllers, Middleware, Filters
├─────────────────────────────────────┤
│   SuperApp.Application (Business)   │  ← Commands, Queries, DTOs, Validators
├─────────────────────────────────────┤
│   SuperApp.Domain (Core)            │  ← Entities, Value Objects, Enums
├─────────────────────────────────────┤
│   SuperApp.Infrastructure (Data)    │  ← Repositories, Database Access
├─────────────────────────────────────┤
│   SuperApp.Shared (Utilities)       │  ← Constants, Helpers, Results
└─────────────────────────────────────┘
```

**Dependency Rules:**
- API → Application → Domain
- Infrastructure → Domain
- Domain has NO dependencies on other layers
- Application can reference Infrastructure via interfaces only

---

## Folder Structure

```
SuperApp/
│
├── SuperApp.API/
│   ├── Controllers/
│   │   ├── NotesController.cs
│   │   ├── AuthController.cs
│   │   └── UserProfileController.cs
│   ├── Middlewares/
│   │   ├── GlobalExceptionMiddleware.cs
│   │   └── ValidateTokenMiddleware.cs
│   ├── Filters/
│   │   └── ValidationFilter.cs
│   ├── Extensions/
│   │   └── ServiceCollectionExtensions.cs
│   ├── Program.cs
│   └── Startup.cs
│
├── SuperApp.Application/
│   ├── Common/
│   │   ├── Interfaces/
│   │   │   ├── IRepository.cs
│   │   │   └── IConnectionFactory.cs
│   │   ├── Behaviors/
│   │   │   ├── ValidationBehavior.cs
│   │   │   └── LoggingBehavior.cs
│   │   ├── Exceptions/
│   │   │   ├── AppException.cs
│   │   │   ├── NotFoundException.cs
│   │   │   ├── ValidationException.cs
│   │   │   └── UnauthorizedException.cs
│   │   └── Mappings/
│   │       └── MappingProfile.cs
│   ├── Features/
│   │   ├── Notes/
│   │   │   ├── Commands/
│   │   │   │   └── CreateNote/
│   │   │   │       ├── CreateNoteCommand.cs
│   │   │   │       ├── CreateNoteCommandHandler.cs
│   │   │   │       └── CreateNoteValidator.cs
│   │   │   └── Queries/
│   │   │       └── GetNotes/
│   │   │           ├── GetNotesQuery.cs
│   │   │           └── GetNotesQueryHandler.cs
│   │   ├── Auth/
│   │   ├── UserProfile/
│   │   └── StandardRegistry/
│   └── DTOs/
│       ├── Requests/
│       │   ├── CreateNoteRequest.cs
│       │   └── LoginRequest.cs
│       └── Responses/
│           ├── NoteDto.cs
│           └── LoginResponse.cs
│
├── SuperApp.Domain/
│   ├── Entities/
│   │   ├── Note.cs
│   │   ├── User.cs
│   │   └── UserProfile.cs
│   ├── Enums/
│   │   └── AuthType.cs
│   └── ValueObjects/
│
├── SuperApp.Infrastructure/
│   ├── Data/
│   │   ├── IConnectionFactory.cs
│   │   └── ConnectionFactory.cs
│   ├── Repositories/
│   │   ├── BaseRepository.cs
│   │   ├── NoteRepository.cs
│   │   ├── AuthRepository.cs
│   │   ├── UserProfileRepository.cs
│   │   └── StandardRegistryRepository.cs
│   ├── StoredProcedures/
│   │   └── StoredProcedures.cs
│   └── Extensions/
│       ├── DbDataReaderMapper.cs
│       └── DataTableExtensions.cs
│
├── SuperApp.Shared/
│   ├── Constants/
│   │   ├── AppConstants.cs
│   │   └── ErrorMessages.cs
│   ├── Helpers/
│   │   └── PasswordHelper.cs
│   └── Results/
│       └── Result.cs
│
└── SuperApp.Tests/
    ├── Unit/
    │   ├── Application/
    │   └── Domain/
    └── Integration/
        └── Infrastructure/
```

---

## Coding Standards

### Naming Conventions

```csharp
// Classes: PascalCase
public class NoteRepository { }

// Interfaces: I + PascalCase
public interface INoteRepository { }

// Methods: PascalCase
public async Task<Note> GetNoteByIdAsync(int id) { }

// Private fields: _camelCase
private readonly ILogger _logger;

// Parameters: camelCase
public void ProcessNote(Note note, string userName) { }

// Constants: PascalCase
public const int MaxRetryAttempts = 3;

// Local variables: camelCase
var noteList = new List<Note>();
```

### File Naming
- One class per file
- File name matches class name exactly
- Use descriptive names: `CreateNoteCommandHandler.cs` not `Handler1.cs`

### Async/Await
- All repository methods must be async
- Suffix async methods with `Async`: `GetNotesAsync()`
- Always use `ConfigureAwait(false)` in library code (not needed in ASP.NET Core)

```csharp
// ✅ Good
public async Task<List<Note>> GetNotesAsync(bool getAll)
{
    return await _repository.GetNotesAsync(getAll);
}

// ❌ Bad
public Task<List<Note>> GetNotes(bool getAll)
{
    return _repository.GetNotes(getAll);
}
```

### Code Comments
- Use XML documentation for public APIs
- Avoid obvious comments
- Comment WHY, not WHAT

```csharp
/// <summary>
/// Retrieves notes filtered by user permissions and search criteria
/// </summary>
/// <param name="getAll">If true, bypasses user-specific filtering</param>
/// <param name="searchText">Optional search term for note name/description</param>
/// <returns>List of notes matching the criteria</returns>
public async Task<List<Note>> GetNotesAsync(bool getAll, string? searchText)
{
    // Using stored procedure for performance on large datasets
    return await ExecuteStoredProcedure(...);
}
```

---

## Database Access Patterns

### Using BaseRepository

All repositories MUST inherit from `BaseRepository`:

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
}
```

### Stored Procedure Naming Convention

```csharp
public static class StoredProcedures
{
    // Format: [dbo].[usp_{operation}_{entity}]
    public const string SelectNotes = "[dbo].[usp_s_Notes]";
    public const string InsertNote = "[dbo].[usp_i_Note]";
    public const string UpdateNote = "[dbo].[usp_u_Note]";
    public const string DeleteNote = "[dbo].[usp_d_Note]";
}
```

### Handling Nullable Parameters

```csharp
// ✅ Good - Use helper method
AddParameterIfNotNull(command, "@iv_SearchText", searchText);

// ❌ Bad - Manual null checking
if (!string.IsNullOrEmpty(searchText))
    command.Parameters.Add(new SqlParameter("@iv_SearchText", searchText));
else
    command.Parameters.Add(new SqlParameter("@iv_SearchText", DBNull.Value));
```

### Connection Management

**Never create connections directly.** Always use `IConnectionFactory`:

```csharp
// ✅ Good
private readonly IConnectionFactory _connectionFactory;

using var conn = await _connectionFactory.CreateSuperAppConnectionAsync();

// ❌ Bad
var conn = new SqlConnection("hardcoded string");
await conn.OpenAsync();
```

### Command Timeout

Use constants for timeouts:

```csharp
public static class DatabaseConstants
{
    public const int DefaultCommandTimeout = 1200; // 20 minutes
    public const int LongRunningCommandTimeout = 3600; // 1 hour
}
```

---

## Error Handling

### Exception Types

Use custom exceptions that inherit from `AppException`:

```csharp
// Not found scenarios (404)
throw new NotFoundException($"Note with ID {id} not found");

// Validation failures (400)
throw new ValidationException(validationErrors);

// Unauthorized access (401)
throw new UnauthorizedException("Invalid credentials");

// Forbidden access (403)
throw new ForbiddenException("You don't have permission to access this resource");
```

### Repository Error Handling

**Never use empty try-catch blocks:**

```csharp
// ✅ Good - Let BaseRepository handle SQL exceptions
public async Task<Note> GetNoteByIdAsync(int id)
{
    var notes = await ExecuteStoredProcedure(...);
    return notes.FirstOrDefault() ?? throw new NotFoundException($"Note {id} not found");
}

// ❌ Bad - Useless try-catch
try
{
    // logic
}
catch
{
    throw; // Adds no value!
}
```

### Logging Errors

```csharp
// ✅ Good - Structured logging
_logger.LogError(ex, 
    "Failed to create note for user {UserId} with name {NoteName}", 
    userId, note.Name);

// ❌ Bad - String interpolation
_logger.LogError($"Failed to create note: {ex.Message}");
```

### Controller Error Responses

Don't handle errors in controllers - let `GlobalExceptionMiddleware` handle them:

```csharp
// ✅ Good - Throw exceptions, let middleware handle
[HttpGet("{id}")]
public async Task<ActionResult<NoteDto>> GetNote(int id)
{
    var query = new GetNoteByIdQuery(id);
    var result = await _mediator.Send(query);
    return Ok(result); // Will be 404 if not found (thrown by handler)
}

// ❌ Bad - Manual error handling
[HttpGet("{id}")]
public async Task<ActionResult<NoteDto>> GetNote(int id)
{
    try
    {
        var result = await _mediator.Send(new GetNoteByIdQuery(id));
        return Ok(result);
    }
    catch (Exception ex)
    {
        return StatusCode(500, ex.Message); // Don't do this!
    }
}
```

---

## Authentication & Authorization

### JWT Token Configuration

Tokens include these claims:
- `sub`: User identifier (email or phone)
- `jti`: Unique token ID (GUID)
- `exp`: Expiration timestamp

**Token lifetime:** 60 minutes (configured in appsettings.json)

### Protecting Endpoints

```csharp
// Require authentication for entire controller
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class NotesController : ControllerBase
{
    // All methods require valid JWT token
}

// Allow anonymous access to specific endpoints
[AllowAnonymous]
[HttpPost("login")]
public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
{
    // Public endpoint
}
```

### Role-Based Authorization (Future)

```csharp
// Require specific role
[Authorize(Roles = "Admin")]
[HttpDelete("{id}")]
public async Task<IActionResult> DeleteNote(int id)
{
    // Only admins can delete
}

// Require policy
[Authorize(Policy = "RequireNoteOwnership")]
[HttpPut("{id}")]
public async Task<IActionResult> UpdateNote(int id, UpdateNoteRequest request)
{
    // Custom policy checks ownership
}
```

### Password Security

**Always use the helper methods:**

```csharp
// ✅ Hash new password (creates random salt automatically)
string hashedPassword = PasswordHelper.HashPassword(plainPassword);

// ✅ Verify password during login
bool isValid = PasswordHelper.VerifyPassword(plainPassword, hashedPassword);

// ❌ Never compare hashes directly
if (HashPassword(input) == storedHash) // This will ALWAYS fail!
```

---

## API Design

### RESTful Endpoints

Follow REST conventions:

| HTTP Method | Endpoint | Description |
|-------------|----------|-------------|
| GET | `/api/notes` | Get all notes |
| GET | `/api/notes/{id}` | Get note by ID |
| POST | `/api/notes` | Create new note |
| PUT | `/api/notes/{id}` | Update entire note |
| PATCH | `/api/notes/{id}` | Partial update |
| DELETE | `/api/notes/{id}` | Delete note |

### Request/Response Models

**Never expose domain entities directly:**

```csharp
// ✅ Good - Use DTOs
[HttpPost]
public async Task<ActionResult<NoteDto>> CreateNote([FromBody] CreateNoteRequest request)
{
    var command = new CreateNoteCommand(request.Name, request.Description);
    var result = await _mediator.Send(command);
    return CreatedAtAction(nameof(GetNote), new { id = result.Id }, result);
}

// ❌ Bad - Exposing domain entity
[HttpPost]
public async Task<ActionResult<Note>> CreateNote([FromBody] Note note)
{
    // Clients can set internal properties, see sensitive data
}
```

### HTTP Status Codes

Use appropriate status codes:

```csharp
// 200 OK - Successful GET, PUT, PATCH
return Ok(result);

// 201 Created - Successful POST
return CreatedAtAction(nameof(GetNote), new { id = result.Id }, result);

// 204 No Content - Successful DELETE
return NoContent();

// 400 Bad Request - Validation failed
// Thrown automatically by ValidationBehavior

// 401 Unauthorized - Missing/invalid token
// Handled by JWT middleware

// 404 Not Found - Resource doesn't exist
throw new NotFoundException($"Note {id} not found");

// 500 Internal Server Error - Unexpected error
// Handled by GlobalExceptionMiddleware
```

### API Versioning (Future)

```csharp
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class NotesController : ControllerBase
{
    // Version 1.0 endpoints
}
```

---

## Testing Guidelines

### Unit Tests

Test business logic in handlers:

```csharp
public class CreateNoteCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidRequest_CreatesNote()
    {
        // Arrange
        var mockRepo = new Mock<INoteRepository>();
        var handler = new CreateNoteCommandHandler(mockRepo.Object, _mapper);
        var command = new CreateNoteCommand("Test Note", "Description");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        mockRepo.Verify(r => r.CreateNoteAsync(It.IsAny<Note>()), Times.Once);
    }
}
```

### Integration Tests

Test repository against real database:

```csharp
public class NoteRepositoryTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    [Fact]
    public async Task GetNotesAsync_ReturnsAllNotes()
    {
        // Arrange
        var repository = _fixture.CreateRepository<NoteRepository>();

        // Act
        var notes = await repository.GetNotesAsync(getAll: true, searchText: null);

        // Assert
        Assert.NotEmpty(notes);
    }
}
```

### Test Naming Convention

```
MethodName_Scenario_ExpectedResult

Examples:
- GetNoteById_ExistingId_ReturnsNote
- CreateNote_InvalidData_ThrowsValidationException
- DeleteNote_NonExistentId_ThrowsNotFoundException
```

---

## Security Best Practices

### Configuration Management

**Never commit secrets to source control:**

```jsonc
// ✅ User Secrets (secrets.json) - Not in Git
{
  "ConnectionStrings": {
    "SuperAppConnection": "Server=...;Password=..."
  },
  "OAuth": {
    "ClientSecret": "actual-secret-here"
  },
  "Jwt": {
    "Key": "production-key-here"
  }
}

// ✅ appsettings.json - Safe to commit
{
  "Logging": {
    "LogLevel": { "Default": "Information" }
  },
  "OAuth": {
    "ClientId": "887853390661-xxx.apps.googleusercontent.com",
    "RedirectUri": "https://app.example.com/callback"
  }
}
```

### Input Validation

Always validate inputs using FluentValidation:

```csharp
public class CreateNoteValidator : AbstractValidator<CreateNoteCommand>
{
    public CreateNoteValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(200).WithMessage("Name cannot exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(5000).WithMessage("Description cannot exceed 5000 characters");
    }
}
```

### SQL Injection Prevention

**Always use parameterized queries (enforced by BaseRepository):**

```csharp
// ✅ Good - Parameters prevent SQL injection
command.Parameters.Add(new SqlParameter("@iv_SearchText", searchText));

// ❌ Bad - Never concatenate SQL strings
command.CommandText = $"SELECT * FROM Notes WHERE Name = '{searchText}'";
```

### Sensitive Data

**Never log sensitive information:**

```csharp
// ✅ Good
_logger.LogInformation("User {UserId} logged in", userId);

// ❌ Bad
_logger.LogInformation("User logged in with password: {Password}", password);
```

### CORS Configuration

Restrict allowed origins in production:

```csharp
services.AddCors(options =>
{
    options.AddPolicy("Production", builder =>
    {
        builder.WithOrigins("https://app.example.com")
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
    });
});
```

---

## Quick Reference

### Creating a New Feature

1. **Create Command/Query** in `Application/Features/{Feature}/Commands` or `Queries`
2. **Create Handler** in the same folder
3. **Create Validator** using FluentValidation
4. **Create DTOs** in `Application/DTOs/Requests` and `Responses`
5. **Update Repository** interface and implementation if needed
6. **Add Controller Endpoint** in `API/Controllers`
7. **Write Tests** in `Tests/Unit` and `Tests/Integration`

### Common Commands

```bash
# Run application
dotnet run --project SuperApp.API

# Run tests
dotnet test

# Add migration (if using EF in future)
dotnet ef migrations add MigrationName

# Manage user secrets
dotnet user-secrets set "ConnectionStrings:SuperAppConnection" "your-connection-string"
```

### Code Review Checklist

- [ ] No hardcoded connection strings or secrets
- [ ] All async methods suffixed with `Async`
- [ ] Proper exception types used
- [ ] DTOs used instead of domain entities in API
- [ ] Input validation implemented
- [ ] Appropriate HTTP status codes returned
- [ ] Repository inherits from `BaseRepository`
- [ ] No empty try-catch blocks
- [ ] Structured logging used
- [ ] XML documentation for public APIs
- [ ] Unit tests written
- [ ] No SQL concatenation (SQL injection risk)

---

## Getting Help

- Check existing code examples in similar features
- Review this guideline document
- Ask team lead for architecture decisions
- Consult Microsoft documentation for .NET best practices

---

**Document Version:** 1.0  
**Last Updated:** October 2025  
**Maintained By:** Development Team