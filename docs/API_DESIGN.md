# API Design Guide

[← Back to Main Documentation](../.copilot-instructions.md.md)

---

## Table of Contents
1. [RESTful Principles](#restful-principles)
2. [Endpoint Naming](#endpoint-naming)
3. [HTTP Methods](#http-methods)
4. [Request/Response DTOs](#requestresponse-dtos)
5. [Status Codes](#status-codes)
6. [Pagination](#pagination)
7. [Filtering and Sorting](#filtering-and-sorting)
8. [API Versioning](#api-versioning)

---

## RESTful Principles

### Resource-Based URLs

```
✅ Good - Noun-based, resource-focused
GET    /api/notes
GET    /api/notes/123
POST   /api/notes
PUT    /api/notes/123
DELETE /api/notes/123

❌ Bad - Verb-based
GET    /api/getNotes
GET    /api/getNoteById/123
POST   /api/createNote
POST   /api/updateNote/123
POST   /api/deleteNote/123
```

### Hierarchy and Relationships

```
✅ Good - Clear hierarchy
GET    /api/users/456/notes           # Get notes for user 456
GET    /api/users/456/notes/123       # Get specific note for user
POST   /api/users/456/notes           # Create note for user
DELETE /api/users/456/notes/123       # Delete user's note

✅ Good - Independent resources
GET    /api/notes?userId=456          # Alternative using query params
```

---

## Endpoint Naming

### Conventions

```csharp
// ✅ Good naming
[Route("api/[controller]")]  // Results in /api/notes
public class NotesController : ControllerBase
{
    [HttpGet]                              // GET /api/notes
    public async Task<ActionResult<List<NoteDto>>> GetNotes()
    
    [HttpGet("{id}")]                      // GET /api/notes/123
    public async Task<ActionResult<NoteDto>> GetNote(int id)
    
    [HttpPost]                             // POST /api/notes
    public async Task<ActionResult<NoteDto>> CreateNote([FromBody] CreateNoteRequest request)
    
    [HttpPut("{id}")]                      // PUT /api/notes/123
    public async Task<IActionResult> UpdateNote(int id, [FromBody] UpdateNoteRequest request)
    
    [HttpPatch("{id}")]                    // PATCH /api/notes/123
    public async Task<IActionResult> PartialUpdateNote(int id, [FromBody] JsonPatchDocument<Note> patch)
    
    [HttpDelete("{id}")]                   // DELETE /api/notes/123
    public async Task<IActionResult> DeleteNote(int id)
}
```

### Plural vs Singular

```
✅ Use plural nouns for collections
/api/notes
/api/users
/api/products

✅ Use plural with ID for single resource
/api/notes/123
/api/users/456

❌ Avoid mixing plural and singular
/api/note/123      # Inconsistent
/api/notes         # Should be consistent
```

### Special Actions

```csharp
// For actions that don't fit CRUD, use action names after the resource
[HttpPost("{id}/archive")]        // POST /api/notes/123/archive
public async Task<IActionResult> ArchiveNote(int id)

[HttpPost("{id}/restore")]        // POST /api/notes/123/restore
public async Task<IActionResult> RestoreNote(int id)

[HttpPost("{id}/share")]          // POST /api/notes/123/share
public async Task<IActionResult> ShareNote(int id, [FromBody] ShareRequest request)

[HttpGet("search")]                // GET /api/notes/search?q=keyword
public async Task<ActionResult<List<NoteDto>>> SearchNotes([FromQuery] string q)
```

---

## HTTP Methods

### Standard CRUD Operations

| HTTP Method | Operation | Idempotent | Safe | Request Body | Response Body |
|-------------|-----------|------------|------|--------------|---------------|
| GET | Read | Yes | Yes | No | Yes |
| POST | Create | No | No | Yes | Yes (created resource) |
| PUT | Update (full) | Yes | No | Yes | Optional |
| PATCH | Update (partial) | No | No | Yes | Optional |
| DELETE | Delete | Yes | No | No | No (204) |

### GET - Retrieve Resources

```csharp
// Get all notes
[HttpGet]
[ProducesResponseType(typeof(List<NoteDto>), StatusCodes.Status200OK)]
public async Task<ActionResult<List<NoteDto>>> GetNotes(
    [FromQuery] bool getAll = false,
    [FromQuery] string? searchText = null)
{
    var query = new GetNotesQuery(getAll, searchText);
    var result = await _mediator.Send(query);
    return Ok(result);
}

// Get single note
[HttpGet("{id}")]
[ProducesResponseType(typeof(NoteDto), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
public async Task<ActionResult<NoteDto>> GetNote(int id)
{
    var query = new GetNoteByIdQuery(id);
    var result = await _mediator.Send(query);
    return Ok(result);
}
```

### POST - Create Resources

```csharp
[HttpPost]
[ProducesResponseType(typeof(NoteDto), StatusCodes.Status201Created)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
public async Task<ActionResult<NoteDto>> CreateNote([FromBody] CreateNoteRequest request)
{
    var command = new CreateNoteCommand(request.Name, request.Description);
    var result = await _mediator.Send(command);
    
    // Return 201 with Location header
    return CreatedAtAction(
        nameof(GetNote),
        new { id = result.NoteId },
        result
    );
}
```

### PUT - Full Update

```csharp
[HttpPut("{id}")]
[ProducesResponseType(StatusCodes.Status204NoContent)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
public async Task<IActionResult> UpdateNote(int id, [FromBody] UpdateNoteRequest request)
{
    var command = new UpdateNoteCommand(id, request.Name, request.Description, request.Tags);
    await _mediator.Send(command);
    return NoContent();
}
```

### PATCH - Partial Update

```csharp
[HttpPatch("{id}")]
[ProducesResponseType(StatusCodes.Status204NoContent)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
public async Task<IActionResult> PartialUpdateNote(int id, [FromBody] PatchNoteRequest request)
{
    var command = new PatchNoteCommand(id, request);
    await _mediator.Send(command);
    return NoContent();
}
```

### DELETE - Remove Resources

```csharp
[HttpDelete("{id}")]
[ProducesResponseType(StatusCodes.Status204NoContent)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
public async Task<IActionResult> DeleteNote(int id)
{
    var command = new DeleteNoteCommand(id);
    await _mediator.Send(command);
    return NoContent();
}
```

---

## Request/Response DTOs

### Never Expose Domain Entities

```csharp
// ❌ Bad - Exposing domain entity directly
[HttpGet("{id}")]
public async Task<ActionResult<Note>> GetNote(int id)
{
    return await _repository.GetNoteByIdAsync(id);
}

// ✅ Good - Using DTO
[HttpGet("{id}")]
public async Task<ActionResult<NoteDto>> GetNote(int id)
{
    var query = new GetNoteByIdQuery(id);
    var result = await _mediator.Send(query);
    return Ok(result);
}
```

### Request DTOs

```csharp
// SuperApp.Application/DTOs/Requests/CreateNoteRequest.cs
public record CreateNoteRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;

    [StringLength(5000)]
    public string? Description { get; init; }

    [StringLength(500)]
    public string? Tags { get; init; }
}

// SuperApp.Application/DTOs/Requests/UpdateNoteRequest.cs
public record UpdateNoteRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;

    [StringLength(5000)]
    public string? Description { get; init; }

    [StringLength(500)]
    public string? Tags { get; init; }
}

// SuperApp.Application/DTOs/Requests/PatchNoteRequest.cs
public record PatchNoteRequest
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? Tags { get; init; }
}
```

### Response DTOs

```csharp
// SuperApp.Application/DTOs/Responses/NoteDto.cs
public record NoteDto
{
    public int NoteId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Tags { get; init; }
    public DateTime CreatedDate { get; init; }
    public DateTime? ModifiedDate { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
}

// SuperApp.Application/DTOs/Responses/NoteListDto.cs
public record NoteListDto
{
    public int NoteId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Tags { get; init; }
    public DateTime CreatedDate { get; init; }
    // Lighter version for lists - no description
}

// SuperApp.Application/DTOs/Responses/NoteDetailDto.cs
public record NoteDetailDto
{
    public int NoteId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Tags { get; init; }
    public DateTime CreatedDate { get; init; }
    public DateTime? ModifiedDate { get; init; }
    public UserDto CreatedBy { get; init; } = null!;
    public UserDto? ModifiedBy { get; init; }
    public List<CommentDto> Comments { get; init; } = new();
    // Rich version with related data
}
```

### Wrapper Response for Consistency

```csharp
// SuperApp.Shared/Results/Result.cs
public record Result<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Message { get; init; }
    public List<string> Errors { get; init; } = new();

    public static Result<T> SuccessResult(T data, string? message = null)
        => new() { Success = true, Data = data, Message = message };

    public static Result<T> FailureResult(string message, List<string>? errors = null)
        => new() { Success = false, Message = message, Errors = errors ?? new() };
}

// Usage in controller
[HttpGet("{id}")]
public async Task<ActionResult<Result<NoteDto>>> GetNote(int id)
{
    var query = new GetNoteByIdQuery(id);
    var note = await _mediator.Send(query);
    return Ok(Result<NoteDto>.SuccessResult(note, "Note retrieved successfully"));
}
```

---

## Status Codes

### Success Codes (2xx)

```csharp
// 200 OK - Successful GET, PUT, PATCH
[HttpGet("{id}")]
public async Task<ActionResult<NoteDto>> GetNote(int id)
{
    var result = await _mediator.Send(new GetNoteByIdQuery(id));
    return Ok(result); // 200
}

// 201 Created - Successful POST
[HttpPost]
public async Task<ActionResult<NoteDto>> CreateNote([FromBody] CreateNoteRequest request)
{
    var result = await _mediator.Send(new CreateNoteCommand(request));
    return CreatedAtAction(nameof(GetNote), new { id = result.NoteId }, result); // 201
}

// 204 No Content - Successful DELETE, or PUT/PATCH with no response body
[HttpDelete("{id}")]
public async Task<IActionResult> DeleteNote(int id)
{
    await _mediator.Send(new DeleteNoteCommand(id));
    return NoContent(); // 204
}

// 202 Accepted - Request accepted for async processing
[HttpPost("{id}/process")]
public async Task<IActionResult> ProcessNote(int id)
{
    await _queue.EnqueueProcessingJob(id);
    return AcceptedAtAction(nameof(GetProcessingStatus), new { id }); // 202
}
```

### Client Error Codes (4xx)

```csharp
// 400 Bad Request - Validation errors (handled by FluentValidation)
// Thrown automatically by ValidationBehavior

// 401 Unauthorized - Authentication required/failed (handled by JWT middleware)
[Authorize]
[HttpGet]
public async Task<ActionResult<List<NoteDto>>> GetNotes()
{
    // Automatically returns 401 if no valid token
}

// 403 Forbidden - Authenticated but not authorized
[HttpDelete("{id}")]
public async Task<IActionResult> DeleteNote(int id)
{
    // Handler throws ForbiddenException if user doesn't own the note
    await _mediator.Send(new DeleteNoteCommand(id));
    return NoContent();
}

// 404 Not Found - Resource doesn't exist
[HttpGet("{id}")]
public async Task<ActionResult<NoteDto>> GetNote(int id)
{
    // Handler throws NotFoundException if note doesn't exist
    var result = await _mediator.Send(new GetNoteByIdQuery(id));
    return Ok(result);
}

// 409 Conflict - Resource conflict
[HttpPost]
public async Task<ActionResult<NoteDto>> CreateNote([FromBody] CreateNoteRequest request)
{
    // Handler throws ConflictException if name already exists
    var result = await _mediator.Send(new CreateNoteCommand(request));
    return CreatedAtAction(nameof(GetNote), new { id = result.NoteId }, result);
}

// 422 Unprocessable Entity - Business rule violation
[HttpPost("{id}/archive")]
public async Task<IActionResult> ArchiveNote(int id)
{
    // Handler throws BusinessRuleException if note can't be archived
    await _mediator.Send(new ArchiveNoteCommand(id));
    return NoContent();
}
```

### Server Error Codes (5xx)

```csharp
// 500 Internal Server Error - Unexpected errors (handled by GlobalExceptionMiddleware)
// 503 Service Unavailable - Dependency unavailable
[HttpGet]
public async Task<ActionResult<List<NoteDto>>> GetNotes()
{
    // If database is down, middleware returns 503
}
```

---

## Pagination

### Query Parameters Pattern

```csharp
// SuperApp.Application/DTOs/Requests/PaginationRequest.cs
public record PaginationRequest
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    
    public int Skip => (PageNumber - 1) * PageSize;
    public int Take => PageSize;
}

// SuperApp.Application/DTOs/Responses/PagedResult.cs
public record PagedResult<T>
{
    public List<T> Items { get; init; } = new();
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}

// Controller usage
[HttpGet]
public async Task<ActionResult<PagedResult<NoteDto>>> GetNotes(
    [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 20,
    [FromQuery] string? searchText = null)
{
    var query = new GetNotesQuery(pageNumber, pageSize, searchText);
    var result = await _mediator.Send(query);
    return Ok(result);
}

// Response example:
// GET /api/notes?pageNumber=2&pageSize=10
{
    "items": [ /* 10 notes */ ],
    "pageNumber": 2,
    "pageSize": 10,
    "totalCount": 156,
    "totalPages": 16,
    "hasPreviousPage": true,
    "hasNextPage": true
}
```

### Link Header Pattern (Alternative)

```csharp
[HttpGet]
public async Task<ActionResult<List<NoteDto>>> GetNotes(
    [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 20)
{
    var query = new GetNotesQuery(pageNumber, pageSize);
    var result = await _mediator.Send(query);
    
    // Add pagination links to headers
    var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.Path}";
    Response.Headers.Add("X-Total-Count", result.TotalCount.ToString());
    Response.Headers.Add("Link", GeneratePaginationLinks(baseUrl, pageNumber, pageSize, result.TotalPages));
    
    return Ok(result.Items);
}

private string GeneratePaginationLinks(string baseUrl, int currentPage, int pageSize, int totalPages)
{
    var links = new List<string>();
    
    if (currentPage > 1)
        links.Add($"<{baseUrl}?pageNumber={currentPage - 1}&pageSize={pageSize}>; rel=\"prev\"");
    
    if (currentPage < totalPages)
        links.Add($"<{baseUrl}?pageNumber={currentPage + 1}&pageSize={pageSize}>; rel=\"next\"");
    
    links.Add($"<{baseUrl}?pageNumber=1&pageSize={pageSize}>; rel=\"first\"");
    links.Add($"<{baseUrl}?pageNumber={totalPages}&pageSize={pageSize}>; rel=\"last\"");
    
    return string.Join(", ", links);
}

// Response headers:
// X-Total-Count: 156
// Link: <https://api.example.com/notes?pageNumber=1&pageSize=20>; rel="prev",
//       <https://api.example.com/notes?pageNumber=3&pageSize=20>; rel="next",
//       <https://api.example.com/notes?pageNumber=1&pageSize=20>; rel="first",
//       <https://api.example.com/notes?pageNumber=8&pageSize=20>; rel="last"
```

---

## Filtering and Sorting

### Query Parameter Pattern

```csharp
// SuperApp.Application/DTOs/Requests/NoteFilterRequest.cs
public record NoteFilterRequest
{
    public string? SearchText { get; init; }
    public DateTime? CreatedFrom { get; init; }
    public DateTime? CreatedTo { get; init; }
    public List<string>? Tags { get; init; }
    public bool? IsArchived { get; init; }
    public string? SortBy { get; init; } = "CreatedDate";
    public string? SortOrder { get; init; } = "desc"; // asc or desc
}

// Controller
[HttpGet]
public async Task<ActionResult<PagedResult<NoteDto>>> GetNotes(
    [FromQuery] NoteFilterRequest filter,
    [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 20)
{
    var query = new GetNotesQuery(filter, pageNumber, pageSize);
    var result = await _mediator.Send(query);
    return Ok(result);
}

// Example requests:
// GET /api/notes?searchText=meeting&tags=work&tags=urgent&sortBy=Name&sortOrder=asc
// GET /api/notes?createdFrom=2024-01-01&createdTo=2024-12-31&isArchived=false
// GET /api/notes?searchText=project&sortBy=ModifiedDate&sortOrder=desc&pageNumber=1&pageSize=50
```

### Complex Filtering with OData (Optional)

```csharp
// Install: Microsoft.AspNetCore.OData
// Program.cs
builder.Services.AddControllers()
    .AddOData(options => options
        .Select()
        .Filter()
        .OrderBy()
        .Expand()
        .Count()
        .SetMaxTop(100));

// Controller
[HttpGet]
[EnableQuery] // Enables OData query options
public IQueryable<NoteDto> GetNotes()
{
    return _context.Notes
        .ProjectTo<NoteDto>(_mapper.ConfigurationProvider);
}

// Example requests:
// GET /api/notes?$filter=contains(Name,'meeting')&$orderby=CreatedDate desc
// GET /api/notes?$filter=CreatedDate ge 2024-01-01&$top=10&$skip=20
// GET /api/notes?$select=NoteId,Name&$expand=CreatedBy
```

---

## API Versioning

### URL Path Versioning

```csharp
// Install: Microsoft.AspNetCore.Mvc.Versioning

// Program.cs
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
});

// Controller v1
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class NotesController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<NoteDto>>> GetNotes()
    {
        // V1 implementation
    }
}

// Controller v2
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class NotesV2Controller : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<NoteDto>>> GetNotes([FromQuery] PaginationRequest request)
    {
        // V2 implementation with pagination
    }
}

// Usage:
// GET /api/v1/notes - Version 1
// GET /api/v2/notes?pageNumber=1&pageSize=20 - Version 2
```

### Header Versioning

```csharp
// Program.cs
builder.Services.AddApiVersioning(options =>
{
    options.ApiVersionReader = new HeaderApiVersionReader("X-API-Version");
});

// Request:
// GET /api/notes
// Headers: X-API-Version: 2.0
```

### Query String Versioning

```csharp
// Program.cs
builder.Services.AddApiVersioning(options =>
{
    options.ApiVersionReader = new QueryStringApiVersionReader("api-version");
});

// Request:
// GET /api/notes?api-version=2.0
```

### Deprecation

```csharp
[ApiVersion("1.0", Deprecated = true)]
[Route("api/v{version:apiVersion}/[controller]")]
public class NotesController : ControllerBase
{
    // Deprecated version
}

// Response headers include:
// api-deprecated-versions: 1.0
// api-supported-versions: 2.0, 3.0
```

---

## Content Negotiation

### Accept Header

```csharp
// Supports both JSON and XML
[HttpGet("{id}")]
[Produces("application/json", "application/xml")]
public async Task<ActionResult<NoteDto>> GetNote(int id)
{
    var result = await _mediator.Send(new GetNoteByIdQuery(id));
    return Ok(result);
}

// Client request:
// GET /api/notes/123
// Accept: application/json  → Returns JSON
// Accept: application/xml   → Returns XML
```

### Custom Media Types

```csharp
[HttpGet("{id}")]
[Produces("application/vnd.superapp.note.v1+json")]
public async Task<ActionResult<NoteDto>> GetNote(int id)
{
    var result = await _mediator.Send(new GetNoteByIdQuery(id));
    return Ok(result);
}

// Client request:
// GET /api/notes/123
// Accept: application/vnd.superapp.note.v1+json
```

---

## Best Practices Summary

### ✅ DO

1. **Use plural nouns for resources**
   ```
   /api/notes, /api/users, /api/products
   ```

2. **Use HTTP methods correctly**
   - GET for reading
   - POST for creating
   - PUT for full updates
   - PATCH for partial updates
   - DELETE for removing

3. **Return appropriate status codes**
   - 200 for successful GET/PUT/PATCH
   - 201 for successful POST
   - 204 for successful DELETE
   - 400 for validation errors
   - 404 for not found

4. **Use DTOs, never expose domain entities**
   ```csharp
   public async Task<ActionResult<NoteDto>> GetNote(int id)
   ```

5. **Include proper API documentation**
   ```csharp
   [ProducesResponseType(typeof(NoteDto), StatusCodes.Status200OK)]
   [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
   ```

6. **Implement pagination for collections**
   ```csharp
   [HttpGet]
   public async Task<ActionResult<PagedResult<NoteDto>>> GetNotes(
       [FromQuery] int pageNumber = 1,
       [FromQuery] int pageSize = 20)
   ```

7. **Version your API**
   ```csharp
   [ApiVersion("1.0")]
   [Route("api/v{version:apiVersion}/[controller]")]
   ```

### ❌ DON'T

1. **Don't use verbs in URLs**
   ```
   ❌ /api/getNotes
   ✅ /api/notes
   ```

2. **Don't mix singular and plural**
   ```
   ❌ /api/note/123
   ❌ /api/notes
   ✅ /api/notes/123
   ```

3. **Don't use POST for everything**
   ```
   ❌ POST /api/updateNote
   ✅ PUT /api/notes/123
   ```

4. **Don't return different structures for success/error**
   ```
   ❌ Success: { data: {} }, Error: { error: "..." }
   ✅ Use ProblemDetails for errors
   ```

5. **Don't expose implementation details**
   ```
   ❌ /api/notes/getFromDatabase
   ✅ /api/notes
   ```

6. **Don't return 200 for errors**
   ```
   ❌ return Ok(new { error = "Not found" })
   ✅ throw new NotFoundException(...)
   ```

---

[← Back to Main Documentation](../.copilot-instructions.md.md) | [Next: Validation →](VALIDATION.md)