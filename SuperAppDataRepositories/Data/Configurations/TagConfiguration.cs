using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Data.Configurations
{
    public class TagConfiguration : IEntityTypeConfiguration<Tag>
    {
        public void Configure(EntityTypeBuilder<Tag> builder)
        {
            // Table mapping
            builder.ToTable("tags");

            // Primary key
            builder.HasKey(t => t.TagId);
            builder.Property(t => t.TagId)
                .HasColumnName("tag_id")
                .ValueGeneratedOnAdd();

            // Properties
            builder.Property(t => t.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            builder.Property(t => t.Name)
                .HasColumnName("name")
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(t => t.Slug)
                .HasColumnName("slug")
                .HasMaxLength(255);

            builder.Property(t => t.Color)
                .HasColumnName("color")
                .HasMaxLength(7)
                .HasDefaultValue("#3B82F6");

            builder.Property(t => t.Icon)
                .HasColumnName("icon")
                .HasMaxLength(50);

            builder.Property(t => t.Description)
                .HasColumnName("description")
                .HasMaxLength(1000);

            builder.Property(t => t.Metadata)
                .HasColumnName("metadata")
                .HasColumnType("nvarchar(max)");

            builder.Property(t => t.UsageCount)
                .HasColumnName("usage_count")
                .HasDefaultValue(0);

            builder.Property(t => t.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("GETDATE()");

            builder.Property(t => t.UpdatedAt)
                .HasColumnName("updated_at");

            builder.Property(t => t.DeletedAt)
                .HasColumnName("deleted_at");

            // Indexes
            builder.HasIndex(t => t.UserId)
                .HasDatabaseName("IX_tags_user");

            builder.HasIndex(t => t.Name)
                .HasDatabaseName("IX_tags_name");

            builder.HasIndex(t => new { t.UserId, t.Slug })
                .HasDatabaseName("UQ_tags_user_slug")
                .IsUnique()
                .HasFilter("[deleted_at] IS NULL");

            builder.HasIndex(t => t.UsageCount)
                .HasDatabaseName("IX_tags_usage_count");

            builder.HasIndex(t => t.CreatedAt)
                .HasDatabaseName("IX_tags_created_at");

            // Soft delete query filter
            builder.HasQueryFilter(t => t.DeletedAt == null);

            // Relationships
            builder.HasOne(t => t.User)
                .WithMany(u => u.Tags) // User can have many Tags
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Note: WorkspaceItem relationships are handled in WorkspaceItemConfiguration
        }
    }
}