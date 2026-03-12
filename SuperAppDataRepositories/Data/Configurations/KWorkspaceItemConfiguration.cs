using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Data.Configurations
{
    /// <summary>
    /// EF Core configuration for KWorkspaceItemEntity
    /// kws.workspace_items is now a self-contained node table (no entity_type/entity_id).
    /// </summary>
    public class KWorkspaceItemConfiguration : IEntityTypeConfiguration<KWorkspaceItemEntity>
    {
        public void Configure(EntityTypeBuilder<KWorkspaceItemEntity> builder)
        {
            builder.ToTable("workspace_items", "kws");

            builder.HasKey(wi => wi.Id);
            builder.Property(wi => wi.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            builder.Property(wi => wi.WorkspaceId)
                .HasColumnName("workspace_id")
                .IsRequired();

            builder.Property(wi => wi.ParentId)
                .HasColumnName("parent_id")
                .IsRequired(false);

            // Node data
            builder.Property(wi => wi.Name)
                .HasColumnName("name")
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(wi => wi.Description)
                .HasColumnName("description")
                .HasColumnType("nvarchar(max)")
                .IsRequired(false);

            builder.Property(wi => wi.Color)
                .HasColumnName("color")
                .HasMaxLength(7)
                .HasDefaultValue("#F59E0B")
                .IsRequired(false);

            builder.Property(wi => wi.Icon)
                .HasColumnName("icon")
                .HasMaxLength(50)
                .HasDefaultValue("📁")
                .IsRequired(false);

            // Materialized Path
            builder.Property(wi => wi.PathIds)
                .HasColumnName("PathIds")
                .HasMaxLength(1000)
                .HasDefaultValue("/");

            builder.Property(wi => wi.PathDepth)
                .HasColumnName("PathDepth")
                .HasDefaultValue(0);

            // Timestamps
            builder.Property(wi => wi.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(wi => wi.UpdatedAt)
                .HasColumnName("updated_at");

            builder.Property(wi => wi.DeletedAt)
                .HasColumnName("deleted_at");

            // Indexes
            builder.HasIndex(wi => new { wi.WorkspaceId, wi.DeletedAt })
                .HasDatabaseName("IX_kworkspace_items_workspace")
                .HasFilter("[deleted_at] IS NULL");

            builder.HasIndex(wi => wi.ParentId)
                .HasDatabaseName("IX_kworkspace_items_parent")
                .HasFilter("[deleted_at] IS NULL");

            builder.HasIndex(wi => new { wi.PathIds, wi.PathDepth })
                .HasDatabaseName("IX_kworkspace_items_path")
                .HasFilter("[deleted_at] IS NULL");

            // Relationships
            builder.HasOne(wi => wi.KWorkspace)
                .WithMany(w => w.Items)
                .HasForeignKey(wi => wi.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(wi => wi.Parent)
                .WithMany()
                .HasForeignKey(wi => wi.ParentId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);
        }
    }
}
