using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Data.Configurations
{
    public class NoteConfiguration : IEntityTypeConfiguration<Note>
    {
        public void Configure(EntityTypeBuilder<Note> builder)
        {
            // Table mapping
            builder.ToTable("notes");

            // Primary key
            builder.HasKey(n => n.NoteId);
            builder.Property(n => n.NoteId)
                .HasColumnName("note_id")
                .ValueGeneratedOnAdd();

            // Properties mapping
            builder.Property(n => n.Name)
                .HasColumnName("name")
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(n => n.Description)
                .HasColumnName("description")
                .HasMaxLength(1000);

            builder.Property(n => n.Content)
                .HasColumnName("content")
                .HasColumnType("nvarchar(max)");

            builder.Property(n => n.Slug)
                .HasColumnName("slug")
                .HasMaxLength(250);

            builder.Property(n => n.Color)
                .HasColumnName("color")
                .HasMaxLength(7);

            builder.Property(n => n.Icon)
                .HasColumnName("icon")
                .HasMaxLength(50);

            builder.Property(n => n.WordCount)
                .HasColumnName("word_count")
                .HasDefaultValue(0);

            builder.Property(n => n.VersionCount)
                .HasColumnName("version_count")
                .HasDefaultValue(1);

            builder.Property(n => n.IsArchived)
                .HasColumnName("is_archived")
                .HasDefaultValue(false);

            builder.Property(n => n.IsPinned)
                .HasColumnName("is_pinned")
                .HasDefaultValue(false);

            builder.Property(n => n.IsFavorite)
                .HasColumnName("is_favorite")
                .HasDefaultValue(false);

            builder.Property(n => n.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            builder.Property(n => n.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("GETDATE()");

            builder.Property(n => n.UpdatedAt)
                .HasColumnName("updated_at");

            builder.Property(n => n.DeletedAt)
                .HasColumnName("deleted_at");

            // Indexes
            builder.HasIndex(n => n.UserId)
                .HasDatabaseName("IX_notes_user_id");

            builder.HasIndex(n => n.Name)
                .HasDatabaseName("IX_notes_name");

            builder.HasIndex(n => n.Slug)
                .HasDatabaseName("IX_notes_slug");

            builder.HasIndex(n => n.IsArchived)
                .HasDatabaseName("IX_notes_is_archived");

            builder.HasIndex(n => n.IsPinned)
                .HasDatabaseName("IX_notes_is_pinned");

            builder.HasIndex(n => n.IsFavorite)
                .HasDatabaseName("IX_notes_is_favorite");

            // Soft delete query filter
            builder.HasQueryFilter(n => n.DeletedAt == null);

            // Relationships
            builder.HasOne(n => n.User)
                .WithMany(u => u.Notes)
                .HasForeignKey(n => n.UserId)
                .HasConstraintName("FK_notes_users_user_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(n => n.Members)
                .WithOne(nm => nm.Note)
                .HasForeignKey(nm => nm.NoteId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(n => n.Versions)
                .WithOne(nv => nv.Note)
                .HasForeignKey(nv => nv.NoteId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}