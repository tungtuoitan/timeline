using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SuperAppModels.Models;
using SuperAppModels.Models.DailyLog;

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
        public DbSet<SuperAppModels.Models.Auth.UserTotp> UserTotps { get; set; }

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

        // DailyLog Tables (pro schema)
        public DbSet<SuperAppModels.Models.DailyLog.DailyLog> DailyLogs { get; set; }
        public DbSet<DailyLogFieldTemplate> DailyLogFieldTemplates { get; set; }

        // Finance Tables (pro schema)
        public DbSet<SuperAppModels.Models.Finance.FinTransaction> FinTransactions { get; set; }
        public DbSet<SuperAppModels.Models.Finance.FinPrice> FinPrices { get; set; }

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

        /// <summary>
        /// Every DateTime column holds a UTC instant (TungRoot #1450). Values read back get
        /// DateTimeKind.Utc; on write a Local value is converted to UTC and an Unspecified value is
        /// assumed to already be UTC. Calendar dates are DateOnly (SQL date) and are not affected.
        /// </summary>
        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            base.ConfigureConventions(configurationBuilder);
            configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
            configurationBuilder.Properties<DateTime?>().HaveConversion<UtcDateTimeConverter>();
        }

        // Stamp CreatedAt/UpdatedAt (UTC) for entities implementing SuperAppModels.Models.ITimestampEntity.
        // Base SaveChanges()/SaveChangesAsync(ct) overloads delegate to these two.
        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            UpdateTimestamps();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        private void UpdateTimestamps()
        {
            var now = DateTime.UtcNow;
            foreach (var entry in ChangeTracker.Entries<ITimestampEntity>())
            {
                if (entry.State == EntityState.Added)
                {
                    // Keep an explicitly set CreatedAt (e.g. imports / restores carrying original times).
                    if (entry.Entity.CreatedAt == null || entry.Entity.CreatedAt == default(DateTime))
                        entry.Entity.CreatedAt = now;
                    entry.Entity.UpdatedAt = now;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedAt = now;
                }
            }
        }
    }

    /// <summary>EF value converter: DateTime stored as UTC, read back with Kind Utc.</summary>
    public sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
    {
        public UtcDateTimeConverter()
            : base(
                v => v.Kind == DateTimeKind.Local ? v.ToUniversalTime() : DateTime.SpecifyKind(v, DateTimeKind.Utc),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
        {
        }
    }
}