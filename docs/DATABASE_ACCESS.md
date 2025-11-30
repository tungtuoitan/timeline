# Database Access Guide

## Hybrid Strategy

SuperApp sử dụng **80% EF Core** + **20% Stored Procedures**

| Use Case | Tech | Reason |
|----------|------|--------|
| Simple CRUD (1 table) | EF Core | Type-safe, maintainable |
| 2-3 JOINs | EF Core | LINQ readable |
| Complex (5+ JOINs, CTEs) | SP | Performance optimized |
| Aggregations/Reports | SP | SQL control |
| Bulk operations | SP | Efficiency |

**Decision Tree:**
```
Simple CRUD? → EF Core
2-3 JOINs?   → EF Core
Complex?     → Stored Procedure
```

## EF Core Setup

**DbContext** (SuperAppDataRepositories/Data/ApplicationDbContext.cs):
```csharp
public class ApplicationDbContext : DbContext
{
    public DbSet<Note> Notes { get; set; }
    public DbSet<Workspace> Workspaces { get; set; }
    public DbSet<User> Users { get; set; }
    // ... other DbSets

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
```

**Registration** (SuperAppAPI/Program.cs):
```csharp
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
```

## Repository Pattern

**Interface** (SuperAppDataRepositories/Ins/INoteRepository.cs):
```csharp
public interface INoteRepository
{
    Task<Note?> GetByIdAsync(int id);
    Task<List<Note>> GetNotesAsync(string userEmail);
    Task<Note> CreateAsync(Note note);
    Task UpdateAsync(Note note);
    Task DeleteAsync(int id);
}
```

**Implementation** (SuperAppDataRepositories/Repositories/NoteRepository.cs):
```csharp
public class NoteRepository : INoteRepository
{
    private readonly ApplicationDbContext _context;

    public async Task<Note?> GetByIdAsync(int id)
    {
        return await _context.Notes
            .Include(n => n.Members)
            .FirstOrDefaultAsync(n => n.NoteId == id);
    }

    public async Task<Note> CreateAsync(Note note)
    {
        _context.Notes.Add(note);
        await _context.SaveChangesAsync();
        return note;
    }
}
```

## EF Core Query Patterns

### Basic Queries
```csharp
// Find by ID
var note = await _context.Notes.FindAsync(id);

// Where + ToList
var notes = await _context.Notes
    .Where(n => n.UserId == userId)
    .ToListAsync();

// Single or default
var note = await _context.Notes
    .FirstOrDefaultAsync(n => n.NoteId == id);
```

### Eager Loading
```csharp
var workspace = await _context.Workspaces
    .Include(w => w.Members)
    .Include(w => w.Items)
        .ThenInclude(i => i.Tag)
    .FirstOrDefaultAsync(w => w.WorkspaceId == id);
```

### Projection
```csharp
var noteDtos = await _context.Notes
    .Where(n => n.UserId == userId)
    .Select(n => new NoteResponse
    {
        NoteId = n.NoteId,
        Title = n.Title,
        Content = n.Content
    })
    .ToListAsync();
```

### Pagination
```csharp
var notes = await _context.Notes
    .Skip((pageNumber - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

### No Tracking (read-only)
```csharp
var notes = await _context.Notes
    .AsNoTracking()
    .ToListAsync();
```

## Connection Management

**IConnectionFactory** (SuperAppDataRepositories/Data/IConnectionFactory.cs):
```csharp
public interface IConnectionFactory
{
    Task<SqlConnection> CreateSuperAppConnectionAsync();
    Task<SqlConnection> CreateUserProfileConnectionAsync();
}
```

## Stored Procedures

**Naming Convention:** `usp_[prefix]_[Entity]`

Prefixes:
- `s_` = SELECT
- `i_` = INSERT
- `u_` = UPDATE
- `d_` = DELETE
- `iu_` = INSERT/UPDATE (upsert)

Examples:
- `usp_s_Notes` - Get notes
- `usp_iu_Note` - Create/Update note
- `usp_d_Notes` - Delete notes

**Execution:**
```csharp
public async Task<List<Note>> GetNotesViaSP(string userEmail)
{
    using var conn = await _connectionFactory.CreateSuperAppConnectionAsync();
    using var cmd = new SqlCommand("usp_s_Notes", conn)
    {
        CommandType = CommandType.StoredProcedure
    };

    cmd.Parameters.Add(new SqlParameter("@iv_UserEmail", userEmail));

    using var reader = await cmd.ExecuteReaderAsync();
    var notes = new List<Note>();

    while (await reader.ReadAsync())
    {
        notes.Add(new Note
        {
            NoteId = reader.GetInt32("note_id"),
            Title = reader.GetString("title"),
            Content = reader.GetString("content")
        });
    }

    return notes;
}
```

## Database Configuration

**Databases:**
- Dev: `SuperApp-dev`, `UserProfile-dev`
- Prod: `SuperApp-prod`, `UserProfile-prod`
- Test: `SuperApp_Test`, `UserProfile_Test`

**Connection Strings** (User Secrets):
```json
{
  "ConnectionStrings": {
    "SuperAppConnection": "Server=.;Database=SuperApp-dev;Trusted_Connection=True;",
    "UserProfileConnection": "Server=.;Database=UserProfile-dev;Trusted_Connection=True;"
  }
}
```

## Entity Configurations

**Fluent API** (SuperAppDataRepositories/Data/Configurations/):
```csharp
public class NoteConfiguration : IEntityTypeConfiguration<Note>
{
    public void Configure(EntityTypeBuilder<Note> builder)
    {
        builder.ToTable("notes");
        builder.HasKey(n => n.NoteId);
        builder.Property(n => n.Title).HasMaxLength(200).IsRequired();
        builder.Property(n => n.Content).HasColumnType("nvarchar(max)");

        builder.HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId);
    }
}
```

## Migrations

```bash
# Add migration
dotnet ef migrations add MigrationName \
  --project SuperAppDataRepositories \
  --startup-project SuperAppAPI

# Update database
dotnet ef database update \
  --project SuperAppDataRepositories \
  --startup-project SuperAppAPI

# Generate SQL script
dotnet ef migrations script \
  --project SuperAppDataRepositories \
  --startup-project SuperAppAPI
```

## Best Practices

### ✅ DO
- Use async/await
- Use `using` for DbContext/connections
- Parameterize all queries
- Use AsNoTracking for read-only
- Project to DTOs in queries
- Use transactions for multi-operations
- Log SQL errors
- Use IConnectionFactory
- Index frequently queried columns

### ❌ DON'T
- Use string interpolation in SQL
- Call .Result or .Wait()
- Load unnecessary data
- Skip error handling
- Hardcode connection strings
- Expose DbContext outside repositories
- Use N+1 queries (use Include)

## Error Handling

```csharp
try
{
    await _context.SaveChangesAsync();
}
catch (DbUpdateConcurrencyException ex)
{
    _logger.LogError(ex, "Concurrency conflict");
    throw new ConflictException("Resource was modified");
}
catch (DbUpdateException ex)
{
    _logger.LogError(ex, "Database update failed");
    throw new DataAccessException("Failed to save changes");
}
```

## Performance Tips

1. **Use AsNoTracking** cho read-only queries
2. **Projection**: Select chỉ fields cần thiết
3. **Pagination**: Luôn dùng Skip/Take
4. **Avoid N+1**: Dùng Include
5. **SplitQuery**: Cho multiple includes
6. **Compiled Queries**: Cho repeated queries
7. **Indexing**: Index foreign keys, search columns

---

**Version:** 2.0
**Last Updated:** November 2025
**See Also:** EF_CORE_GUIDE.md, ARCHITECTURE.md
