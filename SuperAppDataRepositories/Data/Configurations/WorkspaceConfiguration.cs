using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Data.Configurations
{
    public class WorkspaceConfiguration : IEntityTypeConfiguration<Workspace>
    {
        public void Configure(EntityTypeBuilder<Workspace> builder)
        {
            // Table mapping - ws schema (Workspace)
            builder.ToTable("workspaces", "ws");

            // Primary key
            builder.HasKey(w => w.WorkspaceId);
            builder.Property(w => w.WorkspaceId)
                .HasColumnName("workspace_id")
                .ValueGeneratedOnAdd();

            // Properties
            builder.Property(w => w.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            builder.Property(w => w.Name)
                .HasColumnName("name")
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(w => w.Description)
                .HasColumnName("description")
                .HasMaxLength(1000);

            builder.Property(w => w.Color)
                .HasColumnName("color")
                .HasMaxLength(7); // #RRGGBB

            builder.Property(w => w.Icon)
                .HasColumnName("icon")
                .HasMaxLength(50);

            builder.Property(w => w.Type)
                .HasColumnName("type")
                .HasMaxLength(50)
                .HasDefaultValue("hierarchy");

            builder.Property(w => w.MaxDepth)
                .HasColumnName("max_depth")
                .HasDefaultValue(10);

            builder.Property(w => w.IsDefault)
                .HasColumnName("is_default")
                .HasDefaultValue(false);

            builder.Property(w => w.IsPublic)
                .HasColumnName("is_public")
                .HasDefaultValue(false);

            builder.Property(w => w.IsTemplate)
                .HasColumnName("is_template")
                .HasDefaultValue(false);

            builder.Property(w => w.IsArchived)
                .HasColumnName("is_archived")
                .HasDefaultValue(false);

            builder.Property(w => w.TagCount)
                .HasColumnName("tag_count")
                .HasDefaultValue(0);

            builder.Property(w => w.RelationshipCount)
                .HasColumnName("relationship_count")
                .HasDefaultValue(0);

            builder.Property(w => w.MemberCount)
                .HasColumnName("member_count")
                .HasDefaultValue(1);

            builder.Property(w => w.Settings)
                .HasColumnName("settings")
                .HasColumnType("nvarchar(max)");

            builder.Property(w => w.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("GETDATE()");

            builder.Property(w => w.UpdatedAt)
                .HasColumnName("updated_at");

            builder.Property(w => w.LastAccessedAt)
                .HasColumnName("last_accessed_at");

            builder.Property(w => w.DeletedAt)
                .HasColumnName("deleted_at");

            // Indexes
            builder.HasIndex(w => w.UserId)
                .HasDatabaseName("IX_workspaces_user");

            builder.HasIndex(w => w.Name)
                .HasDatabaseName("IX_workspaces_name");

            builder.HasIndex(w => w.Type)
                .HasDatabaseName("IX_workspaces_type");

            builder.HasIndex(w => w.IsDefault)
                .HasDatabaseName("IX_workspaces_is_default")
                .HasFilter("[is_default] = 1 AND [deleted_at] IS NULL");

            builder.HasIndex(w => w.IsPublic)
                .HasDatabaseName("IX_workspaces_is_public")
                .HasFilter("[is_public] = 1 AND [deleted_at] IS NULL");

            builder.HasIndex(w => w.LastAccessedAt)
                .HasDatabaseName("IX_workspaces_last_accessed");

            // Soft delete query filter
            builder.HasQueryFilter(w => w.DeletedAt == null);

            // Relationships
            builder.HasOne(w => w.User)
                .WithMany(u => u.Workspaces) // User can have many Workspaces
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(w => w.Members)
                .WithOne(wm => wm.Workspace)
                .HasForeignKey(wm => wm.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(w => w.Items)
                .WithOne(wi => wi.Workspace)
                .HasForeignKey(wi => wi.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(w => w.RelationshipTypes)
                .WithOne(wrt => wrt.Workspace)
                .HasForeignKey(wrt => wrt.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}