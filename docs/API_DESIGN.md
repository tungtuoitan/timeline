# API Design Guide

## RESTful Principles

- **Resource-Based URLs**: Dùng danh từ số nhiều, tránh động từ
  - ✅ `GET /api/notes`, `POST /api/notes`, `PUT /api/notes/{id}`
  - ❌ `GET /api/getNotes`, `POST /api/createNote`

- **Hierarchy**: `/api/workspaces/{id}/items` hoặc query param `/api/notes?workspaceId={id}`

## Endpoint Naming

- **Controller Route**: `[Route("api/[controller]")]`
- **Plural nouns**: `/api/notes`, `/api/workspaces`
- **Special actions**: `POST /api/notes/{id}/archive`, `GET /api/notes/search`

## HTTP Methods

| Method | Purpose | Body | Return | Idempotent |
|--------|---------|------|--------|-----------|
| GET | Read | No | 200 + Data | Yes |
| POST | Create | Yes | 201 + Location | No |
| PUT | Full update | Yes | 200/204 | Yes |
| PATCH | Partial update | Yes | 200/204 | No |
| DELETE | Delete | No | 204 | Yes |

## Request/Response DTOs

**Requests** (SuperAppModels/DTOs/Requests/):
- CreateNoteRequest, UpdateNoteRequest
- Validation attributes: [Required], [StringLength]

**Responses** (SuperAppModels/DTOs/Responses/):
- NoteResponse, WorkspaceResponse
- No sensitive data (passwords, internal IDs)

**Never expose domain models directly**

## Standard Response Format

```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public List<string>? Errors { get; set; }
}
```

## Status Codes

### Success (2xx)
- **200 OK**: GET/PUT/PATCH success
- **201 Created**: POST success + Location header
- **204 No Content**: DELETE/PUT success without body
- **202 Accepted**: Async processing

### Client Error (4xx)
- **400 Bad Request**: Validation fail
- **401 Unauthorized**: Authentication fail
- **403 Forbidden**: Not authorized for resource
- **404 Not Found**: Resource not found
- **409 Conflict**: Duplicate resource
- **422 Unprocessable Entity**: Business rule violation

### Server Error (5xx)
- **500 Internal Server Error**: Unexpected error
- **503 Service Unavailable**: Dependency down

## Pagination

**Query Parameters:**
```csharp
public class PaginationRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
```

**Response:**
```csharp
public class PagedResponse<T>
{
    public List<T> Items { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage { get; set; }
    public bool HasNextPage { get; set; }
}
```

## Filtering & Sorting

```csharp
public class SearchNotesRequest
{
    public string? SearchText { get; set; }
    public string? CreatedFrom { get; set; } // "YYYY-MM-DD", "YYYY-MM" or ISO with offset -> TimeParsing.ParseInstantLenient
    public string? CreatedTo { get; set; }   // same, whole day/month included -> TimeParsing.ParseInstantLenientEnd
    public string SortBy { get; set; } = "CreatedDate";
    public string SortOrder { get; set; } = "desc"; // asc/desc
}
```

## API Versioning

**URL Path** (recommended):
```csharp
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class NotesController : ControllerBase
```

**Header** (alternative):
```
X-API-Version: 1.0
```

## Content Negotiation

- Default: `application/json`
- Accept header: `Accept: application/json`, `Accept: application/xml`
- Custom media types: `application/vnd.superapp.note.v1+json`

## Best Practices

### ✅ DO
1. Dùng plural nouns cho resources
2. Tuân thủ HTTP methods đúng
3. Trả status codes phù hợp
4. Dùng DTOs, không expose domain models
5. Document với Swagger/OpenAPI
6. Implement pagination cho collections
7. Version API từ đầu
8. Validate inputs
9. Use consistent error format
10. Log requests/responses

### ❌ DON'T
1. Dùng verbs trong URLs
2. Mix singular/plural
3. Dùng POST cho mọi thứ
4. Trả structures khác nhau cho success/error
5. Expose sensitive data
6. Trả 200 cho errors
7. Skip pagination
8. Hardcode values
9. Return domain entities
10. Ignore caching headers

## Example Endpoints

```csharp
// NotesController
[HttpGet]                          // GET /api/notes
[HttpGet("{id}")]                  // GET /api/notes/123
[HttpPost]                         // POST /api/notes
[HttpPut("{id}")]                  // PUT /api/notes/123
[HttpDelete]                       // DELETE /api/notes?ids=1,2,3
[HttpGet("search")]                // GET /api/notes/search?text=foo

// WorkspaceController
[HttpGet("{id}/items")]            // GET /api/workspaces/1/items
[HttpPost("{id}/items")]           // POST /api/workspaces/1/items
```

## Swagger Documentation

```csharp
[HttpGet("{id}")]
[ProducesResponseType(typeof(NoteResponse), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> GetNote(int id)
{
    var note = await _noteService.GetNoteByIdAsync(id);
    return Ok(note);
}
```

---

**Version:** 2.0
**Last Updated:** November 2025
**See Also:** ARCHITECTURE.md, ERROR_HANDLING.md
