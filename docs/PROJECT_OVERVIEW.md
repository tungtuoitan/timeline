# Project Overview

## Introduction

SuperApp is a .NET 8 backend API using Clean Architecture and CQRS pattern.

## Technology Stack

### Core Technologies

- Framework: .NET 9.0
- Language: C# 12.0
- API: ASP.NET Core Web API 9.0
- ORM: Entity Framework Core (latest) for 80% operations
- Database Access: Hybrid (EF Core + Stored Procedures)

### Key Libraries

- Entity Framework Core: ORM & Data Access
- MediatR: CQRS implementation
- FluentValidation: Input validation
- AutoMapper: Object mapping
- Serilog: Logging
- JWT Bearer: Authentication
- Google OAuth: External auth

### Tools

- IDE: Visual Studio 2022 / VS Code / Rider
- Database: SQL Server
- Version Control: Git
- Package Manager: NuGet

## Architecture

Clean Architecture with CQRS.

Layers:
- API: Presentation (Controllers, Middleware)
- Application: Business (Commands, Queries, DTOs)
- Domain: Core (Entities, Enums)
- Infrastructure: Data (Repositories, DB Access)
- Shared: Utilities (Constants, Helpers)

Dependency Rules: Dependencies point inward; Domain has no dependencies.

CQRS: Commands for writes, Queries for reads.

## Layer Responsibilities

- API: HTTP handling, auth, serialization.
- Application: Business logic, validation, mapping.
- Domain: Business entities and rules (framework-agnostic).
- Infrastructure: Data persistence, repositories, stored procs.
- Shared: Cross-cutting utilities.

## Project Structure

- SuperApp.API: Controllers, Middlewares, Filters.
- SuperApp.Application: Features (Commands/Queries), DTOs, Interfaces.
- SuperApp.Domain: Entities, Enums, ValueObjects.
- SuperApp.Infrastructure: Repositories, StoredProcedures.
- SuperApp.Shared: Constants, Helpers, Results.
- SuperApp.Tests: Unit and Integration tests.

## Data Access

Hybrid: EF Core for simple CRUD; Stored Procs for complex queries.

Repository Pattern: Interfaces in Application, implementations in Infrastructure.

## Authentication

- Local: Email/Phone + Password (BCrypt hashed).
- OAuth: Google.
- JWT: Claims (sub, jti, exp); 60-min lifetime.
- Protection: [Authorize] attribute.

## Request Flow

1. Request → Controller → Command/Query.
2. MediatR → Validator → Handler.
3. Handler → Repository → DB (EF/SP).
4. Map to DTO → Response.

## Design Patterns

- CQRS: Separate Commands/Queries.
- Repository: Abstracts data access.
- Dependency Injection: Constructor injection.
- Mediator: Decouples via MediatR.

## Configuration

- appsettings.json: Non-sensitive.
- User Secrets: Sensitive (connections, keys).
- Environment-specific overrides.

## Error Handling

- Exceptions: AppException hierarchy (NotFound, Validation, etc.).
- Middleware: Global exception handling.
- Validation: DataAnnotations + FluentValidation.

## Logging

- Serilog: Structured logging.
- Levels: Trace to Critical.
- Log: Flows, operations, errors; avoid sensitive data.

## Testing

- Unit: Business logic.
- Integration: DB/repositories.
- Mirror source structure.

## Performance

- Async I/O.
- Indexing, pooling.
- Future: Caching.

## Security

- Hashed passwords, JWT validation.
- Parameterized queries.
- HTTPS, CORS, limits.

## Scalability

- Stateless API.
- Future: Replicas, caching, queues.

## Workflow

- Branches: main, develop, feature.
- Process: Branch → Implement → Test → PR → Merge.

## Dependencies

- API: Application, Shared.
- Application: Domain, Shared.
- Domain: None.
- Infrastructure: Domain, Application (interfaces), Shared.
- Shared: None.

## Next Steps

- New devs: Read SETUP.md, examples.
- Experienced: Review architecture docs.

## Resources

- Internal: SETUP, ARCHITECTURE, etc.
- External: Clean Arch, CQRS, .NET docs.

## Document Info

- Version: 1.0
- Updated: October 2025