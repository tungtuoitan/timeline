# Entity Framework Core Guide

[← Back to Main Documentation](../.github/copilot-instructions.md)

---

## 📚 Table of Contents

1. [Overview](#overview)
2. [Setup & Configuration](#setup--configuration)
3. [DbContext](#dbcontext)
4. [Entity Configurations](#entity-configurations)
5. [Migrations](#migrations)
6. [CRUD Operations](#crud-operations)
7. [Querying Patterns](#querying-patterns)
8. [Relationships & Navigation Properties](#relationships--navigation-properties)
9. [Performance Optimization](#performance-optimization)
10. [Best Practices](#best-practices)
11. [Common Patterns](#common-patterns)

---

## Overview

### What is Entity Framework Core?

Entity Framework Core (EF Core) is a modern object-database mapper for .NET. It enables developers to work with a database using .NET objects, eliminating the need for most data-access code.

### Why EF Core for SuperApp?

| Benefit | Description |
|---------|-------------|
| **Type Safety** | Compile-time checking, IntelliSense support |
| **Productivity** | Less boilerplate code compared to ADO.NET |
| **LINQ Support** | Write queries in C# instead of SQL |
| **Change Tracking** | Automatic detection of entity changes |
| **Migrations** | Version control for database schema |
| **Navigation Properties** | Easy access to related data |

### Current Database Structure

Based on **[DATABASE-CURRENT](DATABASE-CURRENT/INDEX.md)**, we have **10 tables**:

**Core Tables:**
- `users` (5 rows)
- `tags` (9 rows)
- `entity_types` (3 rows)

**Workspace Tables:**
- `workspaces` (5 rows)
- `workspace_members` (17 rows)
- `workspace_relationship_types` (5 rows)
- `workspace_items` (0 rows) - ⚠️ UNIFIED table handling both tag-tag and tag-note relationships

**Entity Tables:**
- `notes` (12 rows)
- `note_members` (12 rows)
- `note_versions` (12 rows)

---

## Setup & Configuration

### 1. Install NuGet Packages

```bash
# Core EF packages
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Tools

# For design-time operations
dotnet add package Microsoft.EntityFrameworkCore.Design
```

### 2. Connection String Configuration

**appsettings.json:**
```json
{
  "ConnectionStrings": {
    "SuperAppConnection": "",
    "UserProfileConnection": ""
  }
}
```

**User Secrets (Development):**
```bash
dotnet user-secrets set "ConnectionStrings:SuperAppConnection" "Server=(localdb)\\mssqllocaldb;Database=SuperApp-dev;Integrated Security=true;MultipleActiveResultSets=true;TrustServerCertificate=true" --project src/SuperApp.API
```

### 3. Register DbContext in DI Container

**Program.cs:**
```csharp
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Register DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("SuperAppConnection");
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
        sqlOptions.CommandTimeout(120); // 2 minutes
    });

    // Enable sensitive data logging in Development only
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// Keep IConnectionFactory for stored procedures
builder.Services.AddScoped<IConnectionFactory, ConnectionFactory>();
```

---

## DbContext

### ApplicationDbContext

**Location:** `SuperApp.Infrastructure/Data/ApplicationDbContext.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using SuperApp.Domain.Entities;

namespace SuperApp.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Core Tables
    public DbSet<User> Users { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<EntityType> EntityTypes { get; set; }

    // Workspace Tables
    public DbSet<Workspace> Workspaces { get; set; }
    public DbSet<WorkspaceMember> WorkspaceMembers { get; set; }
    public DbSet<WorkspaceRelationshipType> WorkspaceRelationshipTypes { get; set; }
    public DbSet<WorkspaceItem> WorkspaceItems { get; set; }

    // Entity Tables
    public DbSet<Note> Notes { get; set; }
    public DbSet<NoteMember> NoteMembers { get; set; }
    public DbSet<NoteVersion> NoteVersions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    // Override SaveChanges to handle soft deletes and timestamps
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity is ITimestampEntity timestampEntity)
            {
                if (entry.State == EntityState.Added)
                {
                    timestampEntity.CreatedAt = DateTime.UtcNow;
                }
                timestampEntity.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}

// Interface for entities with timestamps
public interface ITimestampEntity
{
    DateTime? CreatedAt { get; set; }
    DateTime? UpdatedAt { get; set; }
}
```

---

## Entity Configurations

### Configuration Pattern

Use Fluent API in separate configuration classes instead of data annotations.

### Example: User Entity Configuration

**Location:** `SuperApp.Infrastructure/Data/Configurations/UserConfiguration.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperApp.Domain.Entities;

namespace SuperApp.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Table mapping
        builder.ToTable("users");

        // Primary key
        builder.HasKey(u => u.UserId);
        builder.Property(u => u.UserId)
            .HasColumnName("user_id")
            .ValueGeneratedOnAdd();

        // Properties
        builder.Property(u => u.Username)
            .HasColumnName("username")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(u => u.Email)
            .HasColumnName("email")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(u => u.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(255);

        builder.Property(u => u.FullName)
            .HasColumnName("full_name")
            .HasMaxLength(255);

        builder.Property(u => u.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(u => u.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("GETDATE()");

        builder.Property(u => u.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(u => u.DeletedAt)
            .HasColumnName("deleted_at");

        // Indexes
        builder.HasIndex(u => u.Email)
            .HasDatabaseName("IX_users_email")
            .IsUnique()
            .HasFilter("[deleted_at] IS NULL");

        builder.HasIndex(u => u.Username)
            .HasDatabaseName("IX_users_username")
            .IsUnique()
            .HasFilter("[deleted_at] IS NULL");

        builder.HasIndex(u => u.IsActive)
            .HasDatabaseName("IX_users_is_active")
            .HasFilter("[deleted_at] IS NULL");

        // Soft delete query filter
        builder.HasQueryFilter(u => u.DeletedAt == null);

        // Relationships
        builder.HasMany(u => u.Tags)
            .WithOne(t => t.User)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.Workspaces)
            .WithOne(w => w.User)
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.Notes)
            .WithOne(n => n.User)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

### Example: Note Entity Configuration

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

        builder.Property(n => n.Content)
            .HasColumnName("content")
            .HasColumnType("nvarchar(max)");

        builder.Property(n => n.Slug)
            .HasColumnName("slug")
            .HasMaxLength(255);

        builder.Property(n => n.IsArchived)
            .HasColumnName("is_archived")
            .HasDefaultValue(false);

        builder.Property(n => n.IsPinned)
            .HasColumnName("is_pinned")
            .HasDefaultValue(false);

        builder.Property(n => n.IsFavorite)
            .HasColumnName("is_favorite")
            .HasDefaultValue(false);

        // Indexes
        builder.HasIndex(n => n.UserId)
            .HasDatabaseName("IX_notes_user_id");

        builder.HasIndex(n => n.Name)
            .HasDatabaseName("IX_notes_name");

        builder.HasIndex(n => n.Slug)
            .HasDatabaseName("IX_notes_slug");

        // Relationships
        builder.HasOne(n => n.User)
            .WithMany(u => u.Notes)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(n => n.Members)
            .WithOne(m => m.Note)
            .HasForeignKey(m => m.NoteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(n => n.Versions)
            .WithOne(v => v.Note)
            .HasForeignKey(v => v.NoteId)
            .OnDelete(DeleteBehavior.Cascade);

        // Soft delete filter
        builder.HasQueryFilter(n => n.DeletedAt == null);
    }
}
```

### Example: WorkspaceItem Configuration (UNIFIED table)

```csharp
public class WorkspaceItemConfiguration : IEntityTypeConfiguration<WorkspaceItem>
{
    public void Configure(EntityTypeBuilder<WorkspaceItem> builder)
    {
        builder.ToTable("workspace_items");

        builder.HasKey(wi => wi.ItemId);
        builder.Property(wi => wi.ItemId)
            .HasColumnName("item_id")
            .ValueGeneratedOnAdd();

        builder.Property(wi => wi.WorkspaceId)
            .HasColumnName("workspace_id")
            .IsRequired();

        builder.Property(wi => wi.ParentTagId)
            .HasColumnName("parent_tag_id");

        // ⚠️ UNIFIED: child_type can be 'tag' or 'note'
        builder.Property(wi => wi.ChildType)
            .HasColumnName("child_type")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(wi => wi.ChildId)
            .HasColumnName("child_id")
            .IsRequired();

        builder.Property(wi => wi.Position)
            .HasColumnName("position")
            .HasDefaultValue(0);

        builder.Property(wi => wi.Depth)
            .HasColumnName("depth")
            .HasDefaultValue(0);

        // Unique constraint: one item can only be in one location
        builder.HasIndex(wi => new { wi.WorkspaceId, wi.ParentTagId, wi.ChildType, wi.ChildId })
            .HasDatabaseName("UQ_workspace_items_unique")
            .IsUnique();

        // Indexes
        builder.HasIndex(wi => wi.WorkspaceId)
            .HasDatabaseName("IX_workspace_items_workspace");

        builder.HasIndex(wi => wi.ParentTagId)
            .HasDatabaseName("IX_workspace_items_parent");

        builder.HasIndex(wi => new { wi.ChildType, wi.ChildId })
            .HasDatabaseName("IX_workspace_items_child");

        // Relationships
        builder.HasOne(wi => wi.Workspace)
            .WithMany(w => w.Items)
            .HasForeignKey(wi => wi.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(wi => wi.ParentTag)
            .WithMany()
            .HasForeignKey(wi => wi.ParentTagId)
            .OnDelete(DeleteBehavior.Restrict);

        // Note: ChildTag and ChildNote are handled dynamically based on ChildType
    }
}
```

---

## Migrations

### Create Initial Migration

```bash
# From solution root
dotnet ef migrations add InitialCreate --project src/SuperApp.Infrastructure --startup-project src/SuperApp.API --output-dir Data/Migrations

# Review generated migration in Data/Migrations folder
```

### Apply Migration

```bash
# Update database
dotnet ef database update --project src/SuperApp.Infrastructure --startup-project src/SuperApp.API

# Update to specific migration
dotnet ef database update MigrationName --project src/SuperApp.Infrastructure --startup-project src/SuperApp.API

# Rollback to previous migration
dotnet ef database update PreviousMigrationName --project src/SuperApp.Infrastructure --startup-project src/SuperApp.API
```

### Generate SQL Script

```bash
# Generate SQL script without applying
dotnet ef migrations script --project src/SuperApp.Infrastructure --startup-project src/SuperApp.API --output migration.sql

# Generate script for specific range
dotnet ef migrations script FromMigration ToMigration --project src/SuperApp.Infrastructure --startup-project src/SuperApp.API
```

### Remove Last Migration

```bash
# Remove last migration (if not applied)
dotnet ef migrations remove --project src/SuperApp.Infrastructure --startup-project src/SuperApp.API
```

### Scaffold from Existing Database

```bash
# Generate entities and DbContext from existing database
dotnet ef dbcontext scaffold "Server=(localdb)\\mssqllocaldb;Database=SuperApp-dev;Integrated Security=true;TrustServerCertificate=true" Microsoft.EntityFrameworkCore.SqlServer --project src/SuperApp.Infrastructure --startup-project src/SuperApp.API --output-dir Data/Entities --context-dir Data --context ApplicationDbContext --data-annotations --force
```

---

## CRUD Operations

### Create (Insert)

```csharp
public class NoteRepository : BaseRepository, INoteRepository
{
    private readonly ApplicationDbContext _context;

    public NoteRepository(ApplicationDbContext context, ILogger<NoteRepository> logger)
        : base(context, null, logger)
    {
        _context = context;
    }

    // Simple create
    public async Task<Note> CreateNoteAsync(Note note)
    {
        _context.Notes.Add(note);
        await _context.SaveChangesAsync();
        return note;
    }

    // Create with related entities
    public async Task<Note> CreateNoteWithMembersAsync(Note note, List<int> memberUserIds)
    {
        // Add note
        _context.Notes.Add(note);

        // Add members
        foreach (var userId in memberUserIds)
        {
            _context.NoteMembers.Add(new NoteMember
            {
                NoteId = note.NoteId,
                UserId = userId,
                Role = userId == note.UserId ? "owner" : "editor",
                JoinedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
        return note;
    }

    // Bulk insert
    public async Task CreateNotesAsync(List<Note> notes)
    {
        _context.Notes.AddRange(notes);
        await _context.SaveChangesAsync();
    }
}
```

### Read (Select)

```csharp
// Find by primary key
public async Task<Note?> GetNoteByIdAsync(int id)
{
    return await _context.Notes.FindAsync(id);
}

// Simple query
public async Task<List<Note>> GetUserNotesAsync(int userId)
{
    return await _context.Notes
        .Where(n => n.UserId == userId)
        .OrderByDescending(n => n.CreatedAt)
        .ToListAsync();
}

// Query with includes (eager loading)
public async Task<Note?> GetNoteWithDetailsAsync(int id)
{
    return await _context.Notes
        .Include(n => n.Members)
            .ThenInclude(m => m.User)
        .Include(n => n.Versions.OrderByDescending(v => v.CreatedAt).Take(5))
        .FirstOrDefaultAsync(n => n.NoteId == id);
}

// No-tracking query (read-only, better performance)
public async Task<List<Note>> GetNotesReadOnlyAsync(int userId)
{
    return await _context.Notes
        .AsNoTracking()
        .Where(n => n.UserId == userId)
        .ToListAsync();
}

// Projection to DTO (best performance)
public async Task<List<NoteDto>> GetNotesDtoAsync(int userId)
{
    return await _context.Notes
        .Where(n => n.UserId == userId)
        .Select(n => new NoteDto
        {
            NoteId = n.NoteId,
            Name = n.Name,
            Content = n.Content,
            MemberCount = n.Members.Count,
            VersionCount = n.Versions.Count,
            CreatedAt = n.CreatedAt
        })
        .ToListAsync();
}

// Pagination
public async Task<(List<Note> Notes, int TotalCount)> GetNotesPaginatedAsync(
    int userId,
    int pageNumber,
    int pageSize)
{
    var query = _context.Notes
        .Where(n => n.UserId == userId);

    var totalCount = await query.CountAsync();

    var notes = await query
        .OrderByDescending(n => n.CreatedAt)
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    return (notes, totalCount);
}
```

### Update

```csharp
// Update entity
public async Task<Note> UpdateNoteAsync(Note note)
{
    _context.Notes.Update(note);
    await _context.SaveChangesAsync();
    return note;
}

// Update specific fields
public async Task UpdateNoteNameAsync(int noteId, string newName)
{
    var note = await _context.Notes.FindAsync(noteId);
    if (note != null)
    {
        note.Name = newName;
        await _context.SaveChangesAsync();
    }
}

// Bulk update with ExecuteUpdate (EF Core 7+)
public async Task ArchiveOldNotesAsync(DateTime cutoffDate)
{
    await _context.Notes
        .Where(n => n.CreatedAt < cutoffDate && !n.IsArchived)
        .ExecuteUpdateAsync(setters => setters
            .SetProperty(n => n.IsArchived, true)
            .SetProperty(n => n.UpdatedAt, DateTime.UtcNow));
}
```

### Delete

```csharp
// Hard delete
public async Task DeleteNoteAsync(int id)
{
    var note = await _context.Notes.FindAsync(id);
    if (note != null)
    {
        _context.Notes.Remove(note);
        await _context.SaveChangesAsync();
    }
}

// Soft delete
public async Task SoftDeleteNoteAsync(int id)
{
    var note = await _context.Notes.FindAsync(id);
    if (note != null)
    {
        note.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }
}

// Bulk delete with ExecuteDelete (EF Core 7+)
public async Task DeleteArchivedNotesAsync(DateTime olderThan)
{
    await _context.Notes
        .Where(n => n.IsArchived && n.UpdatedAt < olderThan)
        .ExecuteDeleteAsync();
}
```

---

## Querying Patterns

### Dynamic Filtering

```csharp
public async Task<List<Note>> SearchNotesAsync(NoteSearchCriteria criteria)
{
    var query = _context.Notes.AsQueryable();

    // Apply filters dynamically
    if (criteria.UserId.HasValue)
        query = query.Where(n => n.UserId == criteria.UserId.Value);

    if (!string.IsNullOrEmpty(criteria.SearchText))
        query = query.Where(n => n.Name.Contains(criteria.SearchText)
                              || n.Content.Contains(criteria.SearchText));

    if (criteria.IsArchived.HasValue)
        query = query.Where(n => n.IsArchived == criteria.IsArchived.Value);

    if (criteria.IsPinned.HasValue)
        query = query.Where(n => n.IsPinned == criteria.IsPinned.Value);

    if (criteria.CreatedAfter.HasValue)
        query = query.Where(n => n.CreatedAt >= criteria.CreatedAfter.Value);

    // Apply sorting
    query = criteria.SortBy switch
    {
        "name" => criteria.SortDescending
            ? query.OrderByDescending(n => n.Name)
            : query.OrderBy(n => n.Name),
        "created" => criteria.SortDescending
            ? query.OrderByDescending(n => n.CreatedAt)
            : query.OrderBy(n => n.CreatedAt),
        _ => query.OrderByDescending(n => n.CreatedAt)
    };

    return await query.ToListAsync();
}
```

### GroupBy and Aggregations

```csharp
public async Task<List<UserNoteStatistics>> GetNoteStatisticsByUserAsync()
{
    return await _context.Notes
        .GroupBy(n => n.UserId)
        .Select(g => new UserNoteStatistics
        {
            UserId = g.Key,
            TotalNotes = g.Count(),
            ArchivedNotes = g.Count(n => n.IsArchived),
            PinnedNotes = g.Count(n => n.IsPinned),
            LatestNoteDate = g.Max(n => n.CreatedAt)
        })
        .ToListAsync();
}
```

### Exists/Any Queries

```csharp
public async Task<bool> UserHasNotesAsync(int userId)
{
    return await _context.Notes.AnyAsync(n => n.UserId == userId);
}

public async Task<bool> NoteNameExistsAsync(int userId, string name, int? excludeNoteId = null)
{
    var query = _context.Notes
        .Where(n => n.UserId == userId && n.Name == name);

    if (excludeNoteId.HasValue)
        query = query.Where(n => n.NoteId != excludeNoteId.Value);

    return await query.AnyAsync();
}
```

### Raw SQL Queries

```csharp
// Raw SQL query
public async Task<List<Note>> GetNotesRawSqlAsync(int userId)
{
    return await _context.Notes
        .FromSqlRaw("SELECT * FROM notes WHERE user_id = {0} AND deleted_at IS NULL", userId)
        .ToListAsync();
}

// Stored procedure with EF Core
public async Task<List<Note>> GetNotesViaProcedureAsync(int userId)
{
    return await _context.Notes
        .FromSqlRaw("EXEC usp_s_user_notes @userId = {0}", userId)
        .ToListAsync();
}
```

---

## Relationships & Navigation Properties

### One-to-Many

```csharp
// User → Notes
public class User
{
    public int UserId { get; set; }
    public string Username { get; set; }

    // Navigation property
    public ICollection<Note> Notes { get; set; } = new List<Note>();
}

public class Note
{
    public int NoteId { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; }

    // Navigation property
    public User User { get; set; }
}

// Query with navigation
var user = await _context.Users
    .Include(u => u.Notes)
    .FirstOrDefaultAsync(u => u.UserId == userId);
```

### Many-to-Many (through junction table)

```csharp
// Note ←→ User (through NoteMembers)
public class Note
{
    public int NoteId { get; set; }
    public string Name { get; set; }

    public ICollection<NoteMember> Members { get; set; } = new List<NoteMember>();
}

public class NoteMember
{
    public int MemberId { get; set; }
    public int NoteId { get; set; }
    public int UserId { get; set; }
    public string Role { get; set; }

    public Note Note { get; set; }
    public User User { get; set; }
}

// Query
var note = await _context.Notes
    .Include(n => n.Members)
        .ThenInclude(m => m.User)
    .FirstOrDefaultAsync(n => n.NoteId == noteId);
```

### Self-Referencing (Hierarchical)

```csharp
// Tag with parent-child relationship
public class Tag
{
    public int TagId { get; set; }
    public int? ParentId { get; set; }
    public string Name { get; set; }

    // Navigation properties
    public Tag Parent { get; set; }
    public ICollection<Tag> Children { get; set; } = new List<Tag>();
}

// Configuration
public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.HasOne(t => t.Parent)
            .WithMany(t => t.Children)
            .HasForeignKey(t => t.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

// Query hierarchy
var tagTree = await _context.Tags
    .Where(t => t.ParentId == null) // Root tags
    .Include(t => t.Children)
        .ThenInclude(c => c.Children)
    .ToListAsync();
```

---

## Performance Optimization

### 1. Use AsNoTracking for Read-Only Queries

```csharp
// ✅ Good - No tracking overhead
var notes = await _context.Notes
    .AsNoTracking()
    .Where(n => n.UserId == userId)
    .ToListAsync();

// ❌ Bad - Tracking overhead for read-only data
var notes = await _context.Notes
    .Where(n => n.UserId == userId)
    .ToListAsync();
```

### 2. Project to DTOs Instead of Full Entities

```csharp
// ✅ Good - Only select needed columns
var noteDtos = await _context.Notes
    .Where(n => n.UserId == userId)
    .Select(n => new NoteListDto
    {
        NoteId = n.NoteId,
        Name = n.Name,
        CreatedAt = n.CreatedAt
    })
    .ToListAsync();

// ❌ Bad - Loads entire entity with all columns
var notes = await _context.Notes
    .Where(n => n.UserId == userId)
    .ToListAsync();
```

### 3. Use Pagination for Large Result Sets

```csharp
// ✅ Good - Load data in pages
var notes = await _context.Notes
    .Where(n => n.UserId == userId)
    .OrderByDescending(n => n.CreatedAt)
    .Skip((pageNumber - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

### 4. Avoid N+1 Query Problem

```csharp
// ❌ Bad - N+1 queries (1 for notes + N for each note's members)
var notes = await _context.Notes.ToListAsync();
foreach (var note in notes)
{
    var members = note.Members; // Lazy load - separate query!
}

// ✅ Good - Single query with JOIN
var notes = await _context.Notes
    .Include(n => n.Members)
    .ToListAsync();
```

### 5. Use Compiled Queries for Repeated Queries

```csharp
// Define compiled query
private static readonly Func<ApplicationDbContext, int, Task<Note?>> GetNoteByIdCompiled =
    EF.CompileAsyncQuery((ApplicationDbContext context, int id) =>
        context.Notes.FirstOrDefault(n => n.NoteId == id));

// Use compiled query
public async Task<Note?> GetNoteByIdAsync(int id)
{
    return await GetNoteByIdCompiled(_context, id);
}
```

### 6. Use SplitQuery for Multiple Includes

```csharp
// ✅ Good - Separate queries for each include (better for large collections)
var workspace = await _context.Workspaces
    .Include(w => w.Members)
    .Include(w => w.Items)
    .AsSplitQuery()
    .FirstOrDefaultAsync(w => w.WorkspaceId == id);

// ⚠️ Default - Single query with JOINs (can be slow with large collections)
var workspace = await _context.Workspaces
    .Include(w => w.Members)
    .Include(w => w.Items)
    .FirstOrDefaultAsync(w => w.WorkspaceId == id);
```

---

## Best Practices

### 1. Always Use Async Methods

```csharp
// ✅ Good
var notes = await _context.Notes.ToListAsync();

// ❌ Bad - Blocks thread
var notes = _context.Notes.ToList();
```

### 2. Dispose DbContext Properly

```csharp
// ✅ Good - DI handles disposal
public class NoteRepository
{
    private readonly ApplicationDbContext _context;

    public NoteRepository(ApplicationDbContext context)
    {
        _context = context;
    }
}

// ❌ Bad - Manual disposal (error-prone)
using var context = new ApplicationDbContext(options);
```

### 3. Use Transactions for Multiple Operations

```csharp
public async Task TransferNoteOwnershipAsync(int noteId, int newOwnerId)
{
    using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
        // Update note owner
        var note = await _context.Notes.FindAsync(noteId);
        note.UserId = newOwnerId;

        // Update members
        var ownerMember = await _context.NoteMembers
            .FirstOrDefaultAsync(m => m.NoteId == noteId && m.Role == "owner");
        ownerMember.Role = "editor";

        var newOwnerMember = await _context.NoteMembers
            .FirstOrDefaultAsync(m => m.NoteId == noteId && m.UserId == newOwnerId);
        newOwnerMember.Role = "owner";

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

### 4. Implement Soft Deletes with Query Filters

```csharp
// In entity configuration
builder.HasQueryFilter(e => e.DeletedAt == null);

// Automatically filters out soft-deleted records
var notes = await _context.Notes.ToListAsync(); // Only active notes

// Explicitly include soft-deleted records
var allNotes = await _context.Notes
    .IgnoreQueryFilters()
    .ToListAsync(); // Includes soft-deleted notes
```

### 5. Use Value Converters for Enums

```csharp
// Entity
public class Note
{
    public int NoteId { get; set; }
    public NoteStatus Status { get; set; } // Enum
}

public enum NoteStatus
{
    Draft,
    Published,
    Archived
}

// Configuration
builder.Property(n => n.Status)
    .HasConversion<string>() // Store as string in database
    .HasMaxLength(50);
```

---

## Common Patterns

### Repository Pattern Example

```csharp
public class NoteRepository : BaseRepository, INoteRepository
{
    private readonly ApplicationDbContext _context;

    public NoteRepository(
        ApplicationDbContext context,
        IConnectionFactory connectionFactory,
        ILogger<NoteRepository> logger)
        : base(context, connectionFactory, logger)
    {
        _context = context;
    }

    // EF Core operations
    public async Task<Note?> GetByIdAsync(int id)
    {
        return await _context.Notes
            .Include(n => n.Members)
            .FirstOrDefaultAsync(n => n.NoteId == id);
    }

    public async Task<List<Note>> GetAllAsync(int userId)
    {
        return await _context.Notes
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<Note> CreateAsync(Note note)
    {
        _context.Notes.Add(note);
        await _context.SaveChangesAsync();
        return note;
    }

    public async Task<Note> UpdateAsync(Note note)
    {
        _context.Notes.Update(note);
        await _context.SaveChangesAsync();
        return note;
    }

    public async Task DeleteAsync(int id)
    {
        var note = await _context.Notes.FindAsync(id);
        if (note != null)
        {
            _context.Notes.Remove(note);
            await _context.SaveChangesAsync();
        }
    }

    // Stored procedure for complex query
    public async Task<NoteStatisticsDto> GetStatisticsAsync(int userId)
    {
        return await ExecuteStoredProcedure(
            "usp_get_note_statistics",
            addParameters: (cmd) =>
            {
                cmd.Parameters.Add(new SqlParameter("@userId", userId));
                return Task.CompletedTask;
            },
            mapResult: MapToSingle<NoteStatisticsDto>
        );
    }
}
```

### Unit of Work Pattern

```csharp
public interface IUnitOfWork : IDisposable
{
    INoteRepository Notes { get; }
    IWorkspaceRepository Workspaces { get; }
    ITagRepository Tags { get; }

    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
        Notes = new NoteRepository(context);
        Workspaces = new WorkspaceRepository(context);
        Tags = new TagRepository(context);
    }

    public INoteRepository Notes { get; }
    public IWorkspaceRepository Workspaces { get; }
    public ITagRepository Tags { get; }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
```

---

[← Back to Main Documentation](../.github/copilot-instructions.md) | [Next: Database Access →](DATABASE_ACCESS.md)
