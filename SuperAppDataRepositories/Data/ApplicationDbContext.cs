using Microsoft.EntityFrameworkCore;
using SuperAppModels.Models;
using SuperAppModels.Utils;

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
        public DbSet<SuperAppModels.Models.File> Files { get; set; }
        public DbSet<WorkspaceItemEntity> WorkspaceItems { get; set; }

        // K Tables (k schema)
        public DbSet<KKnowledge> KKnowledges { get; set; }
        public DbSet<KNodeEntity> KNodes { get; set; }
        public DbSet<KQuestionEntity> KQuestions { get; set; }
        public DbSet<KPointHistoryEntity> KPointHistory { get; set; }
        public DbSet<KQuestionStatusHistoryEntity> KQuestionStatusHistory { get; set; }
        public DbSet<KNodeStatusHistoryEntity> KNodeStatusHistory { get; set; }
        public DbSet<KAttachmentEntity> KAttachments { get; set; }
        public DbSet<KAttachmentLinkEntity> KAttachmentLinks { get; set; }

        // Entity Tables (dbo schema)
        public DbSet<Note> Notes { get; set; }
        public DbSet<Hashtag> Hashtags { get; set; }
        public DbSet<EntityHashtag> EntityHashtags { get; set; }

        // Lookup Tables (dbo schema)
        public DbSet<Entity> Entities { get; set; }

        // System Tables (dbo schema)
        public DbSet<StandardRegistry> StandardRegistries { get; set; }
        public DbSet<Keyword> Keywords { get; set; }

        // Auth Tables (auth schema)
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        // Personal Productivity App Tables (pro schema)
        public DbSet<Project> Projects { get; set; }
        public DbSet<ProTask> ProTasks { get; set; }
        public DbSet<TargetKeyword> TargetKeywords { get; set; }
        public DbSet<TaskChecklistHistory> TaskChecklistHistories { get; set; }
        public DbSet<TaskComment> TaskComments { get; set; }
        public DbSet<FlowEdge> FlowEdges { get; set; }
        public DbSet<FlowNodePosition> FlowNodePositions { get; set; }

        // LifeLog Tables (log schema)
        public DbSet<LifeLogTrack> LifeLogTracks { get; set; }
        public DbSet<LifeLogLog> LifeLogLogs { get; set; }

        // Wiki Tables (wiki schema)
        public DbSet<WikiKeyword> WikiKeywords { get; set; }
        public DbSet<WikiKeywordSynonym> WikiKeywordSynonyms { get; set; }
        public DbSet<WikiInfo> WikiInfos { get; set; }
        public DbSet<WikiInfoKeyword> WikiInfoKeywords { get; set; }

        // ⚠️ REMOVED - Tables không tồn tại trong schema mới:
        // - Tag/EntityType/EntityTag → Sẽ tạo models mới cho dbo.hashtags, dbo.entities, dbo.entity_hashtags
        // - WorkspaceMember, WorkspaceRelationshipType → Dropped
        // - NoteMember, NoteVersion → Dropped

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // k.test and k.test_node are dropped — exclude from model
            modelBuilder.Ignore<KTestEntity>();
            modelBuilder.Ignore<KTestNodeEntity>();

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
                        timestampEntity.CreatedAt = VietnamDateTime.Now();
                    }
                    timestampEntity.UpdatedAt = VietnamDateTime.Now();
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