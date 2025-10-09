# Project Overview

## Introduction

SuperApp is a modern backend API built with .NET 8, following Clean Architecture principles and implementing the CQRS (Command Query Responsibility Segregation) pattern. This document provides a comprehensive overview of the project's technology stack, architecture, and core design principles.

---

## Technology Stack

### Core Technologies

| Component | Technology | Version | Purpose |
|-----------|-----------|---------|---------|
| **Framework** | .NET | 8.0 | Primary application framework |
| **Language** | C# | 12.0 | Programming language |
| **API Type** | ASP.NET Core Web API | 8.0 | RESTful API implementation |
| **Database Access** | ADO.NET | Built-in | Direct database access via stored procedures |

### Key Libraries & Packages

| Library | Purpose | Documentation |
|---------|---------|---------------|
| **MediatR** | CQRS pattern implementation | Decouples requests from handlers |
| **FluentValidation** | Input validation | Validates DTOs and commands |
| **AutoMapper** | Object mapping | Maps between entities and DTOs |
| **Serilog** | Structured logging | Application logging and diagnostics |
| **JWT Bearer** | Authentication | Token-based authentication |
| **Google OAuth** | External authentication | Social login integration |

### Development Tools

- **IDE:** Visual Studio 2022 / VS Code / JetBrains Rider
- **Database:** Microsoft SQL Server
- **Version Control:** Git
- **Package Manager:** NuGet

---

## Architecture Pattern

SuperApp follows **Clean Architecture** (also known as Onion Architecture or Hexagonal Architecture) combined with the **CQRS pattern**.

### Clean Architecture Principles

```
+---------------------------------------+
|   SuperApp.API (Presentation)         |  <- Controllers, Middleware, Filters
+---------------------------------------+
|   SuperApp.Application (Business)     |  <- Commands, Queries, DTOs, Validators
+---------------------------------------+
|   SuperApp.Domain (Core)              |  <- Entities, Value Objects, Enums
+---------------------------------------+
|   SuperApp.Infrastructure (Data)      |  <- Repositories, Database Access
+---------------------------------------+
|   SuperApp.Shared (Utilities)         |  <- Constants, Helpers, Results
+---------------------------------------+
```

### Dependency Rules

The architecture enforces strict dependency rules:

1. **API** depends on **Application**
2. **Application** depends on **Domain**
3. **Infrastructure** depends on **Domain**
4. **Domain** has NO dependencies on other layers (pure business logic)
5. **Application** references **Infrastructure** via interfaces only

**Key Principle:** Dependencies point inward. The Domain layer is completely isolated from external concerns.

### CQRS Pattern

The application separates read and write operations:

- **Commands** - Modify state (Create, Update, Delete)
- **Queries** - Read data (Get, List, Search)

Benefits:
- Clear separation of concerns
- Optimized queries for reads
- Better scalability
- Easier to test

---

## Layer Responsibilities

### 1. API Layer (SuperApp.API)

**Purpose:** HTTP interface and request/response handling

**Contains:**
- Controllers (REST endpoints)
- Middleware (exception handling, logging)
- Filters (validation, authentication)
- Dependency injection configuration
- Startup configuration

**Responsibilities:**
- Route HTTP requests to appropriate handlers
- Serialize/deserialize JSON
- Handle authentication and authorization
- Return appropriate HTTP status codes
- Apply global filters and middleware

**Does NOT contain:**
- Business logic
- Data access logic
- Domain models

### 2. Application Layer (SuperApp.Application)

**Purpose:** Business logic and orchestration

**Contains:**
- Commands and Command Handlers (writes)
- Queries and Query Handlers (reads)
- DTOs (Data Transfer Objects)
- Validators (FluentValidation)
- AutoMapper profiles
- MediatR pipeline behaviors
- Application exceptions
- Repository interfaces

**Responsibilities:**
- Implement business rules
- Coordinate between repositories
- Validate inputs
- Map between domain and DTOs
- Handle application-specific exceptions

**Does NOT contain:**
- HTTP concerns (controllers, middleware)
- Database implementation details
- Direct SQL queries

### 3. Domain Layer (SuperApp.Domain)

**Purpose:** Core business entities and rules

**Contains:**
- Entities (business objects)
- Value Objects (immutable descriptors)
- Enums (domain-specific enumerations)
- Domain exceptions
- Domain interfaces (if needed)

**Responsibilities:**
- Define the core business model
- Encapsulate business rules within entities
- Represent the problem domain

**Does NOT contain:**
- Any dependencies on other layers
- Database concerns
- HTTP concerns
- Application-specific logic

**Key Principle:** The Domain layer should be framework-agnostic and reusable in any application type (Web API, Desktop, Console).

### 4. Infrastructure Layer (SuperApp.Infrastructure)

**Purpose:** External concerns and data persistence

**Contains:**
- Repository implementations
- Database connection factory
- Stored procedure definitions
- Data mapping utilities
- External service integrations

**Responsibilities:**
- Execute stored procedures
- Map database results to domain entities
- Manage database connections
- Implement data access patterns
- Handle database-specific exceptions

**Does NOT contain:**
- Business logic
- Domain rules
- HTTP concerns

### 5. Shared Layer (SuperApp.Shared)

**Purpose:** Cross-cutting utilities

**Contains:**
- Constants
- Helper classes
- Extension methods
- Result wrappers
- Common utilities

**Responsibilities:**
- Provide reusable utilities
- Define application-wide constants
- Implement common patterns

---

## Project Structure

### Complete Folder Hierarchy

```
SuperApp/
|
+-- SuperApp.API/                       # Presentation Layer
|   +-- Controllers/                    # REST API endpoints
|   |   +-- NotesController.cs
|   |   +-- AuthController.cs
|   |   +-- UserProfileController.cs
|   +-- Middlewares/                    # HTTP pipeline components
|   |   +-- GlobalExceptionMiddleware.cs
|   |   +-- ValidateTokenMiddleware.cs
|   +-- Filters/                        # Action filters
|   |   +-- ValidationFilter.cs
|   +-- Extensions/                     # Service configuration
|   |   +-- ServiceCollectionExtensions.cs
|   +-- Program.cs                      # Application entry point
|   +-- appsettings.json                # Configuration (non-secrets)
|   +-- appsettings.Development.json    # Development config
|
+-- SuperApp.Application/               # Business Logic Layer
|   +-- Common/
|   |   +-- Interfaces/                 # Repository contracts
|   |   |   +-- IRepository.cs
|   |   |   +-- IConnectionFactory.cs
|   |   +-- Behaviors/                  # MediatR pipeline
|   |   |   +-- ValidationBehavior.cs
|   |   |   +-- LoggingBehavior.cs
|   |   +-- Exceptions/                 # Application exceptions
|   |   |   +-- AppException.cs
|   |   |   +-- NotFoundException.cs
|   |   |   +-- ValidationException.cs
|   |   |   +-- UnauthorizedException.cs
|   |   +-- Mappings/                   # AutoMapper profiles
|   |       +-- MappingProfile.cs
|   +-- Features/                       # Feature-based organization
|   |   +-- Notes/
|   |   |   +-- Commands/               # Write operations
|   |   |   |   +-- CreateNote/
|   |   |   |       +-- CreateNoteCommand.cs
|   |   |   |       +-- CreateNoteCommandHandler.cs
|   |   |   |       +-- CreateNoteValidator.cs
|   |   |   +-- Queries/                # Read operations
|   |   |       +-- GetNotes/
|   |   |           +-- GetNotesQuery.cs
|   |   |           +-- GetNotesQueryHandler.cs
|   |   +-- Auth/
|   |   +-- UserProfile/
|   |   +-- StandardRegistry/
|   +-- DTOs/                           # Data Transfer Objects
|       +-- Requests/
|       |   +-- CreateNoteRequest.cs
|       |   +-- LoginRequest.cs
|       +-- Responses/
|           +-- NoteDto.cs
|           +-- LoginResponse.cs
|
+-- SuperApp.Domain/                    # Core Domain Layer
|   +-- Entities/                       # Business entities
|   |   +-- Note.cs
|   |   +-- User.cs
|   |   +-- UserProfile.cs
|   +-- Enums/                          # Domain enumerations
|   |   +-- AuthType.cs
|   +-- ValueObjects/                   # Immutable value types
|
+-- SuperApp.Infrastructure/            # Data Access Layer
|   +-- Data/                           # Connection management
|   |   +-- IConnectionFactory.cs
|   |   +-- ConnectionFactory.cs
|   +-- Repositories/                   # Data access implementations
|   |   +-- BaseRepository.cs
|   |   +-- NoteRepository.cs
|   |   +-- AuthRepository.cs
|   |   +-- UserProfileRepository.cs
|   |   +-- StandardRegistryRepository.cs
|   +-- StoredProcedures/               # SP definitions
|   |   +-- StoredProcedures.cs
|   +-- Extensions/                     # Data utilities
|       +-- DbDataReaderMapper.cs
|       +-- DataTableExtensions.cs
|
+-- SuperApp.Shared/                    # Utilities Layer
|   +-- Constants/                      # Application constants
|   |   +-- AppConstants.cs
|   |   +-- ErrorMessages.cs
|   +-- Helpers/                        # Utility classes
|   |   +-- PasswordHelper.cs
|   +-- Results/                        # Result wrappers
|       +-- Result.cs
|
+-- SuperApp.Tests/                     # Test Projects
    +-- Unit/                           # Unit tests
    |   +-- Application/
    |   +-- Domain/
    +-- Integration/                    # Integration tests
        +-- Infrastructure/
```

---

## Data Access Strategy

### Why ADO.NET + Stored Procedures?

SuperApp uses ADO.NET with stored procedures instead of Entity Framework for the following reasons:

**Advantages:**
1. **Performance** - Direct SQL execution with no ORM overhead
2. **Control** - Full control over SQL queries and optimization
3. **Stored Procedures** - Business logic can be encapsulated in the database
4. **Legacy Integration** - Works well with existing database schemas
5. **Minimal Overhead** - Lightweight data access layer

**Trade-offs:**
- More manual code for data mapping
- No automatic migrations
- Requires database schema management

### Repository Pattern

All data access goes through repositories that inherit from `BaseRepository`:

```
BaseRepository (Infrastructure)
    |
    v
NoteRepository : BaseRepository, INoteRepository
    |
    v
INoteRepository (Application - Interface)
```

This pattern provides:
- Consistent error handling
- Connection management
- Parameter handling
- Result mapping
- Logging

---

## Authentication & Authorization

### Authentication Methods

SuperApp supports two authentication methods:

1. **Local Authentication**
   - Email/password or phone/password
   - Passwords hashed using BCrypt
   - Returns JWT token on successful login

2. **OAuth (Google)**
   - Sign in with Google
   - No password required
   - Returns JWT token after OAuth flow

### JWT Token Structure

Tokens contain these claims:
- `sub` - User identifier (email or phone)
- `jti` - Unique token ID (GUID)
- `exp` - Expiration timestamp

**Token Lifetime:** 60 minutes (configurable)

### Endpoint Protection

Controllers/actions are protected using:
```csharp
[Authorize]  // Require authentication
[AllowAnonymous]  // Allow public access
```

---

## Request Flow

### Typical Request Flow

```
1. HTTP Request arrives at API Controller
   |
   v
2. ModelState validation (automatic)
   |
   v
3. Controller creates Command/Query
   |
   v
4. MediatR dispatches to Handler
   |
   v
5. ValidationBehavior validates (FluentValidation)
   |
   v
6. Handler executes business logic
   |
   v
7. Repository accesses database (stored procedure)
   |
   v
8. Results mapped to DTOs
   |
   v
9. Response returned to client
```

### Example: Create Note Flow

```
POST /api/notes
{
  "name": "My Note",
  "description": "Note content"
}
   |
   v
NotesController.CreateNote()
   |
   v
MediatR.Send(CreateNoteCommand)
   |
   v
CreateNoteValidator.Validate() (checkmark)
   |
   v
CreateNoteCommandHandler.Handle()
   |
   v
NoteRepository.CreateNoteAsync()
   |
   v
[dbo].[usp_i_Note] stored procedure
   |
   v
Returns: NoteDto
```

---

## Key Design Patterns

### 1. CQRS (Command Query Responsibility Segregation)

**Commands** - Modify state:
```csharp
public record CreateNoteCommand(string Name, string Description) : IRequest<NoteDto>;
```

**Queries** - Read data:
```csharp
public record GetNotesQuery(bool GetAll, string? SearchText) : IRequest<List<NoteDto>>;
```

### 2. Repository Pattern

Abstracts data access:
```csharp
public interface INoteRepository
{
    Task<List<Note>> GetNotesAsync(bool getAll, string? searchText);
    Task<Note?> GetNoteByIdAsync(int id);
    Task<int> CreateNoteAsync(Note note);
}
```

### 3. Dependency Injection

All dependencies injected via constructor:
```csharp
public class CreateNoteCommandHandler
{
    private readonly INoteRepository _repository;
    private readonly IMapper _mapper;
    
    public CreateNoteCommandHandler(
        INoteRepository repository,
        IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }
}
```

### 4. Mediator Pattern

MediatR decouples controllers from handlers:
```csharp
// Controller
var result = await _mediator.Send(new CreateNoteCommand(name, desc));

// Handler automatically invoked
public class CreateNoteCommandHandler : IRequestHandler<CreateNoteCommand, NoteDto>
```

---

## Configuration Management

### Application Settings

**appsettings.json** - Non-sensitive configuration:
```json
{
  "Logging": {
    "LogLevel": { "Default": "Information" }
  },
  "Jwt": {
    "Issuer": "SuperApp",
    "Audience": "SuperAppUsers",
    "ExpiryMinutes": 60
  }
}
```

**User Secrets** - Sensitive configuration:
```json
{
  "ConnectionStrings": {
    "SuperAppConnection": "Server=...;Database=...;Password=..."
  },
  "Jwt": {
    "Key": "your-secret-key-here"
  },
  "OAuth": {
    "ClientSecret": "google-client-secret"
  }
}
```

### Environment-Specific Settings

- `appsettings.Development.json` - Development overrides
- `appsettings.Production.json` - Production overrides
- User Secrets - Local development secrets
- Environment Variables - Production secrets

---

## Error Handling Strategy

### Exception Hierarchy

```
AppException (base)
|
+-- NotFoundException (404)
+-- ValidationException (400)
+-- UnauthorizedException (401)
+-- ForbiddenException (403)
```

### Global Exception Middleware

All exceptions caught by `GlobalExceptionMiddleware`:
- Logs error details
- Returns consistent error response
- Maps exception types to HTTP status codes

### Validation Strategy

**Two layers of validation:**

1. **Model Validation** - Automatic via DataAnnotations
2. **Business Validation** - FluentValidation in handlers

---

## Logging Strategy

### Structured Logging with Serilog

```csharp
_logger.LogInformation(
    "User {UserId} created note {NoteId}",
    userId,
    noteId
);
```

### Log Levels

- **Trace** - Very detailed debugging
- **Debug** - Internal debugging information
- **Information** - General application flow
- **Warning** - Unexpected but recoverable events
- **Error** - Error events that don't stop the application
- **Critical** - Critical failures requiring immediate attention

### What to Log

**DO log:**
- Request/response flow
- Business operations
- Authentication attempts
- Errors and exceptions

**DON'T log:**
- Passwords or credentials
- Personal identifiable information (PII)
- Credit card numbers
- Session tokens

---

## Testing Strategy

### Test Pyramid

```
        /\
       /  \      E2E Tests (few)
      /____\
     /      \    Integration Tests (some)
    /________\
   /          \  Unit Tests (many)
  /____________\
```

### Test Types

1. **Unit Tests** - Test business logic in isolation
2. **Integration Tests** - Test database and repository layer
3. **E2E Tests** (future) - Test complete API flows

### Test Organization

Tests mirror source code structure:
```
SuperApp.Tests.Unit/
  +-- Application/
  |   +-- Features/
  |       +-- Notes/
  |           +-- CreateNoteCommandHandlerTests.cs
  +-- Domain/
      +-- Entities/
          +-- NoteTests.cs
```

---

## Performance Considerations

### Database Performance

- Use stored procedures for complex queries
- Implement proper indexing (database level)
- Use connection pooling (automatic with ADO.NET)
- Set appropriate command timeouts

### API Performance

- Async/await everywhere for I/O operations
- Minimal allocations in hot paths
- DTOs prevent over-fetching
- Response compression enabled

### Caching Strategy (Future)

- Distributed cache for frequently accessed data
- Memory cache for static data
- Cache invalidation on updates

---

## Security Best Practices

### Authentication Security

- Passwords hashed with BCrypt (cost factor: 12)
- JWT tokens signed and validated
- Tokens expire after 60 minutes
- Refresh tokens (future enhancement)

### API Security

- HTTPS only in production
- CORS configured for allowed origins
- Rate limiting (future enhancement)
- Request size limits

### Data Security

- Parameterized queries prevent SQL injection
- Input validation on all endpoints
- No sensitive data in logs
- Connection strings in secrets

---

## Scalability Considerations

### Current Architecture

The application is designed to scale horizontally:
- Stateless API (JWT tokens, no server sessions)
- Database connection pooling
- Async operations throughout

### Future Enhancements

- Read replicas for queries
- Caching layer (Redis)
- Message queue for background jobs
- API gateway for routing

---

## Development Workflow

### Feature Development Process

1. Create feature branch from `develop`
2. Implement feature following Clean Architecture
3. Write unit tests for handlers
4. Write integration tests for repositories
5. Update documentation
6. Create pull request
7. Code review
8. Merge to `develop`

### Branching Strategy

```
main (production)
  |
  v
develop (integration)
  |
  v
feature/feature-name (active development)
```

---

## Dependencies Between Layers

### What Each Layer Can Reference

**SuperApp.API** can reference:
- SuperApp.Application
- SuperApp.Shared

**SuperApp.Application** can reference:
- SuperApp.Domain
- SuperApp.Shared

**SuperApp.Domain** can reference:
- Nothing (pure business logic)

**SuperApp.Infrastructure** can reference:
- SuperApp.Domain
- SuperApp.Application (interfaces only)
- SuperApp.Shared

**SuperApp.Shared** can reference:
- Nothing (pure utilities)

---

## Common Scenarios

### Adding a New Feature

See detailed guide in CODE_EXAMPLES.md

### Modifying Existing Feature

1. Update Command/Query if needed
2. Update Validator if validation changes
3. Update Handler for business logic changes
4. Update Repository if data access changes
5. Update DTOs if response structure changes
6. Update tests

### Troubleshooting

Common issues and solutions in TROUBLESHOOTING.md

---

## Next Steps

### New Developers

1. Read SETUP.md to configure your environment
2. Study CODE_EXAMPLES.md for practical examples
3. Review CODING_STANDARDS.md for style guidelines
4. Start with a small feature to learn the patterns

### Experienced Developers

1. Review ARCHITECTURE.md for design decisions
2. Check DATABASE_ACCESS.md for data patterns
3. Read API_DESIGN.md for endpoint conventions
4. Contribute to documentation improvements

---

## Additional Resources

### Internal Documentation

- [Setup Guide](SETUP.md)
- [Architecture Guide](ARCHITECTURE.md)
- [Coding Standards](CODING_STANDARDS.md)
- [Database Access](DATABASE_ACCESS.md)
- [API Design](API_DESIGN.md)
- [Code Examples](CODE_EXAMPLES.md)

### External Resources

- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [CQRS Pattern](https://docs.microsoft.com/en-us/azure/architecture/patterns/cqrs)
- [.NET Documentation](https://docs.microsoft.com/en-us/dotnet/)
- [ASP.NET Core Best Practices](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/best-practices)

---

## Document Information

- **Version:** 1.0
- **Last Updated:** October 2025
- **Maintained By:** Development Team
- **Review Schedule:** Quarterly

For questions or suggestions, please contact the development team or create a documentation issue.