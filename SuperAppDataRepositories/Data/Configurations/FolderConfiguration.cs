using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Data.Configurations
{
    /// <summary>
    /// Configuration for Folder entity (renamed from old Tag entity)
    /// Maps to 'folders' table (renamed from 'tags' in migration)
    /// </summary>
    public class FolderConfiguration : IEntityTypeConfiguration<Folder>
    {
        public void Configure(EntityTypeBuilder<Folder> builder)
        {
            // Table mapping - dbo schema
            // IMPORTANT: This maps to 'folders' table (renamed from old 'tags' table in migration)
            builder.ToTable("folders", "ws");

            // Primary key
            builder.HasKey(f => f.FolderId);
            builder.Property(f => f.FolderId)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            // Properties - EXACTLY match ws.folders schema
            builder.Property(f => f.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            builder.Property(f => f.Name)
                .HasColumnName("name")
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(f => f.Description)
                .HasColumnName("description")
                .HasColumnType("nvarchar(max)");

            builder.Property(f => f.Color)
                .HasColumnName("color")
                .HasMaxLength(7)
                .HasDefaultValue("#F59E0B");

            builder.Property(f => f.Icon)
                .HasColumnName("icon")
                .HasMaxLength(50)
                .HasDefaultValue("📁");

            builder.Property(f => f.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(f => f.UpdatedAt)
                .HasColumnName("updated_at");

            builder.Property(f => f.DeletedAt)
                .HasColumnName("deleted_at");

            // Indexes - match schema
            builder.HasIndex(f => f.UserId)
                .HasDatabaseName("IX_folders_user")
                .HasFilter("[deleted_at] IS NULL");

            builder.HasIndex(f => f.Name)
                .HasDatabaseName("IX_folders_name");

            builder.HasIndex(f => f.CreatedAt)
                .HasDatabaseName("IX_folders_created");

            // Soft delete query filter
            builder.HasQueryFilter(f => f.DeletedAt == null);

            // Relationships
            builder.HasOne(f => f.User)
                .WithMany() // User can have many Folders
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // WorkspaceItem relationships are handled in WorkspaceItemConfiguration
        }
    }
}
