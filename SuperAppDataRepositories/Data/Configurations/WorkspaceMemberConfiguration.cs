using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Data.Configurations
{
    public class WorkspaceMemberConfiguration : IEntityTypeConfiguration<WorkspaceMember>
    {
        public void Configure(EntityTypeBuilder<WorkspaceMember> builder)
        {
            // Table mapping
            builder.ToTable("workspace_members");

            // Primary key
            builder.HasKey(wm => wm.MemberId);
            builder.Property(wm => wm.MemberId)
                .HasColumnName("member_id")
                .ValueGeneratedOnAdd();

            // Properties
            builder.Property(wm => wm.WorkspaceId)
                .HasColumnName("workspace_id")
                .IsRequired();

            builder.Property(wm => wm.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            builder.Property(wm => wm.Role)
                .HasColumnName("role")
                .HasMaxLength(50)
                .HasDefaultValue("viewer");

            builder.Property(wm => wm.InvitedBy)
                .HasColumnName("invited_by");

            builder.Property(wm => wm.InvitationStatus)
                .HasColumnName("invitation_status")
                .HasMaxLength(20)
                .HasDefaultValue("active");

            builder.Property(wm => wm.CustomPermissions)
                .HasColumnName("custom_permissions")
                .HasColumnType("nvarchar(max)");

            builder.Property(wm => wm.InvitedAt)
                .HasColumnName("invited_at");

            builder.Property(wm => wm.JoinedAt)
                .HasColumnName("joined_at");

            builder.Property(wm => wm.LastAccessedAt)
                .HasColumnName("last_accessed_at");

            builder.Property(wm => wm.DeletedAt)
                .HasColumnName("deleted_at");

            // Indexes
            builder.HasIndex(wm => wm.WorkspaceId)
                .HasDatabaseName("IX_workspace_members_workspace");

            builder.HasIndex(wm => wm.UserId)
                .HasDatabaseName("IX_workspace_members_user");

            builder.HasIndex(wm => new { wm.WorkspaceId, wm.UserId })
                .HasDatabaseName("UQ_workspace_members_unique")
                .IsUnique()
                .HasFilter("[deleted_at] IS NULL");

            builder.HasIndex(wm => wm.Role)
                .HasDatabaseName("IX_workspace_members_role");

            // Soft delete query filter
            builder.HasQueryFilter(wm => wm.DeletedAt == null);

            // Relationships - THIS IS THE FIX FOR THE ERROR
            // Relationship 1: WorkspaceMember.User -> User (via UserId)
            builder.HasOne(wm => wm.User)
                .WithMany() // User can have many WorkspaceMembers
                .HasForeignKey(wm => wm.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relationship 2: WorkspaceMember.Inviter -> User (via InvitedBy)
            builder.HasOne(wm => wm.Inviter)
                .WithMany() // User can invite many WorkspaceMembers
                .HasForeignKey(wm => wm.InvitedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Relationship 3: WorkspaceMember.Workspace -> Workspace
            builder.HasOne(wm => wm.Workspace)
                .WithMany(w => w.Members)
                .HasForeignKey(wm => wm.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}