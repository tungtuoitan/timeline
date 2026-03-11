using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Data.Configurations
{
    /// <summary>
    /// EF Core configuration for KWorkspaceItemEntity entity
    /// Maps to: kws.workspace_items table
    /// </summary>
    public class KWorkspaceItemConfiguration : IEntityTypeConfiguration<KWorkspaceItemEntity>
    {
        public void Configure(EntityTypeBuilder<KWorkspaceItemEntity> builder)
        {
            // Table mapping - kws schema (KWorkspace)
            builder.ToTable("workspace_items", "kws");

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

            builder.Property(wi => wi.PathIds)
                .HasColumnName("PathIds")
                .HasMaxLength(1000)
                .HasDefaultValue("/");

            builder.Property(wi => wi.PathDepth)
                .HasColumnName("PathDepth")
                .HasDefaultValue(0);

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

            builder.HasIndex(wi => new { wi.EntityType, wi.EntityId })
                .HasDatabaseName("IX_kworkspace_items_item");

            // Unique constraint: one item can only exist once per workspace
            builder.HasIndex(wi => new { wi.WorkspaceId, wi.EntityType, wi.EntityId })
                .HasDatabaseName("UQ_kworkspace_items_unique")
                .IsUnique();

            // Relationships
            builder.HasOne(wi => wi.KWorkspace)
                .WithMany(w => w.Items)  // Explicitly map to KWorkspace.Items collection
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
