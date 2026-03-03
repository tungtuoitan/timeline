using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Data.Configurations
{
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.ToTable("refresh_tokens", "auth");

            builder.HasKey(t => t.Id);
            builder.Property(t => t.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            builder.Property(t => t.TokenHash)
                .HasColumnName("token_hash")
                .HasMaxLength(256)
                .IsRequired();

            builder.Property(t => t.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            builder.Property(t => t.ExpiresAt)
                .HasColumnName("expires_at")
                .IsRequired();

            builder.Property(t => t.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("GETUTCDATE()")
                .IsRequired();

            builder.Property(t => t.RevokedAt)
                .HasColumnName("revoked_at");

            builder.Property(t => t.ReplacedByTokenHash)
                .HasColumnName("replaced_by_token_hash")
                .HasMaxLength(256);

            builder.Property(t => t.DeviceInfo)
                .HasColumnName("device_info")
                .HasMaxLength(512);

            builder.HasIndex(t => t.TokenHash)
                .HasDatabaseName("IX_refresh_tokens_token_hash")
                .IsUnique();

            builder.HasIndex(t => t.UserId)
                .HasDatabaseName("IX_refresh_tokens_user_id");

            builder.HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
