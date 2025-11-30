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

        // Core Tables (urm schema)
        public DbSet<User> Users { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }

        // Workspace Tables (ws schema)
        public DbSet<Workspace> Workspaces { get; set; }
        public DbSet<Folder> Folders { get; set; }
        public DbSet<SuperAppModels.Models.FileInfo> Files { get; set; }
        public DbSet<WorkspaceItem> WorkspaceItems { get; set; }

        // Entity Tables (dbo schema)
        public DbSet<Note> Notes { get; set; }

        // System Tables (dbo schema)
        public DbSet<StandardRegistry> StandardRegistries { get; set; }

        // ⚠️ REMOVED - Tables không tồn tại trong schema mới:
        // - Tag/EntityType/EntityTag → Sẽ tạo models mới cho dbo.hashtags, dbo.entities, dbo.entity_hashtags
        // - WorkspaceMember, WorkspaceRelationshipType → Dropped
        // - NoteMember, NoteVersion → Dropped

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