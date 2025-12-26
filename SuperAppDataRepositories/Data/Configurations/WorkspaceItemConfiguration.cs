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

            builder.Property(wi => wi.ItemType)
                .HasColumnName("item_type")
                .IsRequired();

            builder.Property(wi => wi.ItemId)
                .HasColumnName("item_id")
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

            builder.HasIndex(wi => new { wi.ItemType, wi.ItemId })
                .HasDatabaseName("IX_workspace_items_item");

            // Unique constraint: one item can only exist once per workspace
            builder.HasIndex(wi => new { wi.WorkspaceId, wi.ItemType, wi.ItemId })
                .HasDatabaseName("UQ_workspace_items_unique")
                .IsUnique();

            // Relationships
            builder.HasOne(wi => wi.Workspace)
                .WithMany(w => w.Items)  // Explicitly map to Workspace.Items collection
                .HasForeignKey(wi => wi.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);

            // Parent folder relationship - explicitly specify FK to avoid EF Core auto-generating ParentId1
            builder.HasOne(wi => wi.Folder)
                .WithMany(f => f.WorkspaceItems)  // Map to Folder.WorkspaceItems collection
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