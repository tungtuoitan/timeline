using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Data.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            // Table mapping - urm schema (User Resource Management)
            builder.ToTable("users", "urm");

            // Primary key
            builder.HasKey(u => u.UserId);
            builder.Property(u => u.UserId)
                .HasColumnName("user_id")
                .ValueGeneratedOnAdd();

            // Properties
            builder.Property(u => u.Username)
                .HasColumnName("username")
                .HasMaxLength(255);

            builder.Property(u => u.Email)
                .HasColumnName("email")
                .HasMaxLength(255);

            builder.Property(u => u.PasswordHash)
                .HasColumnName("password_hash")
                .HasMaxLength(255);

            builder.Property(u => u.DisplayName)
                .HasColumnName("display_name")
                .HasMaxLength(255);

            builder.Property(u => u.AvatarUrl)
                .HasColumnName("avatar_url")
                .HasMaxLength(500);

            builder.Property(u => u.Bio)
                .HasColumnName("bio")
                .HasMaxLength(1000);

            builder.Property(u => u.Preferences)
                .HasColumnName("preferences")
                .HasColumnType("nvarchar(max)");

            builder.Property(u => u.EmailVerified)
                .HasColumnName("email_verified")
                .HasDefaultValue(false);

            builder.Property(u => u.LastLoginAt)
                .HasColumnName("last_login_at");

            builder.Property(u => u.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true);

            builder.Property(u => u.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("GETDATE()");

            builder.Property(u => u.UpdatedAt)
                .HasColumnName("updated_at");

            builder.Property(u => u.DeletedAt)
                .HasColumnName("deleted_at");

            // Indexes
            builder.HasIndex(u => u.Email)
                .HasDatabaseName("IX_users_email")
                .IsUnique()
                .HasFilter("[deleted_at] IS NULL");

            builder.HasIndex(u => u.Username)
                .HasDatabaseName("IX_users_username")
                .IsUnique()
                .HasFilter("[deleted_at] IS NULL");

            builder.HasIndex(u => u.IsActive)
                .HasDatabaseName("IX_users_is_active")
                .HasFilter("[deleted_at] IS NULL");

            // Soft delete query filter
            builder.HasQueryFilter(u => u.DeletedAt == null);

            // Relationships are configured in their respective entity configurations
            // to avoid conflicts and duplicate foreign key columns:
            // - Note-User relationship: configured in NoteConfiguration
            // - Workspace-User relationship: configured in WorkspaceConfiguration
            // - Tag-User relationship: configured in TagConfiguration
            // - WorkspaceMember relationships: configured in WorkspaceMemberConfiguration
        }
    }
}