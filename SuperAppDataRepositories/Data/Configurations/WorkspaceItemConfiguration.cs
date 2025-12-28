using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Data.Configurations
{
    /// <summary>
    /// EF Core configuration for WorkspaceItemEntity entity
    /// Maps to: ws.workspace_items table
    /// Schema: REBUILD_SIMPLIFIED_SCHEMA.sql
    /// </summary>
    public class WorkspaceItemConfiguration : IEntityTypeConfiguration<WorkspaceItemEntity>
    {
        public void Configure(EntityTypeBuilder<WorkspaceItemEntity> builder)
        {
            // Table mapping - ws schema (Workspace)
            builder.ToTable("workspace_items", "ws");

            // Primary key
            builder.HasKey(wi => wi.Id);
            builder.Property(wi => wi.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            // Properties
            builder.Property(wi => wi.WorkspaceId)
                .HasColumnName("workspace_id")
                .IsRequired();

            builder.Property(wi => wi.ParentId)
                .HasColumnName("parent_id")
                .IsRequired(false); // Nullable for root-level items

            builder.Property(wi => wi.EntityType)
                .HasColumnName("entity_type")
                .IsRequired();

            builder.Property(wi => wi.EntityId)
                .HasColumnName("entity_id")
                .IsRequired();

            builder.Property(wi => wi.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(wi => wi.UpdatedAt)
                .HasColumnName("updated_at");

            builder.Property(wi => wi.DeletedAt)
                .HasColumnName("deleted_at");

            builder.Property(wi => wi.CopyInfo)
                .HasColumnName("copy_info")
                .HasColumnType("nvarchar(max)");

            // Indexes
            builder.HasIndex(wi => new { wi.WorkspaceId, wi.DeletedAt })
                .HasDatabaseName("IX_workspace_items_workspace")
                .HasFilter("[deleted_at] IS NULL");

            builder.HasIndex(wi => wi.ParentId)
                .HasDatabaseName("IX_workspace_items_parent")
                .HasFilter("[deleted_at] IS NULL");

            builder.HasIndex(wi => new { wi.EntityType, wi.EntityId })
                .HasDatabaseName("IX_workspace_items_item");

            // Unique constraint: one item can only exist once per workspace
            builder.HasIndex(wi => new { wi.WorkspaceId, wi.EntityType, wi.EntityId })
                .HasDatabaseName("UQ_workspace_items_unique")
                .IsUnique();

            // Relationships
            builder.HasOne(wi => wi.Workspace)
                .WithMany(w => w.Items)  // Explicitly map to Workspace.Items collection
                .HasForeignKey(wi => wi.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);

            // Self-referencing parent relationship (workspace_items.parent_id → workspace_items.id)
            builder.HasOne(wi => wi.Parent)
                .WithMany()  // No inverse navigation property
                .HasForeignKey(wi => wi.ParentId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);  // Nullable FK for root-level items

            // Polymorphic relationships (navigations populated at runtime based on ItemType)
            builder.Ignore(wi => wi.ChildFolder);
            builder.Ignore(wi => wi.ChildNote);
            builder.Ignore(wi => wi.ChildFile);
        }
    }
}