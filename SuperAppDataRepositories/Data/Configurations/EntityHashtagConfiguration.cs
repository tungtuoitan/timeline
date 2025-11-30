using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Data.Configurations
{
    /// <summary>
    /// EF Core configuration for EntityHashtag entity (polymorphic tagging)
    /// Maps to: dbo.entity_hashtags table
    /// Schema: REBUILD_SIMPLIFIED_SCHEMA.sql
    /// Links hashtags to workspace/folder/note/file entities
    /// </summary>
    public class EntityHashtagConfiguration : IEntityTypeConfiguration<EntityHashtag>
    {
        public void Configure(EntityTypeBuilder<EntityHashtag> builder)
        {
            // Table mapping - dbo schema (Default)
            builder.ToTable("entity_hashtags", "dbo");

            // Primary key
            builder.HasKey(et => et.Id);
            builder.Property(et => et.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            // Properties - EXACTLY match dbo.entity_hashtags schema
            builder.Property(et => et.EntityType)
                .HasColumnName("entity_type")
                .IsRequired();

            builder.Property(et => et.EntityId)
                .HasColumnName("entity_id")
                .IsRequired();

            builder.Property(et => et.HashtagId)
                .HasColumnName("hashtag_id")
                .IsRequired();

            builder.Property(et => et.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("GETUTCDATE()");

            // Unique constraint - one hashtag can only be applied once to an entity
            builder.HasIndex(et => new { et.HashtagId, et.EntityType, et.EntityId })
                .HasDatabaseName("UQ_entity_hashtags_unique")
                .IsUnique();

            // Index for finding hashtags by entity
            builder.HasIndex(et => new { et.EntityType, et.EntityId })
                .HasDatabaseName("IX_entity_hashtags_entity");

            // Relationships
            builder.HasOne(et => et.Hashtag)
                .WithMany(h => h.EntityHashtags)
                .HasForeignKey(et => et.HashtagId)
                .OnDelete(DeleteBehavior.Cascade);

            // Polymorphic relationships (navigations populated at runtime based on EntityType)
            // EntityType: 1=workspace, 2=folder, 3=note, 4=file (from dbo.entities)
            builder.Ignore(et => et.EntityWorkspace);
            builder.Ignore(et => et.EntityFolder);
            builder.Ignore(et => et.EntityNote);
            builder.Ignore(et => et.EntityFile);
        }
    }
}
