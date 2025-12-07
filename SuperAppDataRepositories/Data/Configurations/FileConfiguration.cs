using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Data.Configurations
{
    /// <summary>
    /// Entity Framework Core configuration for File entity
    /// Maps to 'files' table in ws schema
    /// Schema: REBUILD_SIMPLIFIED_SCHEMA.sql
    /// </summary>
    public class FileConfiguration : IEntityTypeConfiguration<SuperAppModels.Models.File>
    {
        public void Configure(EntityTypeBuilder<SuperAppModels.Models.File> builder)
        {
            // Table mapping - ws schema (Workspace)
            builder.ToTable("files", "ws");

            // Primary key
            builder.HasKey(f => f.Id);
            builder.Property(f => f.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            // Properties - EXACTLY match ws.files schema
            builder.Property(f => f.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            builder.Property(f => f.Name)
                .HasColumnName("name")
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(f => f.Url)
                .HasColumnName("url")
                .HasMaxLength(1000);

            builder.Property(f => f.FileSize)
                .HasColumnName("file_size");

            builder.Property(f => f.MimeType)
                .HasColumnName("mime_type")
                .HasMaxLength(100);

            builder.Property(f => f.Extension)
                .HasColumnName("extension")
                .HasMaxLength(20);

            builder.Property(f => f.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(f => f.UpdatedAt)
                .HasColumnName("updated_at");

            builder.Property(f => f.DeletedAt)
                .HasColumnName("deleted_at");

            // Indexes - match schema
            builder.HasIndex(f => f.UserId)
                .HasDatabaseName("IX_files_user")
                .HasFilter("[deleted_at] IS NULL");

            builder.HasIndex(f => f.MimeType)
                .HasDatabaseName("IX_files_type");

            builder.HasIndex(f => f.CreatedAt)
                .HasDatabaseName("IX_files_created");

            // Relationships
            builder.HasOne(f => f.User)
                .WithMany()
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Ignore polymorphic navigation (managed via WorkspaceItem.ItemType)
            builder.Ignore(f => f.WorkspaceItems);
        }
    }
}
