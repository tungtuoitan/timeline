using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Data.Configurations
{
    /// <summary>
    /// Configuration for EntityTag entity
    /// Polymorphic many-to-many tagging system for workspaces, folders, notes, and files
    /// Maps to 'entity_tags' table
    /// </summary>
    public class EntityTagConfiguration : IEntityTypeConfiguration<EntityTag>
    {
        public void Configure(EntityTypeBuilder<EntityTag> builder)
        {
            // Table mapping - dbo schema
            builder.ToTable("entity_tags", "dbo");

            // Primary key
            builder.HasKey(et => et.EntityTagId);
            builder.Property(et => et.EntityTagId)
                .HasColumnName("entity_tag_id")
                .ValueGeneratedOnAdd();

            // Foreign key to Tag (hashtags in tags_new table)
            builder.Property(et => et.TagId)
                .HasColumnName("tag_id")
                .IsRequired();

            // Polymorphic entity reference
            builder.Property(et => et.EntityType)
                .HasColumnName("entity_type")
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(et => et.EntityId)
                .HasColumnName("entity_id")
                .IsRequired();

            // Audit field
            builder.Property(et => et.TaggedBy)
                .HasColumnName("tagged_by")
                .IsRequired();

            // Timestamps
            builder.Property(et => et.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(et => et.UpdatedAt)
                .HasColumnName("updated_at");

            builder.Property(et => et.DeletedAt)
                .HasColumnName("deleted_at");

            // Indexes
            // Composite index for efficient entity tag lookups
            builder.HasIndex(et => new { et.EntityType, et.EntityId })
                .HasDatabaseName("IX_entity_tags_entity");

            // Index for tag lookups
            builder.HasIndex(et => et.TagId)
                .HasDatabaseName("IX_entity_tags_tag");

            // Index for user lookups
            builder.HasIndex(et => et.TaggedBy)
                .HasDatabaseName("IX_entity_tags_user");

            // Unique constraint: one tag per entity (prevent duplicates)
            builder.HasIndex(et => new { et.TagId, et.EntityType, et.EntityId })
                .HasDatabaseName("UQ_entity_tags_tag_entity")
                .IsUnique()
                .HasFilter("[deleted_at] IS NULL");

            // Soft delete query filter
            builder.HasQueryFilter(et => et.DeletedAt == null);

            // Relationships

            // FK to Tag (hashtags)
            builder.HasOne(et => et.Tag)
                .WithMany() // Tag can have many EntityTags
                .HasForeignKey(et => et.TagId)
                .OnDelete(DeleteBehavior.Cascade);

            // FK to User (who tagged)
            builder.HasOne(et => et.TaggedByUser)
                .WithMany() // User can create many EntityTags
                .HasForeignKey(et => et.TaggedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Note: Polymorphic relationships (to Workspace, Folder, Note, File)
            // cannot be configured in EF Core and must be handled manually in queries
        }
    }
}
