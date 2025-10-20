using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Data.Configurations
{
    public class NoteVersionConfiguration : IEntityTypeConfiguration<NoteVersion>
    {
        public void Configure(EntityTypeBuilder<NoteVersion> builder)
        {
            // Table mapping
            builder.ToTable("note_versions");

            // Primary key
            builder.HasKey(nv => nv.VersionId);
            builder.Property(nv => nv.VersionId)
                .HasColumnName("version_id")
                .ValueGeneratedOnAdd();

            // Properties mapping
            builder.Property(nv => nv.NoteId)
                .HasColumnName("note_id")
                .IsRequired();

            builder.Property(nv => nv.VersionNumber)
                .HasColumnName("version_number")
                .IsRequired();

            builder.Property(nv => nv.Name)
                .HasColumnName("name")
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(nv => nv.Description)
                .HasColumnName("description")
                .HasMaxLength(1000);

            builder.Property(nv => nv.Content)
                .HasColumnName("content")
                .HasColumnType("nvarchar(max)");

            builder.Property(nv => nv.WordCount)
                .HasColumnName("word_count")
                .HasDefaultValue(0);

            builder.Property(nv => nv.ChangeSummary)
                .HasColumnName("change_summary")
                .HasMaxLength(500);

            builder.Property(nv => nv.CreatedBy)
                .HasColumnName("created_by")
                .IsRequired();

            builder.Property(nv => nv.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("GETDATE()");

            // Configure relationships with explicit foreign keys
            
            // NoteVersion -> Note relationship (configured in NoteConfiguration)
            
            // NoteVersion -> User relationship (the creator user)
            builder.HasOne(nv => nv.Creator)
                .WithMany() // User can create many versions
                .HasForeignKey(nv => nv.CreatedBy)
                .HasConstraintName("FK_note_versions_users_created_by")
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(nv => nv.NoteId)
                .HasDatabaseName("IX_note_versions_note_id");

            builder.HasIndex(nv => new { nv.NoteId, nv.VersionNumber })
                .HasDatabaseName("IX_note_versions_note_version")
                .IsUnique(); // One version number per note

            builder.HasIndex(nv => nv.CreatedBy)
                .HasDatabaseName("IX_note_versions_created_by");

            builder.HasIndex(nv => nv.CreatedAt)
                .HasDatabaseName("IX_note_versions_created_at");
        }
    }
}