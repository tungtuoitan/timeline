using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Data.Configurations
{
    /// <summary>
    /// Entity Framework Core configuration for FileInfo entity
    /// Maps to 'files' table in database
    /// </summary>
    public class FileConfiguration : IEntityTypeConfiguration<SuperAppModels.Models.FileInfo>
    {
        public void Configure(EntityTypeBuilder<SuperAppModels.Models.FileInfo> builder)
        {
            // Table mapping - dbo schema
            builder.ToTable("files", "dbo");

            // Primary key
            builder.HasKey(f => f.FileId);
            builder.Property(f => f.FileId)
                .HasColumnName("file_id")
                .ValueGeneratedOnAdd();

            // Properties mapping
            builder.Property(f => f.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            builder.Property(f => f.Name)
                .HasColumnName("name")
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(f => f.OriginalFilename)
                .HasColumnName("original_filename")
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(f => f.FilePath)
                .HasColumnName("file_path")
                .HasMaxLength(1000)
                .IsRequired();

            builder.Property(f => f.FileSize)
                .HasColumnName("file_size")
                .IsRequired();

            builder.Property(f => f.MimeType)
                .HasColumnName("mime_type")
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(f => f.Extension)
                .HasColumnName("extension")
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(f => f.Description)
                .HasColumnName("description")
                .HasMaxLength(1000);

            builder.Property(f => f.Slug)
                .HasColumnName("slug")
                .HasMaxLength(550);

            builder.Property(f => f.IsPublic)
                .HasColumnName("is_public")
                .HasDefaultValue(false);

            builder.Property(f => f.IsArchived)
                .HasColumnName("is_archived")
                .HasDefaultValue(false);

            builder.Property(f => f.IsPinned)
                .HasColumnName("is_pinned")
                .HasDefaultValue(false);

            builder.Property(f => f.IsFavorite)
                .HasColumnName("is_favorite")
                .HasDefaultValue(false);

            builder.Property(f => f.DownloadCount)
                .HasColumnName("download_count")
                .HasDefaultValue(0);

            builder.Property(f => f.LastDownloadedAt)
                .HasColumnName("last_downloaded_at");

            builder.Property(f => f.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("GETDATE()");

            builder.Property(f => f.UpdatedAt)
                .HasColumnName("updated_at");

            builder.Property(f => f.DeletedAt)
                .HasColumnName("deleted_at");

            // Indexes
            builder.HasIndex(f => f.UserId)
                .HasDatabaseName("IX_files_user_id");

            builder.HasIndex(f => f.Slug)
                .HasDatabaseName("IX_files_slug");

            builder.HasIndex(f => f.CreatedAt)
                .HasDatabaseName("IX_files_created_at");

            builder.HasIndex(f => f.MimeType)
                .HasDatabaseName("IX_files_mime_type");

            // Soft delete query filter
            builder.HasQueryFilter(f => f.DeletedAt == null);

            // Relationships
            builder.HasOne(f => f.User)
                .WithMany()
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
