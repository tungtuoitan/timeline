# Entity Framework Core Guide

## Overview

EF Core là ORM chính cho SuperApp, sử dụng cho **80% operations** (CRUD, simple queries).

**Packages:**
- Microsoft.EntityFrameworkCore
- Microsoft.EntityFrameworkCore.SqlServer
- Microsoft.EntityFrameworkCore.Tools
- Microsoft.EntityFrameworkCore.Design

## DbContext

**ApplicationDbContext** (SuperAppDataRepositories/Data/ApplicationDbContext.cs):
```csharp
public class ApplicationDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Note> Notes { get; set; }
    public DbSet<Workspace> Workspaces { get; set; }
    public DbSet<Tag> Tags { get; set; }
    // ... other DbSets

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply all configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Auto-update timestamps
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity is ITimestampEntity entity)
            {
                if (entry.State == EntityState.Added)
                    entity.CreatedAt = DateTime.UtcNow;
                entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
```

**Registration** (Program.cs):
```csharp
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
        sqlOptions.CommandTimeout(30);
    });

    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});
```

## Entity Configurations

**Fluent API** (SuperAppDataRepositories/Data/Configurations/):

```csharp
public class NoteConfiguration : IEntityTypeConfiguration<Note>
{
    public void Configure(EntityTypeBuilder<Note> builder)
    {
        // Table
        builder.ToTable("notes");

        // Primary Key
        builder.HasKey(n => n.NoteId);

        // Properties
        builder.Property(n => n.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(n => n.Content)
            .HasColumnType("nvarchar(max)");

        // Relationships
        builder.HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(n => n.Members)
            .WithOne()
            .HasForeignKey(nm => nm.NoteId);

        // Indexes
        builder.HasIndex(n => n.UserId);
        builder.HasIndex(n => n.CreatedAt);

        // Query Filters (Soft Delete)
        builder.HasQueryFilter(n => n.DeletedAt == null);
    }
}
```

## CRUD Operations

### Create
```csharp
// Single
var note = new Note { Title = "New Note", Content = "..." };
_context.Notes.Add(note);
await _context.SaveChangesAsync();

// Multiple
var notes = new List<Note> { note1, note2 };
_context.Notes.AddRange(notes);
await _context.SaveChangesAsync();
```

### Read
```csharp
// By ID
var note = await _context.Notes.FindAsync(id);

// First or default
var note = await _context.Notes
    .FirstOrDefaultAsync(n => n.NoteId == id);

// Where
var notes = await _context.Notes
    .Where(n => n.UserId == userId)
    .ToListAsync();

// Include relations
var note = await _context.Notes
    .Include(n => n.Members)
    .Include(n => n.Versions)
    .FirstOrDefaultAsync(n => n.NoteId == id);
```

### Update
```csharp
// Tracked entity
var note = await _context.Notes.FindAsync(id);
note.Title = "Updated Title";
await _context.SaveChangesAsync();

// Untracked entity
_context.Notes.Update(note);
await _context.SaveChangesAsync();

// Bulk update (EF7+)
await _context.Notes
    .Where(n => n.UserId == userId)
    .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsArchived, true));
```

### Delete
```csharp
// Hard delete
var note = await _context.Notes.FindAsync(id);
_context.Notes.Remove(note);
await _context.SaveChangesAsync();

// Soft delete
note.DeletedAt = DateTime.UtcNow;
await _context.SaveChangesAsync();

// Bulk delete (EF7+)
await _context.Notes
    .Where(n => n.DeletedAt < cutoffDate)
    .ExecuteDeleteAsync();
```

## Query Patterns

### Projection
```csharp
var dtos = await _context.Notes
    .Where(n => n.UserId == userId)
    .Select(n => new NoteResponse
    {
        NoteId = n.NoteId,
        Title = n.Title,
        CreatedAt = n.CreatedAt
    })
    .ToListAsync();
```

### Pagination
```csharp
var pageSize = 20;
var pageNumber = 1;

var notes = await _context.Notes
    .OrderByDescending(n => n.CreatedAt)
    .Skip((pageNumber - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

### AsNoTracking (Read-Only)
```csharp
var notes = await _context.Notes
    .AsNoTracking()
    .Where(n => n.UserId == userId)
    .ToListAsync();
```

### GroupBy
```csharp
var stats = await _context.Notes
    .GroupBy(n => n.UserId)
    .Select(g => new
    {
        UserId = g.Key,
        Count = g.Count(),
        LastCreated = g.Max(n => n.CreatedAt)
    })
    .ToListAsync();
```

## Relationships

### One-to-Many
```csharp
// User has many Notes
builder.HasMany(u => u.Notes)
    .WithOne(n => n.User)
    .HasForeignKey(n => n.UserId);
```

### Many-to-Many (with junction table)
```csharp
// Note <-> User (NoteMember junction)
builder.HasMany(n => n.Members)
    .WithOne()
    .HasForeignKey(nm => nm.NoteId);
```

### Self-Referencing
```csharp
// Tag parent/children
builder.HasOne(t => t.Parent)
    .WithMany(t => t.Children)
    .HasForeignKey(t => t.ParentId);
```

## Migrations

```bash
# Add migration
dotnet ef migrations add InitialCreate \
  --project SuperAppDataRepositories \
  --startup-project SuperAppAPI

# Update database
dotnet ef database update \
  --project SuperAppDataRepositories \
  --startup-project SuperAppAPI

# Generate SQL script
dotnet ef migrations script \
  --project SuperAppDataRepositories \
  --startup-project SuperAppAPI \
  --output migration.sql

# Remove last migration
dotnet ef migrations remove \
  --project SuperAppDataRepositories \
  --startup-project SuperAppAPI
```

## Performance Optimization

### 1. AsNoTracking
```csharp
// Use for read-only queries
var notes = await _context.Notes.AsNoTracking().ToListAsync();
```

### 2. Projection
```csharp
// Select only needed fields
var titles = await _context.Notes
    .Select(n => n.Title)
    .ToListAsync();
```

### 3. SplitQuery
```csharp
// For multiple includes
var workspace = await _context.Workspaces
    .Include(w => w.Members)
    .Include(w => w.Items)
    .AsSplitQuery()
    .FirstOrDefaultAsync();
```

### 4. Compiled Queries
```csharp
private static readonly Func<ApplicationDbContext, int, Task<Note?>> _getNoteById =
    EF.CompileAsyncQuery((ApplicationDbContext context, int id) =>
        context.Notes.FirstOrDefault(n => n.NoteId == id));

// Usage
var note = await _getNoteById(_context, noteId);
```

## Best Practices

### ✅ DO
- Use async methods (.ToListAsync, .SaveChangesAsync)
- Use AsNoTracking for read-only
- Use Include for eager loading
- Project to DTOs
- Use pagination
- Use transactions for multi-operations
- Use Fluent API in separate configuration classes
- Use query filters for soft deletes

### ❌ DON'T
- Use .Result or .Wait()
- Load unnecessary data
- Use N+1 queries (use Include)
- Expose DbContext outside repositories
- Use string concatenation for SQL
- Skip migrations

---

**Version:** 2.0
**Last Updated:** November 2025
**See Also:** DATABASE_ACCESS.md, ARCHITECTURE.md
