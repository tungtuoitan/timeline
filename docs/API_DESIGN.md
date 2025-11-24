# API Design Guide Summary

## Table of Contents
- [API Design Guide Summary](#api-design-guide-summary)
  - [Table of Contents](#table-of-contents)
  - [RESTful Principles](#restful-principles)
  - [Endpoint Naming](#endpoint-naming)
  - [HTTP Methods](#http-methods)
  - [Request/Response DTOs](#requestresponse-dtos)
  - [Status Codes](#status-codes)
  - [Pagination](#pagination)
  - [Filtering and Sorting](#filtering-and-sorting)
  - [API Versioning](#api-versioning)
  - [Content Negotiation](#content-negotiation)
  - [Best Practices Summary](#best-practices-summary)
    - [✅ DO](#-do)
    - [❌ DON'T](#-dont)

## RESTful Principles

- **Resource-Based URLs**: Sử dụng danh từ tập trung vào tài nguyên, tránh động từ. Ví dụ: GET /api/notes, POST /api/notes, PUT /api/notes/{id}.

- **Hierarchy and Relationships**: Xây dựng phân cấp rõ ràng như /api/users/{userId}/notes, hoặc sử dụng query params như /api/notes?userId={userId} cho tài nguyên độc lập.

## Endpoint Naming

- **Conventions**: Sử dụng [Route("api/[controller]")] cho controller, kết hợp HttpGet, HttpPost, HttpPut, HttpPatch, HttpDelete với path phù hợp.

- **Plural vs Singular**: Luôn dùng danh từ số nhiều cho collections (/api/notes) và single resources (/api/notes/{id}). Tránh mix singular/plural.

- **Special Actions**: Đối với hành động không phải CRUD, thêm action name sau resource, ví dụ: POST /api/notes/{id}/archive, GET /api/notes/search.

## HTTP Methods

- **Standard CRUD Operations**:
  - GET: Read (idempotent, safe, no body).
  - POST: Create (non-idempotent, has body, return 201 with Location).
  - PUT: Full update (idempotent, has body, return 204).
  - PATCH: Partial update (non-idempotent, has body, return 204).
  - DELETE: Delete (idempotent, no body, return 204).

- **GET**: Retrieve all or single resource, hỗ trợ query params như searchText.

- **POST**: Create resource, return created DTO.

- **PUT**: Full update với toàn bộ data.

- **PATCH**: Partial update với fields thay đổi.

- **DELETE**: Remove resource.

## Request/Response DTOs

- **Never Expose Domain Entities**: Luôn dùng DTOs để map data, tránh lộ domain model.

- **Request DTOs**: Sử dụng records với validation attributes (Required, StringLength), ví dụ: CreateNoteRequest, UpdateNoteRequest, PatchNoteRequest.

- **Response DTOs**: Sử dụng records cho data trả về, ví dụ: NoteDto (full), NoteListDto (light for lists), NoteDetailDto (with relations).

- **Wrapper Response**: Sử dụng Result<T> với Success, Data, Message, Errors để consistent response structure.

## Status Codes

- **Success (2xx)**:
  - 200 OK: Successful GET/PUT/PATCH.
  - 201 Created: Successful POST.
  - 204 No Content: Successful DELETE/PUT/PATCH without body.
  - 202 Accepted: Async processing.

- **Client Error (4xx)**:
  - 400 Bad Request: Validation errors.
  - 401 Unauthorized: Auth failed.
  - 403 Forbidden: Not authorized.
  - 404 Not Found: Resource missing.
  - 409 Conflict: Resource conflict.
  - 422 Unprocessable Entity: Business rule violation.

- **Server Error (5xx)**:
  - 500 Internal Server Error: Unexpected.
  - 503 Service Unavailable: Dependency down.

## Pagination

- **Query Parameters**: Sử dụng pageNumber (default 1), pageSize (default 20), tính skip/take.

- **PagedResult<T>**: Bao gồm Items, PageNumber, PageSize, TotalCount, TotalPages, HasPreviousPage, HasNextPage.

- **Alternative: Link Header**: Thêm X-Total-Count và Link với rel=prev/next/first/last.

## Filtering and Sorting

- **Query Parameter Pattern**: Sử dụng record như NoteFilterRequest với searchText, createdFrom/To, tags, isArchived, sortBy (default CreatedDate), sortOrder (asc/desc).

- **Complex Filtering (Optional)**: Sử dụng OData cho $filter, $orderby, $top, $skip, $select, $expand.

## API Versioning

- **URL Path Versioning**: Sử dụng /api/v{version:apiVersion}/[controller], với ApiVersion attribute.

- **Header Versioning**: Sử dụng X-API-Version header.

- **Query String Versioning**: Sử dụng ?api-version={version}.

- **Deprecation**: Đánh dấu ApiVersion Deprecated=true, thêm headers api-deprecated-versions và api-supported-versions.

## Content Negotiation

- **Accept Header**: Hỗ trợ multiple formats như application/json, application/xml.

- **Custom Media Types**: Sử dụng như application/vnd.superapp.note.v1+json cho versioning qua content type.

## Best Practices Summary

### ✅ DO
1. Sử dụng plural nouns cho resources.
2. Tuân thủ HTTP methods đúng cho CRUD.
3. Trả status codes phù hợp.
4. Sử dụng DTOs, không expose domain.
5. Thêm API documentation với ProducesResponseType.
6. Implement pagination cho collections.
7. Version API.

### ❌ DON'T
1. Sử dụng verbs trong URLs.
2. Mix singular/plural.
3. Dùng POST cho mọi thứ.
4. Trả structures khác nhau cho success/error.
5. Expose implementation details.
6. Trả 200 cho errors.