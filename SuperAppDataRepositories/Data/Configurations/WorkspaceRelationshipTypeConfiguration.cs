using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Data.Configurations
{
    public class WorkspaceRelationshipTypeConfiguration : IEntityTypeConfiguration<WorkspaceRelationshipType>
    {
        public void Configure(EntityTypeBuilder<WorkspaceRelationshipType> builder)
        {
            // Table mapping - ws schema (Workspace)
            builder.ToTable("workspace_relationship_types", "ws");

            // Primary key
            builder.HasKey(wrt => wrt.RelationshipTypeId);
            builder.Property(wrt => wrt.RelationshipTypeId)
                .HasColumnName("relationship_type_id")
                .ValueGeneratedOnAdd();

            // Properties
            builder.Property(wrt => wrt.WorkspaceId)
                .HasColumnName("workspace_id")
                .IsRequired();

            builder.Property(wrt => wrt.TypeName)
                .HasColumnName("type_name")
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(wrt => wrt.DisplayName)
                .HasColumnName("display_name")
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(wrt => wrt.Description)
                .HasColumnName("description")
                .HasMaxLength(1000);

            builder.Property(wrt => wrt.Icon)
                .HasColumnName("icon")
                .HasMaxLength(50);

            builder.Property(wrt => wrt.Color)
                .HasColumnName("color")
                .HasMaxLength(7);

            builder.Property(wrt => wrt.LineStyle)
                .HasColumnName("line_style")
                .HasMaxLength(20)
                .HasDefaultValue("solid");

            builder.Property(wrt => wrt.LineWidth)
                .HasColumnName("line_width")
                .HasDefaultValue(2);

            builder.Property(wrt => wrt.IsBidirectional)
                .HasColumnName("is_bidirectional")
                .HasDefaultValue(false);

            builder.Property(wrt => wrt.AllowsCycles)
                .HasColumnName("allows_cycles")
                .HasDefaultValue(false);

            builder.Property(wrt => wrt.MaxDepth)
                .HasColumnName("max_depth");

            builder.Property(wrt => wrt.ValidationRules)
                .HasColumnName("validation_rules")
                .HasColumnType("nvarchar(max)");

            builder.Property(wrt => wrt.SortOrder)
                .HasColumnName("sort_order")
                .HasDefaultValue(0);

            builder.Property(wrt => wrt.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("GETDATE()");

            builder.Property(wrt => wrt.UpdatedAt)
                .HasColumnName("updated_at");

            builder.Property(wrt => wrt.DeletedAt)
                .HasColumnName("deleted_at");

            // Indexes
            builder.HasIndex(wrt => wrt.WorkspaceId)
                .HasDatabaseName("IX_workspace_relationship_types_workspace");

            builder.HasIndex(wrt => new { wrt.WorkspaceId, wrt.TypeName })
                .HasDatabaseName("UQ_workspace_relationship_types_unique")
                .IsUnique()
                .HasFilter("[deleted_at] IS NULL");

            builder.HasIndex(wrt => wrt.SortOrder)
                .HasDatabaseName("IX_workspace_relationship_types_sort_order");

            // Soft delete query filter
            builder.HasQueryFilter(wrt => wrt.DeletedAt == null);

            // Relationships
            builder.HasOne(wrt => wrt.Workspace)
                .WithMany(w => w.RelationshipTypes)
                .HasForeignKey(wrt => wrt.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}