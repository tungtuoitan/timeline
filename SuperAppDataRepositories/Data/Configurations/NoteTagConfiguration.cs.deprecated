using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Data.Configurations;

public class NoteTagConfiguration : IEntityTypeConfiguration<NoteTag>
{
    public void Configure(EntityTypeBuilder<NoteTag> builder)
    {
        // Table mapping
        builder.ToTable("note_tags");

        // Composite primary key (many-to-many relationship)
        builder.HasKey(nt => new { nt.NoteId, nt.TagId });

        // Foreign key properties
        builder.Property(nt => nt.NoteId)
            .HasColumnName("note_id")
            .IsRequired();

        builder.Property(nt => nt.TagId)
            .HasColumnName("tag_id")
            .IsRequired();

        // Association metadata
        builder.Property(nt => nt.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(nt => nt.CreatedBy)
            .HasColumnName("created_by")
            .HasMaxLength(255);

        // Relationships (if Note and Tag entities exist)
        // Note: These relationships might need to be configured based on actual navigation properties
        // in Note and Tag entities
        builder.HasOne<Note>()
            .WithMany()
            .HasForeignKey(nt => nt.NoteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Tag>()
            .WithMany()
            .HasForeignKey(nt => nt.TagId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(nt => nt.NoteId)
            .HasDatabaseName("ix_note_tags_note");

        builder.HasIndex(nt => nt.TagId)
            .HasDatabaseName("ix_note_tags_tag");

        builder.HasIndex(nt => nt.CreatedAt)
            .HasDatabaseName("ix_note_tags_created");
    }
}