using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperAppModels.Models.Reflect;

namespace SuperAppDataRepositories.Data.Configurations
{
    public class ReflectEntryConfiguration : IEntityTypeConfiguration<ReflectEntry>
    {
        public void Configure(EntityTypeBuilder<ReflectEntry> builder)
        {
            builder.ToTable("reflect_entry", "pro");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.Id).HasColumnName("id").HasMaxLength(36).ValueGeneratedNever();
            builder.Property(r => r.UserId).HasColumnName("user_id").IsRequired();
            builder.Property(r => r.Type).HasColumnName("type").HasMaxLength(64).HasDefaultValue("").IsRequired();
            builder.Property(r => r.Think).HasColumnName("think").HasDefaultValue("").IsRequired();
            builder.Property(r => r.Real).HasColumnName("real").HasDefaultValue("").IsRequired();
            builder.Property(r => r.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("SYSUTCDATETIME()");
            builder.Property(r => r.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("SYSUTCDATETIME()");
            builder.Property(r => r.DeletedAt).HasColumnName("deleted_at");
            builder.Property(r => r.Tag).HasColumnName("tag").HasMaxLength(32);
            builder.Property(r => r.ScannedHash).HasColumnName("scanned_hash").HasMaxLength(64);
            builder.Property(r => r.ScannedAt).HasColumnName("scanned_at");

            builder.HasIndex(r => new { r.UserId, r.CreatedAt });
        }
    }
}
