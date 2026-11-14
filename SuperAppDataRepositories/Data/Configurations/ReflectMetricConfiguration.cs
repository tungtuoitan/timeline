using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperAppModels.Models.Reflect;

namespace SuperAppDataRepositories.Data.Configurations
{
    public class ReflectMetricConfiguration : IEntityTypeConfiguration<ReflectMetric>
    {
        public void Configure(EntityTypeBuilder<ReflectMetric> builder)
        {
            builder.ToTable("reflect_metric", "pro");
            builder.HasKey(m => m.Id);
            builder.Property(m => m.Id).HasColumnName("id").ValueGeneratedOnAdd();
            builder.Property(m => m.UserId).HasColumnName("user_id").IsRequired();
            builder.Property(m => m.Code).HasColumnName("code").HasMaxLength(64).IsRequired();
            builder.Property(m => m.Label).HasColumnName("label").HasMaxLength(128).IsRequired();
            builder.Property(m => m.Unit).HasColumnName("unit").HasMaxLength(32).HasDefaultValue("").IsRequired();
            builder.Property(m => m.Aggregate).HasColumnName("aggregate").HasMaxLength(8).HasDefaultValue("sum").IsRequired();
            builder.Property(m => m.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("SYSUTCDATETIME()");
            builder.Property(m => m.DeletedAt).HasColumnName("deleted_at");
            builder.HasIndex(m => new { m.UserId, m.Code }).IsUnique();
        }
    }

    public class ReflectObservationConfiguration : IEntityTypeConfiguration<ReflectObservation>
    {
        public void Configure(EntityTypeBuilder<ReflectObservation> builder)
        {
            builder.ToTable("reflect_observation", "pro");
            builder.HasKey(o => o.Id);
            builder.Property(o => o.Id).HasColumnName("id").ValueGeneratedOnAdd();
            builder.Property(o => o.UserId).HasColumnName("user_id").IsRequired();
            builder.Property(o => o.EntryId).HasColumnName("entry_id").HasMaxLength(36).IsRequired();
            builder.Property(o => o.MetricId).HasColumnName("metric_id").IsRequired();
            builder.Property(o => o.OccurredOn).HasColumnName("occurred_on").HasColumnType("date");
            builder.Property(o => o.Value).HasColumnName("value").HasColumnType("decimal(18,4)");
            builder.Property(o => o.Quote).HasColumnName("quote").HasMaxLength(400).HasDefaultValue("").IsRequired();
            builder.Property(o => o.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("SYSUTCDATETIME()");
            builder.HasOne(o => o.Metric).WithMany().HasForeignKey(o => o.MetricId);
            builder.HasIndex(o => new { o.EntryId, o.MetricId, o.OccurredOn }).IsUnique();
            builder.HasIndex(o => new { o.UserId, o.MetricId, o.OccurredOn });
        }
    }
}
