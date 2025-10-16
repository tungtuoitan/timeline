# Architecture Guide

[← Back to Main Documentation](../.copilot-instructions.md.md)

---

## Table of Contents
1. [Architecture Overview](#architecture-overview)
2. [Clean Architecture Principles](#clean-architecture-principles)
3. [Project Structure](#project-structure)
4. [Layer Responsibilities](#layer-responsibilities)
5. [Dependency Rules](#dependency-rules)
6. [CQRS Pattern](#cqrs-pattern)
7. [Data Flow](#data-flow)

---

## Architecture Overview

SuperApp follows **Clean Architecture** principles with a **CQRS (Command Query Responsibility Segregation)** pattern for business logic.

### Architecture Diagram

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

---

## Clean Architecture Principles

### 1. Independence of Frameworks
The business logic doesn't depend on external frameworks. We can swap out Entity Framework for Dapper, or ADO.NET for stored procedures (which we're using) without changing business logic.

### 2. Testability
Business rules can be tested without the UI, database, web server, or any external element.

### 3. Independence of UI
The UI can change easily without changing the rest of the system. A Web UI could be replaced with a mobile app without changing business logic.

### 4. Independence of Database
Business rules are not bound to the database. You can swap SQL Server for PostgreSQL, MongoDB, or any other database.

### 5. The Dependency Rule
**Source code dependencies must point inward toward higher-level policies.**

```
Outer Layers (Low-level details) → Inner Layers (High-level policies)

Infrastructure → Application → Domain
     API       → Application → Domain
```

**Nothing in an inner circle can know anything about something in an outer circle.**

---

## Project Structure

### Complete Folder Hierarchy

```
SuperApp/
│
├── src/
│   ├── SuperApp.API/                              # 🌐 Presentation Layer
│   │   ├── Controllers/
│   │   │   ├── NotesController.cs
│   │   │   ├── AuthController.cs
│   │   │   ├── UserProfileController.cs
│   │   │   └── StandardRegistryController.cs
│   │   ├── Middlewares/
│   │   │   ├── GlobalExceptionMiddleware.cs
│   │   │   └── ValidateTokenMiddleware.cs
│   │   ├── Filters/
│   │   │   └── ValidationFilter.cs
│   │   ├── Extensions/
│   │   │   └── ServiceCollectionExtensions.cs
│   │   ├── Properties/
│   │   │   └── launchSettings.json
│   │   ├── appsettings.json
│   │   ├── appsettings.Development.json
│   │   ├── Program.cs
│   │   └── Startup.cs
│   │
│   ├── SuperApp.Application/                      # 💼 Business Logic Layer
│   │   ├── Common/
│   │   │   ├── Interfaces/
│   │   │   │   ├── IRepository.cs
│   │   │   │   ├── IConnectionFactory.cs
│   │   │   │   └── ICurrentUserService.cs
│   │   │   ├── Behaviors/
│   │   │   │   ├── ValidationBehavior.cs
│   │   │   │   ├── LoggingBehavior.cs
│   │   │   │   └── PerformanceBehavior.cs
│   │   │   ├── Exceptions/
│   │   │   │   ├── AppException.cs
│   │   │   │   ├── NotFoundException.cs
│   │   │   │   ├── ValidationException.cs
│   │   │   │   ├── UnauthorizedException.cs
│   │   │   │   └── ForbiddenException.cs
│   │   │   └── Mappings/
│   │   │       └── MappingProfile.cs
│   │   ├── Features/
│   │   │   ├── Notes/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── CreateNote/
│   │   │   │   │   │   ├── CreateNoteCommand.cs
│   │   │   │   │   │   ├── CreateNoteCommandHandler.cs
│   │   │   │   │   │   └── CreateNoteValidator.cs
│   │   │   │   │   ├── UpdateNote/
│   │   │   │   │   │   ├── UpdateNoteCommand.cs
│   │   │   │   │   │   ├── UpdateNoteCommandHandler.cs
│   │   │   │   │   │   └── UpdateNoteValidator.cs
│   │   │   │   │   └── DeleteNote/
│   │   │   │   │       ├── DeleteNoteCommand.cs
│   │   │   │   │       └── DeleteNoteCommandHandler.cs
│   │   │   │   └── Queries/
│   │   │   │       ├── GetNotes/
│   │   │   │       │   ├── GetNotesQuery.cs
│   │   │   │       │   └── GetNotesQueryHandler.cs
│   │   │   │       └── GetNoteById/
│   │   │   │           ├── GetNoteByIdQuery.cs
│   │   │   │           └── GetNoteByIdQueryHandler.cs
│   │   │   ├── Auth/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── Login/
│   │   │   │   │   ├── Register/
│   │   │   │   │   └── GoogleLogin/
│   │   │   │   └── Queries/
│   │   │   ├── UserProfile/
│   │   │   │   ├── Commands/
│   │   │   │   └── Queries/
│   │   │   └── StandardRegistry/
│   │   │       ├── Commands/
│   │   │       └── Queries/
│   │   └── DTOs/
│   │       ├── Requests/
│   │       │   ├── CreateNoteRequest.cs
│   │       │   ├── UpdateNoteRequest.cs
│   │       │   ├── LoginRequest.cs
│   │       │   └── RegisterRequest.cs
│   │       └── Responses/
│   │           ├── NoteDto.cs
│   │           ├── LoginResponse.cs
│   │           └── UserProfileDto.cs
│   │
│   ├── SuperApp.Domain/                           # 🏛️ Domain Layer
│   │   ├── Entities/
│   │   │   ├── Note.cs
│   │   │   ├── User.cs
│   │   │   ├── UserProfile.cs
│   │   │   └── StandardRegistry.cs
│   │   ├── Enums/
│   │   │   ├── AuthType.cs
│   │   │   ├── NoteStatus.cs
│   │   │   └── UserRole.cs
│   │   └── ValueObjects/
│   │       ├── Email.cs
│   │       └── PhoneNumber.cs
│   │
│   ├── SuperApp.Infrastructure/                   # 🗄️ Data Access Layer
│   │   ├── Data/
│   │   │   ├── IConnectionFactory.cs
│   │   │   └── ConnectionFactory.cs
│   │   ├── Repositories/
│   │   │   ├── BaseRepository.cs
│   │   │   ├── NoteRepository.cs
│   │   │   ├── AuthRepository.cs
│   │   │   ├── UserProfileRepository.cs
│   │   │   └── StandardRegistryRepository.cs
│   │   ├── StoredProcedures/
│   │   │   └── StoredProcedures.cs
│   │   └── Extensions/
│   │       ├── DbDataReaderMapper.cs
│   │       └── DataTableExtensions.cs
│   │
│   └── SuperApp.Shared/                           # 🔧 Shared Utilities
│       ├── Constants/
│       │   ├── AppConstants.cs
│       │   ├── ErrorMessages.cs
│       │   └── DatabaseConstants.cs
│       ├── Helpers/
│       │   ├── PasswordHelper.cs
│       │   └── JwtHelper.cs
│       └── Results/
│           └── Result.cs
│
└── tests/
    ├── SuperApp.Tests.Unit/                       # 🧪 Unit Tests
    │   ├── Application/
    │   │   ├── Notes/
    │   │   │   ├── Commands/
    │   │   │   └── Queries/
    │   │   └── Auth/
    │   └── Domain/
    │       └── Entities/
    └── SuperApp.Tests.Integration/                # 🔗 Integration Tests
        ├── Infrastructure/
        │   └── Repositories/
        └── API/
            └── Controllers/
```

---

## Layer Responsibilities

### 1. SuperApp.API (Presentation Layer)

**Purpose:** Handle HTTP requests, route to appropriate handlers, return HTTP responses

**Responsibilities:**
- Controllers (thin, delegate to MediatR)
- HTTP concerns (headers, status codes, content negotiation)
- Middleware (exception handling, authentication)
- Request validation (input binding)
- Swagger/API documentation

**What it SHOULD do:**
```csharp
[ApiController]
[Route("api/[controller]")]
public class NotesController : ControllerBase
{
    private readonly IMediator _mediator;

    public NotesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<List<NoteDto>>> GetNotes(
        [FromQuery] bool getAll,
        [FromQuery] string? searchText)
    {
        var query = new GetNotesQuery(getAll, searchText);
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}
```

**What it should NOT do:**
- ❌ Business logic
- ❌ Direct database access
- ❌ Complex validation (use FluentValidation in Application layer)

---

### 2. SuperApp.Application (Business Logic Layer)

**Purpose:** Implement business logic, orchestrate workflows

**Responsibilities:**
- Commands and Queries (CQRS)
- Handlers (execute business logic)
- Validators (FluentValidation)
- DTOs (data transfer objects)
- Business rules and workflows
- Mapping (AutoMapper profiles)

**What it SHOULD do:**
```csharp
public class CreateNoteCommandHandler : IRequestHandler<CreateNoteCommand, NoteDto>
{
    private readonly INoteRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateNoteCommandHandler> _logger;

    public CreateNoteCommandHandler(
        INoteRepository repository,
        IMapper mapper,
        ILogger<CreateNoteCommandHandler> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<NoteDto> Handle(CreateNoteCommand request, CancellationToken cancellationToken)
    {
        // Business logic here
        var note = new Note
        {
            Name = request.Name,
            Description = request.Description,
            CreatedDate = DateTime.UtcNow
        };

        await _repository.CreateNoteAsync(note);
        
        _logger.LogInformation("Note created with ID {NoteId}", note.NoteId);

        return _mapper.Map<NoteDto>(note);
    }
}
```

**What it should NOT do:**
- ❌ HTTP concerns (status codes, headers)
- ❌ Direct SQL queries (use repositories)
- ❌ Framework-specific code

---

### 3. SuperApp.Domain (Domain Layer)

**Purpose:** Core business entities and rules (no dependencies!)

**Responsibilities:**
- Domain entities (business objects)
- Value objects (immutable types)
- Enums
- Domain exceptions
- Business invariants

**What it SHOULD do:**
```csharp
public class Note
{
    public int NoteId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Tags { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }

    // Domain logic
    public void UpdateContent(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Note name cannot be empty");

        Name = name;
        Description = description;
        ModifiedDate = DateTime.UtcNow;
    }
}
```

**What it should NOT do:**
- ❌ Reference any other layer
- ❌ Database concerns (EF attributes, etc.)
- ❌ HTTP or presentation concerns
- ❌ Infrastructure concerns

---

### 4. SuperApp.Infrastructure (Data Access Layer)

**Purpose:** Implement data access and external service integration using **Hybrid approach (EF Core + Stored Procedures)**

**Responsibilities:**
- **DbContext configuration** (EF Core)
- **Entity configurations** (Fluent API)
- **Repository implementations** (EF Core + SP)
- **Database connection management** (for SPs)
- **Stored procedure execution** (complex queries only)
- **Data mapping** (automatic with EF Core)
- **External API integrations**

**Key Components:**
```
SuperApp.Infrastructure/
├── Data/
│   ├── ApplicationDbContext.cs           # EF Core DbContext
│   ├── Configurations/                    # Entity configurations
│   │   ├── UserConfiguration.cs
│   │   ├── NoteConfiguration.cs
│   │   ├── WorkspaceConfiguration.cs
│   │   └── ...
│   ├── Migrations/                        # EF Core migrations
│   └── IConnectionFactory.cs              # For stored procedures only
│
├── Repositories/
│   ├── BaseRepository.cs                  # Hybrid: EF Core + SP support
│   ├── NoteRepository.cs
│   ├── WorkspaceRepository.cs
│   └── ...
│
└── StoredProcedures/
    └── StoredProcedures.cs                # Only complex queries
```

**What it SHOULD do:**

**✅ EF Core for Simple Operations (80%):**
```csharp
public class NoteRepository : BaseRepository, INoteRepository
{
    private readonly ApplicationDbContext _context;
    private readonly IConnectionFactory _connectionFactory; // For SPs only

    public NoteRepository(
        ApplicationDbContext context,
        IConnectionFactory connectionFactory,
        ILogger<NoteRepository> logger)
        : base(context, connectionFactory, logger)
    {
        _context = context;
    }

    // EF Core: Simple CRUD
    public async Task<Note?> GetNoteByIdAsync(int id)
    {
        return await _context.Notes
            .Include(n => n.Members)
            .Include(n => n.Versions)
            .FirstOrDefaultAsync(n => n.NoteId == id);
    }

    // EF Core: Query with filters
    public async Task<List<Note>> GetUserNotesAsync(int userId, string? searchText)
    {
        var query = _context.Notes
            .Where(n => n.UserId == userId && !n.IsArchived);

        if (!string.IsNullOrEmpty(searchText))
            query = query.Where(n => n.Name.Contains(searchText));

        return await query.ToListAsync();
    }

    // EF Core: Create
    public async Task<Note> CreateNoteAsync(Note note)
    {
        _context.Notes.Add(note);
        await _context.SaveChangesAsync();
        return note;
    }

    // EF Core: Update
    public async Task<Note> UpdateNoteAsync(Note note)
    {
        _context.Notes.Update(note);
        await _context.SaveChangesAsync();
        return note;
    }

    // Stored Procedure: Complex reporting (20%)
    public async Task<WorkspaceStatisticsDto> GetWorkspaceStatisticsAsync(int workspaceId)
    {
        return await ExecuteStoredProcedure(
            "usp_get_workspace_statistics",
            addParameters: (cmd) =>
            {
                cmd.Parameters.Add(new SqlParameter("@workspace_id", workspaceId));
                return Task.CompletedTask;
            },
            mapResult: MapToSingle<WorkspaceStatisticsDto>
        );
    }
}
```

**Entity Configuration Example:**
```csharp
public class NoteConfiguration : IEntityTypeConfiguration<Note>
{
    public void Configure(EntityTypeBuilder<Note> builder)
    {
        builder.ToTable("notes");

        builder.HasKey(n => n.NoteId);
        builder.Property(n => n.NoteId)
            .HasColumnName("note_id")
            .ValueGeneratedOnAdd();

        builder.Property(n => n.Name)
            .HasColumnName("name")
            .HasMaxLength(500)
            .IsRequired();

        // Relationships
        builder.HasOne(n => n.User)
            .WithMany(u => u.Notes)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(n => n.Members)
            .WithOne(m => m.Note)
            .HasForeignKey(m => m.NoteId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(n => n.UserId);
        builder.HasIndex(n => n.Slug).IsUnique();

        // Soft delete filter
        builder.HasQueryFilter(n => n.DeletedAt == null);
    }
}
```

**What it should NOT do:**
- ❌ Business logic
- ❌ HTTP concerns
- ❌ Presentation logic
- ❌ Use stored procedures for simple CRUD (use EF Core instead)

---

### 5. SuperApp.Shared (Cross-Cutting Utilities)

**Purpose:** Shared utilities used across multiple layers

**Responsibilities:**
- Constants
- Helper methods
- Extension methods
- Result types
- Common utilities

**What it SHOULD do:**
```csharp
public static class PasswordHelper
{
    public static string HashPassword(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        var hash = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
        byte[] hashBytes = hash.GetBytes(32);
        
        byte[] combined = new byte[48];
        Array.Copy(salt, 0, combined, 0, 16);
        Array.Copy(hashBytes, 0, combined, 16, 32);
        
        return Convert.ToBase64String(combined);
    }
}
```

---

## Dependency Rules

### Allowed Dependencies

```
API → Application → Domain
API → Infrastructure (only for DI registration)
Infrastructure → Application (interfaces only)
Infrastructure → Domain
Shared → Can be referenced by all layers
```

### Forbidden Dependencies

```
Domain → ❌ Any other layer
Application → ❌ API
Application → ❌ Infrastructure (implementation)
Infrastructure → ❌ API
```

### Dependency Injection Registration

All dependencies are registered in `Program.cs`:

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Database - EF Core DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("SuperAppConnection");
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
        sqlOptions.CommandTimeout(120);
    });

    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// Infrastructure
builder.Services.AddScoped<IConnectionFactory, ConnectionFactory>(); // For SPs only
builder.Services.AddScoped<INoteRepository, NoteRepository>();
builder.Services.AddScoped<IWorkspaceRepository, WorkspaceRepository>();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();

// Application
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(CreateNoteCommand).Assembly));
builder.Services.AddAutoMapper(typeof(MappingProfile).Assembly);
builder.Services.AddValidatorsFromAssembly(typeof(CreateNoteValidator).Assembly);

// Add MediatR behaviors
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
```

---

## CQRS Pattern

### Command Query Separation

**Commands:** Change state, return void or simple result
**Queries:** Read data, never change state

```
Commands (Write)              Queries (Read)
      ↓                             ↓
CreateNoteCommand           GetNotesQuery
UpdateNoteCommand           GetNoteByIdQuery
DeleteNoteCommand           SearchNotesQuery
      ↓                             ↓
  Handlers                      Handlers
      ↓                             ↓
 Repository                    Repository
```

### Command Example

```csharp
// Command
public record CreateNoteCommand(string Name, string? Description) : IRequest<NoteDto>;

// Handler
public class CreateNoteCommandHandler : IRequestHandler<CreateNoteCommand, NoteDto>
{
    private readonly INoteRepository _repository;
    private readonly IMapper _mapper;

    public async Task<NoteDto> Handle(CreateNoteCommand request, CancellationToken cancellationToken)
    {
        var note = new Note
        {
            Name = request.Name,
            Description = request.Description,
            CreatedDate = DateTime.UtcNow
        };

        await _repository.CreateNoteAsync(note);
        return _mapper.Map<NoteDto>(note);
    }
}

// Validator
public class CreateNoteValidator : AbstractValidator<CreateNoteCommand>
{
    public CreateNoteValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);
    }
}
```

### Query Example

```csharp
// Query
public record GetNotesQuery(bool GetAll, string? SearchText) : IRequest<List<NoteDto>>;

// Handler
public class GetNotesQueryHandler : IRequestHandler<GetNotesQuery, List<NoteDto>>
{
    private readonly INoteRepository _repository;
    private readonly IMapper _mapper;

    public async Task<List<NoteDto>> Handle(GetNotesQuery request, CancellationToken cancellationToken)
    {
        var notes = await _repository.GetNotesAsync(request.GetAll, request.SearchText);
        return _mapper.Map<List<NoteDto>>(notes);
    }
}
```

---

## Data Flow

### Request Flow (Complete Journey)

```
1. HTTP Request
   ↓
2. Controller receives request
   ↓
3. Controller creates Command/Query
   ↓
4. MediatR dispatches to Handler
   ↓
5. Validation Behavior validates request (FluentValidation)
   ↓
6. Logging Behavior logs request
   ↓
7. Handler executes business logic
   ↓
8. Handler calls Repository (via interface)
   ↓
9. Repository executes stored procedure
   ↓
10. Database returns data
   ↓
11. Repository maps to entity
   ↓
12. Handler maps entity to DTO
   ↓
13. Handler returns DTO to Controller
   ↓
14. Controller returns HTTP Response
```

### Example Flow Diagram

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
```

---

## Benefits of This Architecture

### ✅ Testability
- Test business logic without database or HTTP
- Mock repositories easily
- Test handlers independently

### ✅ Maintainability
- Changes localized to single layer
- Clear responsibility boundaries
- Easy to understand and navigate

### ✅ Flexibility
- Swap database providers easily
- Change UI without touching business logic
- Add new features without breaking existing code

### ✅ Scalability
- Independent layer scaling
- CQRS allows read/write optimization
- Clear separation enables microservices migration

### ✅ Team Collaboration
- Multiple developers can work on different layers
- Clear contracts via interfaces
- Reduced merge conflicts

---

[← Back to Main Documentation](../.copilot-instructions.md.md) | [Next: Database Access →](DATABASE_ACCESS.md)