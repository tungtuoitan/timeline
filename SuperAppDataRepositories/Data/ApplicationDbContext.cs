using Microsoft.EntityFrameworkCore;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Core Tables
        public DbSet<User> Users { get; set; }
        public DbSet<Tag> Tags { get; set; } // Hashtags (maps to tags_new table)
        public DbSet<EntityType> EntityTypes { get; set; }
        public DbSet<Folder> Folders { get; set; } // Folders (maps to folders table, renamed from old tags)

        // Workspace Tables
        public DbSet<Workspace> Workspaces { get; set; }
        public DbSet<WorkspaceMember> WorkspaceMembers { get; set; }
        public DbSet<WorkspaceRelationshipType> WorkspaceRelationshipTypes { get; set; }
        public DbSet<WorkspaceItem> WorkspaceItems { get; set; }

        // Entity Tables
        public DbSet<Note> Notes { get; set; }
        public DbSet<NoteMember> NoteMembers { get; set; }
        public DbSet<NoteVersion> NoteVersions { get; set; }
        public DbSet<SuperAppModels.Models.FileInfo> Files { get; set; }

        // Tagging System (new polymorphic tagging)
        public DbSet<EntityTag> EntityTags { get; set; } // Polymorphic tagging for all entities

        // ⚠️ DEPRECATED: NoteTag - migrated to EntityTag
        // public DbSet<NoteTag> NoteTags { get; set; }

        // System Configuration Tables
        public DbSet<StandardRegistry> StandardRegistries { get; set; }

        // User Profile Tables
        public DbSet<UserProfile> UserProfiles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply all entity configurations from assembly
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        }

        // Override SaveChanges to handle soft deletes and timestamps
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return await base.SaveChangesAsync(cancellationToken);
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
}