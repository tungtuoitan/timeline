using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Data.Configurations
{
    public class NoteConfiguration : IEntityTypeConfiguration<Note>
    {
        public void Configure(EntityTypeBuilder<Note> builder)
        {
            // Table mapping - dbo schema
            builder.ToTable("notes", "dbo");

            // Primary key
            builder.HasKey(n => n.Id);
            builder.Property(n => n.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            // Properties - EXACTLY match dbo.notes schema
            builder.Property(n => n.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            builder.Property(n => n.Name)
                .HasColumnName("name")
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(n => n.Description)
                .HasColumnName("description")
                .HasColumnType("nvarchar(max)");

            builder.Property(n => n.StatusCode)
                .HasColumnName("status_code")
                .HasMaxLength(50);

            builder.Property(n => n.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(n => n.UpdatedAt)
                .HasColumnName("updated_at");

            builder.Property(n => n.DeletedAt)
                .HasColumnName("deleted_at");

            builder.Property(n => n.CopyInfo)
                .HasColumnName("copy_info")
                .HasColumnType("nvarchar(max)");

            // Indexes - match schema
            builder.HasIndex(n => n.UserId)
                .HasDatabaseName("IX_notes_user")
                .HasFilter("[deleted_at] IS NULL");

            builder.HasIndex(n => n.CreatedAt)
                .HasDatabaseName("IX_notes_created");

            // Relationships
            builder.HasOne(n => n.User)
                .WithMany(u => u.Notes)
                .HasForeignKey(n => n.UserId)
                .HasConstraintName("FK_notes_users_user_id")
                .OnDelete(DeleteBehavior.Restrict);

            // Note: status_code has no FK constraint - just a simple string field
        }
    }
}