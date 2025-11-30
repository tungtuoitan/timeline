using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Data.Configurations
{
    /// <summary>
    /// EF Core configuration for WorkspaceItem entity
    /// Maps to: ws.workspace_items table
    /// Schema: REBUILD_SIMPLIFIED_SCHEMA.sql
    /// </summary>
    public class WorkspaceItemConfiguration : IEntityTypeConfiguration<WorkspaceItem>
    {
        public void Configure(EntityTypeBuilder<WorkspaceItem> builder)
        {
            // Table mapping - ws schema (Workspace)
            builder.ToTable("workspace_items", "ws");

            // Primary key
            builder.HasKey(wi => wi.ItemId);
            builder.Property(wi => wi.ItemId)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            // Properties
            builder.Property(wi => wi.WorkspaceId)
                .HasColumnName("workspace_id")
                .IsRequired();

            builder.Property(wi => wi.FolderId)
                .HasColumnName("folder_id")
                .IsRequired(false); // Nullable for root-level items

            builder.Property(wi => wi.ItemType)
                .HasColumnName("item_type")
                .IsRequired();

            builder.Property(wi => wi.ChildId)
                .HasColumnName("item_id")
                .IsRequired();

            builder.Property(wi => wi.IsOriginal)
                .HasColumnName("is_original")
                .HasDefaultValue(true)
                .IsRequired();

            builder.Property(wi => wi.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(wi => wi.UpdatedAt)
                .HasColumnName("updated_at");

            builder.Property(wi => wi.DeletedAt)
                .HasColumnName("deleted_at");

            // Indexes
            builder.HasIndex(wi => new { wi.WorkspaceId, wi.DeletedAt })
                .HasDatabaseName("IX_workspace_items_workspace")
                .HasFilter("[deleted_at] IS NULL");

            builder.HasIndex(wi => wi.FolderId)
                .HasDatabaseName("IX_workspace_items_folder")
                .HasFilter("[deleted_at] IS NULL");

            builder.HasIndex(wi => new { wi.ItemType, wi.ChildId })
                .HasDatabaseName("IX_workspace_items_item");

            builder.HasIndex(wi => new { wi.ItemType, wi.ChildId, wi.IsOriginal })
                .HasDatabaseName("IX_workspace_items_original")
                .HasFilter("[is_original] = 1");

            // Unique constraint: one item can only exist once per workspace
            builder.HasIndex(wi => new { wi.WorkspaceId, wi.ItemType, wi.ChildId })
                .HasDatabaseName("UQ_workspace_items_unique")
                .IsUnique();

            // Relationships
            builder.HasOne(wi => wi.Workspace)
                .WithMany(w => w.Items)  // Explicitly map to Workspace.Items collection
                .HasForeignKey(wi => wi.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);

            // Parent folder relationship - explicitly specify FK to avoid EF Core auto-generating FolderId1
            builder.HasOne(wi => wi.Folder)
                .WithMany(f => f.WorkspaceItems)  // Map to Folder.WorkspaceItems collection
                .HasForeignKey(wi => wi.FolderId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);  // Nullable FK for root-level items

            // Polymorphic relationships (navigations populated at runtime based on ItemType)
            builder.Ignore(wi => wi.ChildFolder);
            builder.Ignore(wi => wi.ChildNote);
            builder.Ignore(wi => wi.ChildFile);
        }
    }
}