using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Data.Configurations
{
    public class WorkspaceItemConfiguration : IEntityTypeConfiguration<WorkspaceItem>
    {
        public void Configure(EntityTypeBuilder<WorkspaceItem> builder)
        {
            // Table mapping
            builder.ToTable("workspace_items");

            // Primary key
            builder.HasKey(wi => wi.ItemId);
            builder.Property(wi => wi.ItemId)
                .HasColumnName("item_id")
                .ValueGeneratedOnAdd();

            // Properties
            builder.Property(wi => wi.WorkspaceId)
                .HasColumnName("workspace_id")
                .IsRequired();

            builder.Property(wi => wi.ParentTagId)
                .HasColumnName("parent_tag_id")
                .IsRequired();

            builder.Property(wi => wi.ChildType)
                .HasColumnName("child_type")
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(wi => wi.ChildId)
                .HasColumnName("child_id")
                .IsRequired();

            builder.Property(wi => wi.RelationshipType)
                .HasColumnName("relationship_type")
                .HasMaxLength(100);

            builder.Property(wi => wi.Label)
                .HasColumnName("label")
                .HasMaxLength(255);

            builder.Property(wi => wi.Notes)
                .HasColumnName("notes")
                .HasMaxLength(1000);

            builder.Property(wi => wi.ItemPath)
                .HasColumnName("item_path")
                .HasMaxLength(4000);

            builder.Property(wi => wi.Depth)
                .HasColumnName("depth")
                .HasDefaultValue(0);

            builder.Property(wi => wi.SortOrder)
                .HasColumnName("sort_order")
                .HasDefaultValue(0);

            builder.Property(wi => wi.Color)
                .HasColumnName("color")
                .HasMaxLength(7);

            builder.Property(wi => wi.Icon)
                .HasColumnName("icon")
                .HasMaxLength(50);

            builder.Property(wi => wi.AddedBy)
                .HasColumnName("added_by")
                .IsRequired();

            builder.Property(wi => wi.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("GETDATE()");

            builder.Property(wi => wi.UpdatedAt)
                .HasColumnName("updated_at");

            builder.Property(wi => wi.DeletedAt)
                .HasColumnName("deleted_at");

            // Indexes
            builder.HasIndex(wi => wi.WorkspaceId)
                .HasDatabaseName("IX_workspace_items_workspace");

            builder.HasIndex(wi => wi.ParentTagId)
                .HasDatabaseName("IX_workspace_items_parent");

            builder.HasIndex(wi => new { wi.ChildType, wi.ChildId })
                .HasDatabaseName("IX_workspace_items_child");

            builder.HasIndex(wi => wi.ItemPath)
                .HasDatabaseName("IX_workspace_items_path");

            builder.HasIndex(wi => new { wi.WorkspaceId, wi.ParentTagId })
                .HasDatabaseName("IX_workspace_items_workspace_parent");

            // Unique constraint: one item can only be in one location per workspace
            builder.HasIndex(wi => new { wi.WorkspaceId, wi.ParentTagId, wi.ChildType, wi.ChildId })
                .HasDatabaseName("UQ_workspace_items_unique")
                .IsUnique()
                .HasFilter("[deleted_at] IS NULL");

            // Soft delete query filter
            builder.HasQueryFilter(wi => wi.DeletedAt == null);

            // Relationships
            builder.HasOne(wi => wi.Workspace)
                .WithMany(w => w.Items)
                .HasForeignKey(wi => wi.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(wi => wi.ParentTag)
                .WithMany() // Tag can have many children in workspaces
                .HasForeignKey(wi => wi.ParentTagId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(wi => wi.AddedByUser)
                .WithMany() // User can add many items
                .HasForeignKey(wi => wi.AddedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Note: ChildTag and ChildNote relationships are handled dynamically
            // based on ChildType and ChildId, not through navigation properties
        }
    }
}