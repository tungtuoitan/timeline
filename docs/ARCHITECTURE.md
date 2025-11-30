# Architecture Guide

## Tổng quan

SuperApp sử dụng **Clean Architecture** với 4 layers rõ ràng, dependency pointing inward.

## Architecture Diagram

```
┌─────────────────────────────────────────────────────┐
│                  SuperAppAPI                         │
│              (Presentation Layer)                    │
│    Controllers, Middleware, Extensions               │
└────────────────────┬────────────────────────────────┘
                     │ depends on ↓
┌─────────────────────────────────────────────────────┐
│               SuperAppServices                       │
│             (Application Layer)                      │
│    Services, Business Logic, Mappings                │
└────────────────────┬────────────────────────────────┘
                     │ depends on ↓
┌─────────────────────────────────────────────────────┐
│                SuperAppModels                        │
│                (Domain Layer)                        │
│    Models, DTOs, Enums (No dependencies)            │
└─────────────────────────────────────────────────────┘
                     ↑ depends on
┌─────────────────────────────────────────────────────┐
│           SuperAppDataRepositories                   │
│             (Infrastructure Layer)                   │
│    Repositories, DbContext, Configurations           │
└─────────────────────────────────────────────────────┘
```

## Clean Architecture Principles

1. **Independence of Frameworks**: Business logic không phụ thuộc EF Core, có thể thay bằng Dapper
2. **Testability**: Test business rules độc lập không cần UI, DB, server
3. **Independence of UI**: Thay đổi UI không ảnh hưởng business logic
4. **Independence of Database**: Dễ dàng thay SQL Server sang PostgreSQL
5. **Dependency Rule**: Dependencies hướng inward (outer → inner)

## Project Structure

### SuperAppAPI (Presentation)
```
Controllers/
├── NotesController.cs
├── WorkspaceController.cs
├── UserProfileController.cs
├── StandardRegistryController.cs
└── HealthController.cs

Middleware/
├── GlobalExceptionMiddleware.cs
├── JwtValidationMiddleware.cs
├── ValidateTokenMiddleware.cs
└── SecurityHeadersMiddleware.cs

Extensions/
└── ClaimsPrincipalExtensions.cs

Exceptions/
└── AppException.cs
```

### SuperAppServices (Application)
```
Services/
├── NoteService.cs
└── WorkspaceService.cs

Interfaces/
├── INoteService.cs
└── IWorkspaceService.cs

Mappings/
└── MappingProfile.cs
```

### SuperAppModels (Domain)
```
Models/
├── User.cs, UserProfile.cs
├── Note.cs, NoteVersion.cs, NoteMember.cs
├── Workspace.cs, WorkspaceMember.cs, WorkspaceItem.cs
├── Tag.cs, EntityTag.cs, EntityType.cs
└── StandardRegistry.cs

DTOs/
├── Requests/ (CreateNoteRequest, UpdateNoteRequest,...)
└── Responses/ (NoteResponse, WorkspaceResponse,...)

Enums/
└── ApplicationEnums.cs
```

### SuperAppDataRepositories (Infrastructure)
```
Repositories/
├── NoteRepository.cs
├── WorkspaceRepository.cs
├── UserRepository.cs
├── UserProfileRepository.cs
└── StandardRegistryRepository.cs

Ins/ (Interfaces)
├── INoteRepository.cs
├── IWorkspaceRepository.cs
├── IUserRepository.cs
├── IUserProfileRepository.cs
└── IStandardRegistryRepository.cs

Data/
├── ApplicationDbContext.cs
├── ConnectionFactory.cs
├── IConnectionFactory.cs
└── Configurations/ (Entity Fluent API configs)

Migrations/
└── yyyyMMddHHmmss_MigrationName.cs
```

## Layer Responsibilities

### 1. SuperAppAPI (Presentation)
**Làm gì:**
- HTTP request/response handling
- Route đến Services
- Authentication & Authorization
- Model binding & validation
- Swagger documentation

**Không làm:**
- Business logic
- Direct database access
- Complex validations

### 2. SuperAppServices (Application)
**Làm gì:**
- Implement business logic
- Orchestrate workflows
- Call repositories
- Map DTOs ↔ Models
- Business rule validation

**Không làm:**
- HTTP concerns
- Direct SQL queries
- Framework-specific code

### 3. SuperAppModels (Domain)
**Làm gì:**
- Define entities & value objects
- Business rules trong models
- DTOs cho data transfer
- Domain enums

**Không làm:**
- Reference other layers
- Database/HTTP/Infrastructure concerns

### 4. SuperAppDataRepositories (Infrastructure)
**Làm gì:**
- Database access (EF Core + SP)
- Repository implementations
- DbContext & configurations
- Migrations
- External service integrations

**Không làm:**
- Business logic
- HTTP/Presentation concerns

## Dependency Rules

### ✅ Allowed
```
SuperAppAPI → SuperAppServices → SuperAppModels
SuperAppAPI → SuperAppDataRepositories (DI registration only)
SuperAppDataRepositories → SuperAppModels
SuperAppDataRepositories → (Interfaces từ Services nếu cần)
```

### ❌ Forbidden
```
SuperAppModels → Any layer
SuperAppServices → SuperAppAPI
SuperAppDataRepositories → SuperAppAPI
```

## Service Pattern (thay CQRS)

SuperApp sử dụng **Service pattern** thay vì CQRS:

```csharp
public interface INoteService
{
    Task<List<NoteResponse>> GetNotesAsync(string userEmail);
    Task<NoteResponse> CreateNoteAsync(CreateNoteRequest request, string userEmail);
    Task<NoteResponse> UpdateNoteAsync(UpdateNoteRequest request);
    Task DeleteNotesAsync(DeleteNotesRequest request);
}
```

**Workflow:**
1. Controller nhận request
2. Gọi Service method
3. Service thực hiện business logic
4. Service gọi Repository
5. Repository truy cập DB (EF/SP)
6. Map entity → DTO
7. Return response

## Data Flow

```
HTTP Request
    ↓
Controller
    ↓
Service (Business Logic)
    ↓
Repository (Interface)
    ↓
Database (EF Core / Stored Procedure)
    ↓
Map Entity → DTO
    ↓
HTTP Response
```

## DI Registration (Program.cs)

```csharp
// DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Connection Factory
builder.Services.AddSingleton<IConnectionFactory, ConnectionFactory>();

// Repositories
builder.Services.AddScoped<INoteRepository, NoteRepository>();
builder.Services.AddScoped<IWorkspaceRepository, WorkspaceRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Services
builder.Services.AddScoped<INoteService, NoteService>();
builder.Services.AddScoped<IWorkspaceService, WorkspaceService>();

// AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));
```

## Hybrid Data Access

| Use Case | Tech | Reason |
|----------|------|--------|
| Simple CRUD | EF Core | Type-safe, maintainable |
| 2-3 JOINs | EF Core | LINQ readable |
| Complex queries | SP | Performance, optimized |
| Reporting/Bulk | SP | Better control |

## Benefits

✅ **Testability:** Mock repositories, test services independently
✅ **Maintainability:** Clear boundaries, changes localized
✅ **Flexibility:** Swap DB/UI/providers easily
✅ **Scalability:** Ready for microservices
✅ **Collaboration:** Clear layer separation

## Example Flow

```
Browser: POST /api/notes
    ↓
NotesController.CreateNote(request)
    ↓
_noteService.CreateNoteAsync(request, userEmail)
    ↓
NoteService validates & maps request → Note model
    ↓
_noteRepository.CreateNoteAsync(note)
    ↓
Repository: _context.Notes.Add(note)
            _context.SaveChangesAsync()
    ↓
Database: INSERT INTO notes
    ↓
Map Note → NoteResponse
    ↓
Controller: 201 Created + NoteResponse
```

---

**Version:** 2.0
**Last Updated:** November 2025
**See Also:** PROJECT_OVERVIEW.md, DATABASE_ACCESS.md
