using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Data.Configurations
{
    public class NoteMemberConfiguration : IEntityTypeConfiguration<NoteMember>
    {
        public void Configure(EntityTypeBuilder<NoteMember> builder)
        {
            // Table mapping
            builder.ToTable("note_members");

            // Primary key
            builder.HasKey(nm => nm.MemberId);
            builder.Property(nm => nm.MemberId)
                .HasColumnName("member_id")
                .ValueGeneratedOnAdd();

            // Properties mapping
            builder.Property(nm => nm.NoteId)
                .HasColumnName("note_id")
                .IsRequired();

            builder.Property(nm => nm.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            builder.Property(nm => nm.Role)
                .HasColumnName("role")
                .HasMaxLength(50)
                .IsRequired()
                .HasDefaultValue("viewer");

            builder.Property(nm => nm.InvitedBy)
                .HasColumnName("invited_by");

            builder.Property(nm => nm.InvitationStatus)
                .HasColumnName("invitation_status")
                .HasMaxLength(50)
                .IsRequired()
                .HasDefaultValue("active");

            builder.Property(nm => nm.InvitedAt)
                .HasColumnName("invited_at");

            builder.Property(nm => nm.JoinedAt)
                .HasColumnName("joined_at");

            builder.Property(nm => nm.LastAccessedAt)
                .HasColumnName("last_accessed_at");

            builder.Property(nm => nm.DeletedAt)
                .HasColumnName("deleted_at");

            // Configure relationships with explicit foreign keys to prevent EF Core confusion
            
            // NoteMember -> Note relationship (configured in NoteConfiguration)

            // NoteMember -> User relationship (the member user)
            builder.HasOne(nm => nm.User)
                .WithMany() // User can be member of many notes
                .HasForeignKey(nm => nm.UserId)
                .HasConstraintName("FK_note_members_users_user_id")
                .OnDelete(DeleteBehavior.Restrict);

            // NoteMember -> User relationship (the inviter user) - THIS FIXES THE AMBIGUITY
            builder.HasOne(nm => nm.Inviter)
                .WithMany() // User can invite many people
                .HasForeignKey(nm => nm.InvitedBy)
                .HasConstraintName("FK_note_members_users_invited_by")
                .OnDelete(DeleteBehavior.SetNull); // If inviter is deleted, set to null

            // Indexes
            builder.HasIndex(nm => nm.NoteId)
                .HasDatabaseName("IX_note_members_note_id");

            builder.HasIndex(nm => nm.UserId)
                .HasDatabaseName("IX_note_members_user_id");

            builder.HasIndex(nm => new { nm.NoteId, nm.UserId })
                .HasDatabaseName("IX_note_members_note_user")
                .IsUnique(); // One user can only be member of a note once

            builder.HasIndex(nm => nm.InvitedBy)
                .HasDatabaseName("IX_note_members_invited_by");

            builder.HasIndex(nm => nm.Role)
                .HasDatabaseName("IX_note_members_role");

            // Soft delete query filter
            builder.HasQueryFilter(nm => nm.DeletedAt == null);
        }
    }
}