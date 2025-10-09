# Error Handling Guide

[← Back to Main Documentation](../.copilot-instructions.md.md)

---

## Table of Contents
1. [Exception Hierarchy](#exception-hierarchy)
2. [Global Exception Middleware](#global-exception-middleware)
3. [Custom Exceptions](#custom-exceptions)
4. [Controller Error Handling](#controller-error-handling)
5. [Repository Error Handling](#repository-error-handling)
6. [Logging Errors](#logging-errors)
7. [HTTP Status Codes](#http-status-codes)
8. [ProblemDetails Standard](#problemdetails-standard)

---****

## Exception Hierarchy

### Base Exception Class

```csharp
// SuperApp.Application/Common/Exceptions/AppException.cs
public abstract class AppException : Exception
{
    public int StatusCode { get; }
    public string? ErrorCode { get; }

    protected AppException(string message, int statusCode, string? errorCode = null) 
        : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }

    protected AppException(string message, int statusCode, Exception innerException, string? errorCode = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }
}
```

---

## Custom Exceptions

### NotFoundException (404)

```csharp
// SuperApp.Application/Common/Exceptions/NotFoundException.cs
public class NotFoundException : AppException
{
    public NotFoundException(string message) 
        : base(message, StatusCodes.Status404NotFound, "NOT_FOUND")
    {
    }

    public NotFoundException(string entityName, object key)
        : base($"{entityName} with key '{key}' was not found", StatusCodes.Status404NotFound, "NOT_FOUND")
    {
    }
}

// Usage examples:
throw new NotFoundException("Note", noteId);
throw new NotFoundException($"Note with ID {noteId} not found");
```

### ValidationException (400)

```csharp
// SuperApp.Application/Common/Exceptions/ValidationException.cs
public class ValidationException : AppException
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation errors occurred", StatusCodes.Status400BadRequest, "VALIDATION_ERROR")
    {
        Errors = errors;
    }

    public ValidationException(string propertyName, string errorMessage)
        : base("Validation error", StatusCodes.Status400BadRequest, "VALIDATION_ERROR")
    {
        Errors = new Dictionary<string, string[]>
        {
            { propertyName, new[] { errorMessage } }
        };
    }
}

// Usage examples:
throw new ValidationException("Name", "Note name is required");

var errors = new Dictionary<string, string[]>
{
    { "Name", new[] { "Name is required", "Name must be unique" } },
    { "Description", new[] { "Description too long" } }
};
throw new ValidationException(errors);
```

### UnauthorizedException (401)

```csharp
// SuperApp.Application/Common/Exceptions/UnauthorizedException.cs
public class UnauthorizedException : AppException
{
    public UnauthorizedException(string message = "Unauthorized access")
        : base(message, StatusCodes.Status401Unauthorized, "UNAUTHORIZED")
    {
    }
}

// Usage examples:
throw new UnauthorizedException();
throw new UnauthorizedException("Invalid credentials");
throw new UnauthorizedException("Token has expired");
```

### ForbiddenException (403)

```csharp
// SuperApp.Application/Common/Exceptions/ForbiddenException.cs
public class ForbiddenException : AppException
{
    public ForbiddenException(string message = "Access forbidden")
        : base(message, StatusCodes.Status403Forbidden, "FORBIDDEN")
    {
    }
}

// Usage examples:
throw new ForbiddenException();
throw new ForbiddenException("You don't have permission to delete this note");
```

### BusinessRuleException (422)

```csharp
// SuperApp.Application/Common/Exceptions/BusinessRuleException.cs
public class BusinessRuleException : AppException
{
    public BusinessRuleException(string message, string? errorCode = null)
        : base(message, StatusCodes.Status422UnprocessableEntity, errorCode ?? "BUSINESS_RULE_VIOLATION")
    {
    }
}

// Usage examples:
throw new BusinessRuleException("Cannot delete note that has active references");
throw new BusinessRuleException("User has exceeded maximum note limit", "NOTE_LIMIT_EXCEEDED");
```

### ConflictException (409)

```csharp
// SuperApp.Application/Common/Exceptions/ConflictException.cs
public class ConflictException : AppException
{
    public ConflictException(string message)
        : base(message, StatusCodes.Status409Conflict, "CONFLICT")
    {
    }
}

// Usage examples:
throw new ConflictException("A note with this name already exists");
throw new ConflictException("Email address is already registered");
```

---

## Global Exception Middleware

### Implementation

```csharp
// SuperApp.API/Middlewares/GlobalExceptionMiddleware.cs
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, problemDetails) = exception switch
        {
            AppException appEx => CreateAppExceptionResponse(appEx),
            ValidationException validEx => CreateValidationExceptionResponse(validEx),
            SqlException sqlEx => CreateSqlExceptionResponse(sqlEx),
            UnauthorizedAccessException => CreateUnauthorizedResponse(),
            _ => CreateInternalServerErrorResponse(exception)
        };

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(problemDetails);
    }

    private (int, ProblemDetails) CreateAppExceptionResponse(AppException exception)
    {
        var problemDetails = new ProblemDetails
        {
            Status = exception.StatusCode,
            Title = GetTitleForStatusCode(exception.StatusCode),
            Detail = exception.Message,
            Type = $"https://httpstatuses.com/{exception.StatusCode}",
            Instance = GetRequestPath()
        };

        if (!string.IsNullOrEmpty(exception.ErrorCode))
        {
            problemDetails.Extensions["errorCode"] = exception.ErrorCode;
        }

        if (exception is ValidationException validationEx)
        {
            problemDetails.Extensions["errors"] = validationEx.Errors;
        }

        return (exception.StatusCode, problemDetails);
    }

    private (int, ProblemDetails) CreateValidationExceptionResponse(ValidationException exception)
    {
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation Error",
            Detail = "One or more validation errors occurred",
            Type = "https://httpstatuses.com/400",
            Instance = GetRequestPath()
        };

        problemDetails.Extensions["errors"] = exception.Errors;
        problemDetails.Extensions["errorCode"] = "VALIDATION_ERROR";

        return (StatusCodes.Status400BadRequest, problemDetails);
    }

    private (int, ProblemDetails) CreateSqlExceptionResponse(SqlException exception)
    {
        _logger.LogError(exception, "Database error occurred");

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Database Error",
            Detail = _env.IsDevelopment() 
                ? exception.Message 
                : "An error occurred while processing your request",
            Type = "https://httpstatuses.com/500",
            Instance = GetRequestPath()
        };

        if (_env.IsDevelopment())
        {
            problemDetails.Extensions["sqlErrorNumber"] = exception.Number;
            problemDetails.Extensions["sqlErrorMessage"] = exception.Message;
        }

        return (StatusCodes.Status500InternalServerError, problemDetails);
    }

    private (int, ProblemDetails) CreateUnauthorizedResponse()
    {
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Unauthorized",
            Detail = "Authentication is required to access this resource",
            Type = "https://httpstatuses.com/401",
            Instance = GetRequestPath()
        };

        return (StatusCodes.Status401Unauthorized, problemDetails);
    }

    private (int, ProblemDetails) CreateInternalServerErrorResponse(Exception exception)
    {
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Internal Server Error",
            Detail = _env.IsDevelopment() 
                ? exception.Message 
                : "An unexpected error occurred",
            Type = "https://httpstatuses.com/500",
            Instance = GetRequestPath()
        };

        if (_env.IsDevelopment())
        {
            problemDetails.Extensions["stackTrace"] = exception.StackTrace;
            problemDetails.Extensions["exceptionType"] = exception.GetType().Name;
        }

        return (StatusCodes.Status500InternalServerError, problemDetails);
    }

    private string GetTitleForStatusCode(int statusCode) => statusCode switch
    {
        400 => "Bad Request",
        401 => "Unauthorized",
        403 => "Forbidden",
        404 => "Not Found",
        409 => "Conflict",
        422 => "Unprocessable Entity",
        500 => "Internal Server Error",
        _ => "Error"
    };

    private string GetRequestPath() => 
        $"{_httpContext?.Request.Method} {_httpContext?.Request.Path}";
}
```

### Registration

```csharp
// Startup.cs or Program.cs
app.UseMiddleware<GlobalExceptionMiddleware>();

// Or create extension method:
public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionMiddleware>();
    }
}

// Usage:
app.UseGlobalExceptionHandler();
```

---

## Controller Error Handling

### ✅ Correct Pattern (Let Middleware Handle)

```csharp
[ApiController]
[Route("api/[controller]")]
public class NotesController : ControllerBase
{
    private readonly IMediator _mediator;

    public NotesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(NoteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NoteDto>> GetNote(int id)
    {
        // Just call handler - exceptions will be caught by middleware
        var query = new GetNoteByIdQuery(id);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(NoteDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<NoteDto>> CreateNote([FromBody] CreateNoteRequest request)
    {
        var command = new CreateNoteCommand(request.Name, request.Description);
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetNote), new { id = result.NoteId }, result);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteNote(int id)
    {
        var command = new DeleteNoteCommand(id);
        await _mediator.Send(command);
        return NoContent();
    }
}
```

### ❌ Incorrect Pattern (Don't Do This)

```csharp
// ❌ Bad - Manual error handling in controller
[HttpGet("{id}")]
public async Task<ActionResult<NoteDto>> GetNote(int id)
{
    try
    {
        var query = new GetNoteByIdQuery(id);
        var result = await _mediator.Send(query);
        return Ok(result);
    }
    catch (NotFoundException ex)
    {
        return NotFound(ex.Message); // Don't do this!
    }
    catch (Exception ex)
    {
        return StatusCode(500, ex.Message); // Don't do this!
    }
}

// ❌ Bad - Returning entire exception object
[HttpPost]
public async Task<ActionResult> CreateNote([FromBody] CreateNoteRequest request)
{
    try
    {
        var command = new CreateNoteCommand(request.Name, request.Description);
        await _mediator.Send(command);
        return Ok();
    }
    catch (Exception ex)
    {
        return Unauthorized(ex); // Exposes sensitive info!
    }
}
```

---

## Repository Error Handling

### ✅ Correct Pattern

```csharp
public class NoteRepository : BaseRepository, INoteRepository
{
    public NoteRepository(
        IConnectionFactory connectionFactory,
        ILogger<NoteRepository> logger)
        : base(connectionFactory, logger)
    {
    }

    // ✅ Good - Let BaseRepository handle SQL exceptions
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
        // No try-catch needed - BaseRepository handles it
    }

    // ✅ Good - Check business logic errors from stored procedures
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
                    _logger.LogWarning("Business rule violation: {ErrorMessage}", errorMsg);
                    throw new BusinessRuleException(errorMsg);
                }

                return (int)cmd.Parameters["@ov_NoteId"].Value;
            }
        );
    }
}
```

### ❌ Incorrect Pattern

```csharp
// ❌ Bad - Useless try-catch that just re-throws
public async Task<Note?> GetNoteByIdAsync(int id)
{
    try
    {
        return await ExecuteStoredProcedure(...);
    }
    catch (Exception)
    {
        throw; // Adds no value!
    }
}

// ❌ Bad - Swallowing exceptions
public async Task<Note?> GetNoteByIdAsync(int id)
{
    try
    {
        return await ExecuteStoredProcedure(...);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex.Message);
        return null; // Hides the error!
    }
}

// ❌ Bad - Not checking output parameters
public async Task<int> CreateNoteAsync(Note note)
{
    return await ExecuteNonQuery(
        StoredProcedures.InsertNote,
        addParameters: async (cmd) => { /* ... */ },
        extractResult: (cmd) =>
        {
            // Ignoring @ov_ErrorMsg - could have business errors!
            return (int)cmd.Parameters["@ov_NoteId"].Value;
        }
    );
}
```

---

## Logging Errors

### Structured Logging with Serilog

```csharp
// ✅ Good - Structured logging with context
_logger.LogError(ex,
    "Failed to create note for user {UserId} with name {NoteName}",
    userId, noteName);

_logger.LogWarning(
    "Note {NoteId} not found for user {UserId}",
    noteId, userId);

_logger.LogInformation(
    "User {UserId} retrieved {NoteCount} notes",
    userId, noteCount);

// ❌ Bad - String interpolation (not structured)
_logger.LogError($"Failed to create note: {ex.Message}");
_logger.LogError("Error: " + ex.Message);
```

### Log Levels

```csharp
// Trace - Very detailed, typically only enabled in development
_logger.LogTrace("Entering method GetNoteByIdAsync with id: {NoteId}", id);

// Debug - Debugging information, less detailed than Trace
_logger.LogDebug("Retrieved {Count} notes from database", notes.Count);

// Information - General flow of the application
_logger.LogInformation("User {UserId} logged in successfully", userId);

// Warning - Unusual or unexpected events that don't stop execution
_logger.LogWarning("Note {NoteId} accessed by unauthorized user {UserId}", noteId, userId);

// Error - Errors and exceptions that can be handled
_logger.LogError(ex, "Failed to save note {NoteId}", noteId);

// Critical - Failures that require immediate attention
_logger.LogCritical(ex, "Database connection lost");
```

### What NOT to Log

```csharp
// ❌ Never log sensitive information
_logger.LogInformation("User logged in with password: {Password}", password); // NO!
_logger.LogDebug("JWT token: {Token}", jwtToken); // NO!
_logger.LogError("Credit card: {CardNumber}", cardNumber); // NO!

// ✅ Log safely
_logger.LogInformation("User {UserId} logged in successfully", userId);
_logger.LogDebug("JWT token generated for user {UserId}", userId);
_logger.LogError("Payment failed for user {UserId}", userId);
```

---

## HTTP Status Codes

### Standard Status Code Usage

| Code | Name | When to Use | Example |
|------|------|-------------|---------|
| 200 | OK | Successful GET, PUT, PATCH | Retrieved note successfully |
| 201 | Created | Successful POST | Note created |
| 204 | No Content | Successful DELETE | Note deleted |
| 400 | Bad Request | Validation failed | Invalid input data |
| 401 | Unauthorized | Authentication required/failed | Missing/invalid token |
| 403 | Forbidden | Authenticated but not authorized | User can't delete admin note |
| 404 | Not Found | Resource doesn't exist | Note ID not found |
| 409 | Conflict | Resource conflict | Duplicate note name |
| 422 | Unprocessable Entity | Business rule violation | Note limit exceeded |
| 500 | Internal Server Error | Unexpected error | Database connection failed |

### Controller Status Code Examples

```csharp
// 200 OK
[HttpGet("{id}")]
[ProducesResponseType(typeof(NoteDto), StatusCodes.Status200OK)]
public async Task<ActionResult<NoteDto>> GetNote(int id)
{
    var result = await _mediator.Send(new GetNoteByIdQuery(id));
    return Ok(result); // 200
}

// 201 Created
[HttpPost]
[ProducesResponseType(typeof(NoteDto), StatusCodes.Status201Created)]
public async Task<ActionResult<NoteDto>> CreateNote([FromBody] CreateNoteRequest request)
{
    var result = await _mediator.Send(new CreateNoteCommand(request));
    return CreatedAtAction(nameof(GetNote), new { id = result.NoteId }, result); // 201
}

// 204 No Content
[HttpDelete("{id}")]
[ProducesResponseType(StatusCodes.Status204NoContent)]
public async Task<IActionResult> DeleteNote(int id)
{
    await _mediator.Send(new DeleteNoteCommand(id));
    return NoContent(); // 204
}

// 400 Bad Request - Handled by ValidationBehavior
// 404 Not Found - Throw NotFoundException in handler
// 401 Unauthorized - Handled by JWT middleware
// 403 Forbidden - Throw ForbiddenException in handler
```

---

## ProblemDetails Standard

### Standard Response Format

```json
{
  "type": "https://httpstatuses.com/404",
  "title": "Not Found",
  "status": 404,
  "detail": "Note with ID 123 not found",
  "instance": "GET /api/notes/123",
  "errorCode": "NOT_FOUND",
  "traceId": "0HMVFE42N5V7K:00000001"
}
```

### Validation Error Response

```json
{
  "type": "https://httpstatuses.com/400",
  "title": "Validation Error",
  "status": 400,
  "detail": "One or more validation errors occurred",
  "instance": "POST /api/notes",
  "errorCode": "VALIDATION_ERROR",
  "errors": {
    "Name": [
      "Name is required",
      "Name must be unique"
    ],
    "Description": [
      "Description cannot exceed 5000 characters"
    ]
  },
  "traceId": "0HMVFE42N5V7K:00000002"
}
```

### Development vs Production

```json
// Development - Includes stack trace
{
  "type": "https://httpstatuses.com/500",
  "title": "Internal Server Error",
  "status": 500,
  "detail": "Object reference not set to an instance of an object",
  "instance": "POST /api/notes",
  "stackTrace": "at SuperApp.API.Controllers.NotesController...",
  "exceptionType": "NullReferenceException"
}

// Production - Hides implementation details
{
  "type": "https://httpstatuses.com/500",
  "title": "Internal Server Error",
  "status": 500,
  "detail": "An unexpected error occurred",
  "instance": "POST /api/notes"
}
```

---

## Quick Reference

### Exception Quick Guide

| Exception | Status | Usage |
|-----------|--------|-------|
| `NotFoundException` | 404 | Resource not found |
| `ValidationException` | 400 | Input validation failed |
| `UnauthorizedException` | 401 | Authentication failed |
| `ForbiddenException` | 403 | Not authorized |
| `BusinessRuleException` | 422 | Business rule violation |
| `ConflictException` | 409 | Resource conflict |

### Logging Quick Guide

| Level | When to Use |
|-------|-------------|
| Trace | Method entry/exit |
| Debug | Detailed flow |
| Information | Normal events |
| Warning | Unusual but handled |
| Error | Exceptions |
| Critical | System failures |

---

[← Back to Main Documentation](../.copilot-instructions.md.md) | [Next: Authentication →](AUTHENTICATION.md)