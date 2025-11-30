using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Data.Configurations
{
    public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
    {
        public void Configure(EntityTypeBuilder<UserProfile> builder)
        {
            // Table mapping - urm schema (User Resource Management)
            builder.ToTable("user_profiles", "urm");

            builder.HasKey(up => up.Email);

            builder.Property(up => up.Email)
                .HasColumnName("email")
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(up => up.AppC)
                .HasColumnName("app_code")
                .HasMaxLength(50);

            builder.Property(up => up.Parents)
                .HasColumnName("parents")
                .HasColumnType("nvarchar(max)");

            builder.Property(up => up.Priorities)
                .HasColumnName("priorities")
                .HasColumnType("nvarchar(max)");

            builder.Property(up => up.Statuses)
                .HasColumnName("statuses")
                .HasColumnType("nvarchar(max)");

            builder.Property(up => up.Types)
                .HasColumnName("types")
                .HasColumnType("nvarchar(max)");

            builder.Property(up => up.RepeatTypes)
                .HasColumnName("repeat_types")
                .HasColumnType("nvarchar(max)");

            builder.Property(up => up.IsUpdatedTodays)
                .HasColumnName("is_updated_todays")
                .HasMaxLength(50);

            builder.Property(up => up.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(up => up.UpdatedAt)
                .HasColumnName("updated_at");

            builder.Property(up => up.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true);

            builder.HasIndex(up => up.AppC)
                .HasDatabaseName("IX_user_profiles_app_code");

            builder.HasIndex(up => up.IsActive)
                .HasDatabaseName("IX_user_profiles_is_active");
        }
    }
}
