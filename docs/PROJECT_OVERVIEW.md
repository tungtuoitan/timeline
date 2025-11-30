# Project Overview

## Giới thiệu

SuperApp là .NET 8 backend API sử dụng Clean Architecture với hybrid data access pattern.

## Technology Stack

### Core
- .NET 8.0, C# 12.0
- ASP.NET Core Web API
- SQL Server


### Libraries
- AutoMapper: Object mapping
- Serilog: Structured logging
- JWT Bearer: Authentication
- Google OAuth: External auth

## Kiến trúc

Clean Architecture với 4 layers:

| Layer | Project | Chức năng |
|-------|---------|-----------|
| Presentation | **SuperAppAPI** | Controllers, Middleware, Program.cs |
| Application | **SuperAppServices** | Services, Business Logic, Mappings |
| Domain | **SuperAppModels** | Models, DTOs, Enums |
| Infrastructure | **SuperAppDataRepositories** | Repositories, DbContext, Data Access |

**Dependency Rules:** Outer → Inner. Domain không phụ thuộc ai.

```
SuperAppAPI → SuperAppServices → SuperAppModels
                                      ↑
SuperAppDataRepositories ─────────────┘
```

## Layer Responsibilities

### SuperAppAPI (Presentation)
- Controllers: Notes, Workspace, UserProfile, StandardRegistry, Health
- Middleware: GlobalException, JwtValidation, SecurityHeaders
- Extensions: ClaimsPrincipal
- Không chứa business logic

### SuperAppServices (Application)
- Services: NoteService, WorkspaceService
- Interfaces: INoteService, IWorkspaceService
- MappingProfile (AutoMapper)
- Business logic, orchestration

### SuperAppModels (Domain)
- Models: User, Note, Workspace, Tag, etc.
- DTOs: Requests/Responses cho API
- Enums: ApplicationEnums
- Không có dependencies

### SuperAppDataRepositories (Infrastructure)
- Repositories: User, Note, Workspace, StandardRegistry, UserProfile
- ApplicationDbContext (EF Core)
- Configurations: Entity configurations (Fluent API)
- ConnectionFactory: Multi-database support
- Migrations

## Data Access

**Hybrid Strategy:**
- EF Core (80%): Simple CRUD, 2-3 JOINs
- Stored Procedures (20%): Complex aggregations, reporting, bulk ops

**Repositories:** Interface trong `Ins/`, implementation trong `Repositories/`

## Authentication & Authorization

- Local: Email/Phone + Password (BCrypt hashed)
- OAuth: Google
- JWT: Claims-based với 60-min lifetime
- Protection: Middleware-based validation

## Request Flow

```
HTTP Request → Controller → Service → Repository → DB (EF/SP)
                  ↓            ↓          ↓
              Middleware   Business    Data Access
                           Logic
```

## Configuration

- appsettings.json: Non-sensitive configs
- User Secrets: Connection strings, JWT keys, OAuth secrets
- Environment variables: Production overrides

## Error Handling

- Custom exceptions: AppException hierarchy
- Structured logging với Serilog

## Database

**Databases:**
- SuperApp-dev / SuperApp-prod
- UserProfile-dev / UserProfile-prod
- SuperApp_Test / UserProfile_Test

**Tables:** users, workspaces, workspace_members, workspace_items, notes, note_members, note_versions, tags, entity_types, standard_registry

## Development Workflow

**Run:**
```bash
dotnet run --project SuperAppAPI
dotnet watch --project SuperAppAPI
```

**Build & Test:**
```bash
dotnet build
dotnet test
```

**Migrations:**
```bash
dotnet ef migrations add MigrationName --project SuperAppDataRepositories --startup-project SuperAppAPI
dotnet ef database update --project SuperAppDataRepositories --startup-project SuperAppAPI
```

**Secrets:**
```bash
dotnet user-secrets set "Key" "Value" --project SuperAppAPI
```

## Project Dependencies

- SuperAppAPI → SuperAppServices, SuperAppModels, SuperAppDataRepositories
- SuperAppServices → SuperAppModels, SuperAppDataRepositories
- SuperAppDataRepositories → SuperAppModels
- SuperAppModels → (none)

## Best Practices

- Async/await cho I/O operations
- DTOs cho API boundaries
- Repository pattern cho data access
- Dependency injection
- Structured logging
- Parameterized queries

## Security

- Password hashing (BCrypt)
- JWT validation
- HTTPS enforced
- CORS configuration
- SQL injection prevention
- Security headers

## Performance

- Async operations
- Connection pooling
- Query optimization
- AsNoTracking cho read-only
- Pagination

---

**Version:** 2.0
**Last Updated:** November 2025
**See Also:** ARCHITECTURE.md, DATABASE_ACCESS.md, API_DESIGN.md
