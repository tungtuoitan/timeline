# Error Handling Guide

## Table of Contents
- [Error Handling Guide](#error-handling-guide)
  - [Table of Contents](#table-of-contents)
  - [Exception Hierarchy](#exception-hierarchy)
    - [Base Exception](#base-exception)
  - [Custom Exceptions](#custom-exceptions)
    - [NotFoundException (404)](#notfoundexception-404)
    - [ValidationException (400)](#validationexception-400)
    - [UnauthorizedException (401)](#unauthorizedexception-401)
    - [ForbiddenException (403)](#forbiddenexception-403)
    - [BusinessRuleException (422)](#businessruleexception-422)
    - [ConflictException (409)](#conflictexception-409)
  - [Global Exception Middleware](#global-exception-middleware)
    - [Implementation](#implementation)
    - [Registration](#registration)
  - [Controller Error Handling](#controller-error-handling)
  - [Repository Error Handling](#repository-error-handling)
  - [Logging Errors](#logging-errors)
  - [HTTP Status Codes](#http-status-codes)
  - [ProblemDetails Standard](#problemdetails-standard)
  - [Quick Reference](#quick-reference)
    - [Exceptions](#exceptions)
    - [Logging](#logging)

## Exception Hierarchy

### Base Exception

```csharp
public abstract class AppException : Exception
{
    public int StatusCode { get; }
    public string? ErrorCode { get; }

    protected AppException(string message, int statusCode, string? errorCode = null) : base(message) { /* ... */ }
}
```

## Custom Exceptions

### NotFoundException (404)

```csharp
public class NotFoundException : AppException
{
    public NotFoundException(string message) : base(message, 404, "NOT_FOUND") { }
    public NotFoundException(string entityName, object key) : base($"{entityName} with key '{key}' not found", 404, "NOT_FOUND") { }
}
```

### ValidationException (400)

```csharp
public class ValidationException : AppException
{
    public IDictionary<string, string[]> Errors { get; }
    public ValidationException(IDictionary<string, string[]> errors) : base("Validation errors", 400, "VALIDATION_ERROR") { Errors = errors; }
}
```

### UnauthorizedException (401)

```csharp
public class UnauthorizedException : AppException
{
    public UnauthorizedException(string message = "Unauthorized") : base(message, 401, "UNAUTHORIZED") { }
}
```

### ForbiddenException (403)

```csharp
public class ForbiddenException : AppException
{
    public ForbiddenException(string message = "Forbidden") : base(message, 403, "FORBIDDEN") { }
}
```

### BusinessRuleException (422)

```csharp
public class BusinessRuleException : AppException
{
    public BusinessRuleException(string message, string? errorCode = null) : base(message, 422, errorCode ?? "BUSINESS_RULE_VIOLATION") { }
}
```

### ConflictException (409)

```csharp
public class ConflictException : AppException
{
    public ConflictException(string message) : base(message, 409, "CONFLICT") { }
}
```

## Global Exception Middleware

### Implementation

Catches exceptions, logs, and returns ProblemDetails.

```csharp
public class GlobalExceptionMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await _next(context); }
        catch (Exception ex) { _logger.LogError(ex, "Unhandled exception"); await HandleExceptionAsync(context, ex); }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        // Maps to status and ProblemDetails based on exception type
    }
}
```

### Registration

```csharp
app.UseMiddleware<GlobalExceptionMiddleware>();
```

## Controller Error Handling

Let middleware handle exceptions; don't catch in controllers.

```csharp
[HttpGet("{id}")]
public async Task<ActionResult<NoteDto>> GetNote(int id)
{
    var result = await _mediator.Send(new GetNoteByIdQuery(id));
    return Ok(result);
}
```

## Repository Error Handling

Use BaseRepository for SQL handling; check output params for business errors.

```csharp
public async Task<int> CreateNoteAsync(Note note)
{
    return await ExecuteNonQuery(/* ... */, extractResult: (cmd) =>
    {
        var errorMsg = cmd.Parameters["@ov_ErrorMsg"].Value?.ToString();
        if (!string.IsNullOrEmpty(errorMsg)) throw new BusinessRuleException(errorMsg);
        return (int)cmd.Parameters["@ov_NoteId"].Value;
    });
}
```

## Logging Errors

Use structured logging.

```csharp
_logger.LogError(ex, "Failed to create note for user {UserId}", userId);
```

Levels: Trace, Debug, Information, Warning, Error, Critical.

Avoid logging sensitive data.

## HTTP Status Codes

| Code | Name | Use | Example |
|------|------|-----|---------|
| 200 | OK | GET/PUT success | Retrieved note |
| 201 | Created | POST success | Note created |
| 204 | No Content | DELETE success | Note deleted |
| 400 | Bad Request | Validation fail | Invalid data |
| 401 | Unauthorized | Auth fail | Invalid token |
| 403 | Forbidden | Not authorized | No permission |
| 404 | Not Found | Missing resource | Note not found |
| 409 | Conflict | Duplicate | Name exists |
| 422 | Unprocessable | Rule violation | Limit exceeded |
| 500 | Internal Error | Unexpected | DB failure |

## ProblemDetails Standard

Standard error response:

```json
{
  "type": "https://httpstatuses.com/404",
  "title": "Not Found",
  "status": 404,
  "detail": "Note not found",
  "instance": "GET /api/notes/123",
  "errorCode": "NOT_FOUND"
}
```

Validation errors include "errors" dictionary.

Development shows stack trace; production hides details.

## Quick Reference

### Exceptions

| Exception | Status | Use |
|-----------|--------|-----|
| NotFound | 404 | Resource missing |
| Validation | 400 | Input invalid |
| Unauthorized | 401 | Auth failed |
| Forbidden | 403 | No permission |
| BusinessRule | 422 | Rule violation |
| Conflict | 409 | Resource conflict |

### Logging

| Level | Use |
|-------|-----|
| Trace | Detailed dev |
| Debug | Flow |
| Info | Normal events |
| Warning | Handled issues |
| Error | Exceptions |
| Critical | Failures |