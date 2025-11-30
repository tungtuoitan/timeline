# Error Handling Guide

## Exception Hierarchy

**Base Exception** (SuperAppAPI/Exceptions/AppException.cs):
```csharp
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
}
```

## Custom Exceptions

### NotFoundException (404)
```csharp
throw new NotFoundException("Note not found");
throw new NotFoundException("Note", noteId);
```

### ValidationException (400)
```csharp
var errors = new Dictionary<string, string[]>
{
    { "Title", new[] { "Title is required" } }
};
throw new ValidationException(errors);
```

### UnauthorizedException (401)
```csharp
throw new UnauthorizedException("Invalid credentials");
```

### ForbiddenException (403)
```csharp
throw new ForbiddenException("Not authorized to access this resource");
```

### BusinessRuleException (422)
```csharp
throw new BusinessRuleException("Cannot exceed note limit");
```

### ConflictException (409)
```csharp
throw new ConflictException("Email already exists");
```

## Global Exception Middleware

**Implementation** (SuperAppAPI/Middlewares/GlobalExceptionMiddleware.cs):

```csharp
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var statusCode = ex switch
        {
            NotFoundException => StatusCodes.Status404NotFound,
            ValidationException => StatusCodes.Status400BadRequest,
            UnauthorizedException => StatusCodes.Status401Unauthorized,
            ForbiddenException => StatusCodes.Status403Forbidden,
            BusinessRuleException => StatusCodes.Status422UnprocessableEntity,
            ConflictException => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = GetTitle(statusCode),
            Detail = ex.Message,
            Instance = context.Request.Path
        };

        if (ex is ValidationException validationEx)
        {
            problemDetails.Extensions["errors"] = validationEx.Errors;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(problemDetails);
    }
}
```

**Registration** (Program.cs):
```csharp
app.UseMiddleware<GlobalExceptionMiddleware>();
```

## Controller Error Handling

**Don't catch exceptions in controllers** - Let middleware handle:

```csharp
// ✅ Good - Let middleware handle exceptions
[HttpGet("{id}")]
public async Task<IActionResult> GetNote(int id)
{
    var note = await _noteService.GetNoteByIdAsync(id);
    return Ok(note);
}

// ❌ Bad - Don't catch in controller
[HttpGet("{id}")]
public async Task<IActionResult> GetNote(int id)
{
    try
    {
        var note = await _noteService.GetNoteByIdAsync(id);
        return Ok(note);
    }
    catch (Exception ex)
    {
        return StatusCode(500, ex.Message);
    }
}
```

## Repository Error Handling

**Database Errors:**
```csharp
try
{
    await _context.SaveChangesAsync();
}
catch (DbUpdateConcurrencyException ex)
{
    _logger.LogError(ex, "Concurrency conflict for Note {NoteId}", note.NoteId);
    throw new ConflictException("Note was modified by another user");
}
catch (DbUpdateException ex)
{
    _logger.LogError(ex, "Database error saving Note");
    throw new DataAccessException("Failed to save note");
}
```

## Logging

**Structured Logging:**
```csharp
_logger.LogError(ex, "Failed to create note for user {UserId}", userId);
_logger.LogWarning("User {UserId} attempted to access note {NoteId}", userId, noteId);
_logger.LogInformation("Note {NoteId} created successfully", noteId);
```

**Log Levels:**
- **Trace**: Detailed debug info
- **Debug**: Development diagnostics
- **Information**: Normal flow
- **Warning**: Handled issues
- **Error**: Exceptions
- **Critical**: System failures

**Don't log:**
- Passwords
- JWT tokens
- API keys
- Personal sensitive data

## HTTP Status Codes

| Code | Name | Use |
|------|------|-----|
| 200 | OK | Successful GET/PUT/PATCH |
| 201 | Created | Successful POST |
| 204 | No Content | Successful DELETE |
| 400 | Bad Request | Validation errors |
| 401 | Unauthorized | Authentication failed |
| 403 | Forbidden | Not authorized |
| 404 | Not Found | Resource missing |
| 409 | Conflict | Duplicate/conflict |
| 422 | Unprocessable | Business rule violation |
| 500 | Internal Error | Unexpected error |

## ProblemDetails Response

**Standard format:**
```json
{
  "type": "https://httpstatuses.com/404",
  "title": "Not Found",
  "status": 404,
  "detail": "Note with id '123' not found",
  "instance": "/api/notes/123"
}
```

**Validation errors:**
```json
{
  "type": "https://httpstatuses.com/400",
  "title": "Validation Error",
  "status": 400,
  "errors": {
    "Title": ["Title is required"],
    "Content": ["Content must be less than 5000 characters"]
  }
}
```

## Best Practices

### ✅ DO
- Use specific exception types
- Log errors với structured logging
- Return consistent error format
- Hide sensitive details in production
- Use ProblemDetails standard
- Include correlation IDs
- Log with context (userId, noteId, etc.)

### ❌ DON'T
- Catch and swallow exceptions
- Return stack traces in production
- Use generic Exception catches
- Log sensitive data
- Return different formats for different errors
- Expose internal implementation details

---

**Version:** 2.0
**Last Updated:** November 2025
**See Also:** API_DESIGN.md, AUTHENTICATION.md
