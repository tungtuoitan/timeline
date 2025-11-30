using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Data.Configurations
{
    public class WorkspaceConfiguration : IEntityTypeConfiguration<Workspace>
    {
        public void Configure(EntityTypeBuilder<Workspace> builder)
        {
            // Table mapping - ws.workspaces
            builder.ToTable("workspaces", "ws");

            // Primary key
            builder.HasKey(w => w.WorkspaceId);
            builder.Property(w => w.WorkspaceId)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            // Properties that EXIST in DB
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

            builder.Property(w => w.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(w => w.UpdatedAt)
                .HasColumnName("updated_at");

            builder.Property(w => w.DeletedAt)
                .HasColumnName("deleted_at");

            // IGNORE properties that DON'T exist in DB
            builder.Ignore(w => w.Color);
            builder.Ignore(w => w.Icon);
            builder.Ignore(w => w.Type);
            builder.Ignore(w => w.MaxDepth);
            builder.Ignore(w => w.IsDefault);
            builder.Ignore(w => w.IsPublic);
            builder.Ignore(w => w.IsTemplate);
            builder.Ignore(w => w.IsArchived);
            builder.Ignore(w => w.TagCount);
            builder.Ignore(w => w.RelationshipCount);
            builder.Ignore(w => w.MemberCount);
            builder.Ignore(w => w.Settings);
            builder.Ignore(w => w.LastAccessedAt);

            // Indexes
            builder.HasIndex(w => w.UserId)
                .HasDatabaseName("IX_workspaces_user")
                .HasFilter("[deleted_at] IS NULL");

            // Soft delete query filter
            builder.HasQueryFilter(w => w.DeletedAt == null);

            // Relationships
            builder.HasOne(w => w.User)
                .WithMany(u => u.Workspaces)
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(w => w.Items)
                .WithOne(wi => wi.Workspace)
                .HasForeignKey(wi => wi.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);

            // IGNORE navigation properties for tables that don't exist
            builder.Ignore(w => w.Members);
            builder.Ignore(w => w.RelationshipTypes);
        }
    }
}