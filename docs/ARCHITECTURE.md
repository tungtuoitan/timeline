
---
# Architecture Guide Summary

## Table of Contents
1. [Architecture Overview](#architecture-overview)
2. [Clean Architecture Principles](#clean-architecture-principles)
3. [Project Structure](#project-structure)
4. [Layer Responsibilities](#layer-responsibilities)
5. [Dependency Rules](#dependency-rules)
6. [CQRS Pattern](#cqrs-pattern)
7. [Data Flow](#data-flow)
8. [Benefits of This Architecture](#benefits-of-this-architecture)

## Architecture Overview

SuperApp sử dụng Clean Architecture với CQRS cho business logic.

### Architecture Diagram
- SuperApp.API (Presentation): Controllers, Middleware, Program.cs.
- SuperApp.Application (Business Logic): Commands, Queries, Handlers, DTOs, Validators.
- SuperApp.Domain (Domain): Entities, Value Objects, Enums (không phụ thuộc).
- SuperApp.Infrastructure (Data Access): Repositories, Database Access, External Services.
- SuperApp.Shared (Utilities): Constants, Helpers, Results.

Dependencies: Outer layers depend inward (API/Application/Infrastructure → Domain).

```
┌─────────────────────────────────────────────────────────────┐
│                     SuperApp.API                             │
│                  (Presentation Layer)                        │
│  Controllers, Middleware, Filters, Program.cs               │
└────────────────────────┬────────────────────────────────────┘
                         │ depends on ↓
┌─────────────────────────────────────────────────────────────┐
│                 SuperApp.Application                         │
│                  (Business Logic Layer)                      │
│  Commands, Queries, Handlers, DTOs, Validators              │
└────────────────────────┬────────────────────────────────────┘
                         │ depends on ↓
┌─────────────────────────────────────────────────────────────┐
│                   SuperApp.Domain                            │
│                    (Domain Layer)                            │
│  Entities, Value Objects, Enums (No dependencies!)          │
└─────────────────────────────────────────────────────────────┘
                         ↑ depends on
┌─────────────────────────────────────────────────────────────┐
│               SuperApp.Infrastructure                        │
│                 (Data Access Layer)                          │
│  Repositories, Database Access, External Services           │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                   SuperApp.Shared                            │
│                 (Cross-Cutting Utilities)                    │
│  Constants, Helpers, Results, Extensions                    │
└─────────────────────────────────────────────────────────────┘
```

## Clean Architecture Principles

1. **Independence of Frameworks**: Business logic không phụ thuộc framework bên ngoài (có thể thay EF bằng Dapper).

2. **Testability**: Test business rules mà không cần UI, DB, server.

3. **Independence of UI**: Thay UI (web sang mobile) mà không ảnh hưởng business logic.

4. **Independence of Database**: Thay DB (SQL Server sang PostgreSQL) dễ dàng.

5. **Dependency Rule**: Dependencies hướng inward (outer → inner). Inner không biết outer.

## Project Structure

### Folder Hierarchy Summary
- src/
  - SuperApp.API/: Controllers (Notes, Auth,...), Middlewares (Exception, ValidateToken), Filters, Extensions, appsettings, Program.cs.
  - SuperApp.Application/: Common (Interfaces, Behaviors, Exceptions, Mappings), Features (Notes/Auth/UserProfile/StandardRegistry với Commands/Queries), DTOs (Requests/Responses).
  - SuperApp.Domain/: Entities (Note, User,...), Enums (AuthType,...), ValueObjects (Email,...).
  - SuperApp.Infrastructure/: Data (IConnectionFactory), Repositories (Base, Note,...), StoredProcedures.
  - SuperApp.Shared/: Constants, Helpers (Password, Jwt), Results.
- tests/
  - SuperApp.Tests.Unit/: Application (Notes/Auth), Domain.
  - SuperApp.Tests.Integration/: Infrastructure, API.

## Layer Responsibilities

### 1. SuperApp.API (Presentation)
- Xử lý HTTP requests/responses, route đến handlers.
- Controllers mỏng, delegate MediatR.
- Middleware: exception handling, auth.
- Validation input binding.
- Swagger documentation.
- Không: business logic, direct DB access, complex validation.

### 2. SuperApp.Application (Business Logic)
- Implement business logic, workflows.
- CQRS: Commands/Queries, Handlers.
- Validators (FluentValidation).
- DTOs, business rules.
- Mapping (AutoMapper).
- Không: HTTP concerns, direct SQL, framework-specific.

### 3. SuperApp.Domain (Domain)
- Core entities, value objects, enums.
- Domain exceptions, invariants.
- Không: reference other layers, DB/HTTP/infra concerns.

### 4. SuperApp.Infrastructure (Data Access)
- Hybrid: EF Core (80% simple ops) + Stored Procedures (20% complex).
- DbContext config, entity configs (Fluent API).
- Repositories: implement interfaces, execute SP cho complex queries.
- Data mapping, external integrations.
- Không: business logic, HTTP/presentation.

### 5. SuperApp.Shared (Utilities)
- Constants, helpers (PasswordHash), extensions, Result types.

## Dependency Rules

### Allowed
- API → Application → Domain.
- API → Infrastructure (DI registration).
- Infrastructure → Application (interfaces), Domain.
- Shared → All layers.

### Forbidden
- Domain → Any.
- Application → API, Infrastructure impl.
- Infrastructure → API.

### DI Registration (Program.cs)
- AddDbContext<ApplicationDbContext> với SQL Server.
- Scoped repos (INoteRepository → NoteRepository).
- MediatR từ assemblies.
- AutoMapper, Validators.
- Pipeline behaviors: Validation, Logging.

## CQRS Pattern

- **Commands**: Change state, return void/result (Create/Update/Delete).
- **Queries**: Read data, no change (Get/Search).
- Handlers execute logic, call repos.
- Validators cho commands.

## Data Flow

### Request Flow
1. HTTP Request → Controller.
2. Controller tạo Command/Query → MediatR dispatch.
3. Pipeline: Validation → Logging → Handler.
4. Handler: business logic → Repository (interface).
5. Repository: EF/SP → DB.
6. DB return → Map entity → DTO.
7. Handler → Controller → HTTP Response.

## Benefits of This Architecture

- **Testability**: Mock repos, test handlers independent.
- **Maintainability**: Changes localized, clear boundaries.
- **Flexibility**: Swap DB/UI/providers.
- **Scalability**: CQRS optimize read/write, microservices ready.
- **Collaboration**: Layers riêng biệt, interfaces contracts.

## Example Flow Diagram

```
┌──────────────┐
│   Browser    │
└──────┬───────┘
       │ POST /api/notes
       ↓
┌──────────────────────┐
│  NotesController     │
│  CreateNote(request) │
└──────┬───────────────┘
       │ Send(CreateNoteCommand)
       ↓
┌──────────────────────────────┐
│  MediatR Pipeline            │
│  ├─ ValidationBehavior       │ ← FluentValidation
│  ├─ LoggingBehavior          │ ← Serilog
│  └─ CreateNoteCommandHandler │
└──────┬───────────────────────┘
       │ CreateNoteAsync(note)
       ↓
┌──────────────────────────────┐
│  NoteRepository               │
│  ✅ EF Core: _context.Notes   │
│     .Add(note)                │
│     .SaveChangesAsync()       │
└──────┬───────────────────────┘
       │ INSERT INTO notes
       ↓
┌──────────────────────────────┐
│  SQL Server                   │
│  Database (SuperApp-dev)      │
└──────┬───────────────────────┘
       │ Returns note with ID
       ↓
┌──────────────────────┐
│  AutoMapper          │
│  Map<NoteDto>(note)  │
└──────┬───────────────┘
       │ Returns NoteDto
       ↓
┌──────────────────────┐
│  Controller          │
│  Ok(noteDto)         │
└──────┬───────────────┘
       │ 201 Created
       ↓
┌──────────────┐
│   Browser    │
└──────────────┘