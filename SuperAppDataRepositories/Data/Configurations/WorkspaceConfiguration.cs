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
            builder.HasKey(w => w.Id);
            builder.Property(w => w.Id)
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

            // Indexes
            builder.HasIndex(w => w.UserId)
                .HasDatabaseName("IX_workspaces_user")
                .HasFilter("[deleted_at] IS NULL");

            // Relationships
            builder.HasOne(w => w.User)
                .WithMany(u => u.Workspaces)
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(w => w.Items)
                .WithOne(wi => wi.Workspace)
                .HasForeignKey(wi => wi.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}