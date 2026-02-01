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
            builder.HasKey(u => u.Id);
            builder.Property(u => u.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            // Properties - EXACTLY match urm.users schema
            builder.Property(u => u.Email)
                .HasColumnName("email")
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(u => u.Phone)
                .HasColumnName("phone")
                .HasMaxLength(20);

            builder.Property(u => u.Password)
                .HasColumnName("password")
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(u => u.AuthType)
                .HasColumnName("auth_type")
                .HasMaxLength(50)
                .HasDefaultValue("local");

            builder.Property(u => u.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true);

            // Google OAuth tokens
            builder.Property(u => u.GoogleAccessToken)
                .HasColumnName("google_access_token")
                .HasMaxLength(2048);

            builder.Property(u => u.GoogleRefreshToken)
                .HasColumnName("google_refresh_token")
                .HasMaxLength(512);

            builder.Property(u => u.GoogleTokenExpiresAt)
                .HasColumnName("google_token_expires_at");

            builder.Property(u => u.LastLoginAt)
                .HasColumnName("last_login_at");

            builder.Property(u => u.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(u => u.UpdatedAt)
                .HasColumnName("updated_at");

            builder.Property(u => u.DeletedAt)
                .HasColumnName("deleted_at");

            // Indexes - match schema
            builder.HasIndex(u => u.Email)
                .HasDatabaseName("IX_users_email")
                .HasFilter("[deleted_at] IS NULL");

            builder.HasIndex(u => u.Phone)
                .HasDatabaseName("IX_users_phone")
                .HasFilter("[deleted_at] IS NULL");

            // Relationships are configured in their respective entity configurations
            // to avoid conflicts and duplicate foreign key columns:
            // - Note-User relationship: configured in NoteConfiguration
            // - Workspace-User relationship: configured in WorkspaceConfiguration
            // - Tag-User relationship: configured in TagConfiguration
            // - WorkspaceMember relationships: configured in WorkspaceMemberConfiguration
        }
    }
}