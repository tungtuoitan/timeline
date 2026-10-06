using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperAppModels.Models.Auth;

namespace SuperAppDataRepositories.Data.Configurations
{
    public class UserTotpConfiguration : IEntityTypeConfiguration<UserTotp>
    {
        public void Configure(EntityTypeBuilder<UserTotp> builder)
        {
            builder.ToTable("user_totp", "auth");

            builder.HasKey(t => t.UserId);

            builder.Property(t => t.UserId).HasColumnName("user_id").ValueGeneratedNever();
            builder.Property(t => t.Secret).HasColumnName("secret").HasMaxLength(64);
            builder.Property(t => t.PendingSecret).HasColumnName("pending_secret").HasMaxLength(64);
            builder.Property(t => t.EnabledAt).HasColumnName("enabled_at");
            builder.Property(t => t.LastUsedStep).HasColumnName("last_used_step");
            builder.Property(t => t.FailedCount).HasColumnName("failed_count").HasDefaultValue(0);
            builder.Property(t => t.LockedUntil).HasColumnName("locked_until");
            builder.Property(t => t.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("SYSUTCDATETIME()");
            builder.Property(t => t.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("SYSUTCDATETIME()");
        }
    }
}
