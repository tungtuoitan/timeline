using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperAppModels.Models.DailyLog;

namespace SuperAppDataRepositories.Data.Configurations
{
    public class DailyLogConfiguration : IEntityTypeConfiguration<DailyLog>
    {
        public void Configure(EntityTypeBuilder<DailyLog> builder)
        {
            builder.ToTable("daily_log", "pro");

            builder.HasKey(l => l.Id);

            builder.Property(l => l.Id).HasColumnName("id").ValueGeneratedOnAdd();
            builder.Property(l => l.UserId).HasColumnName("user_id").IsRequired();
            builder.Property(l => l.LogDate).HasColumnName("log_date").HasColumnType("date").IsRequired();
            builder.Property(l => l.ValuesJson).HasColumnName("values_json").HasDefaultValue("{}").IsRequired();
            builder.Property(l => l.TemplateJson).HasColumnName("template_json");
            builder.Property(l => l.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("SYSUTCDATETIME()");
            builder.Property(l => l.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("SYSUTCDATETIME()");
            builder.Property(l => l.DeletedAt).HasColumnName("deleted_at");

            builder.HasIndex(l => new { l.UserId, l.LogDate });
        }
    }

    public class DailyLogFieldTemplateConfiguration : IEntityTypeConfiguration<DailyLogFieldTemplate>
    {
        public void Configure(EntityTypeBuilder<DailyLogFieldTemplate> builder)
        {
            builder.ToTable("daily_log_field_template", "pro");

            builder.HasKey(f => f.Id);

            builder.Property(f => f.Id).HasColumnName("id").ValueGeneratedOnAdd();
            builder.Property(f => f.UserId).HasColumnName("user_id").IsRequired();
            builder.Property(f => f.Section).HasColumnName("section").HasMaxLength(16).IsRequired();
            builder.Property(f => f.FieldKey).HasColumnName("field_key").HasMaxLength(64).IsRequired();
            builder.Property(f => f.Label).HasColumnName("label").HasMaxLength(128).IsRequired();
            builder.Property(f => f.FieldType).HasColumnName("field_type").HasMaxLength(16).IsRequired();
            builder.Property(f => f.RangeMin).HasColumnName("range_min");
            builder.Property(f => f.RangeMax).HasColumnName("range_max");
            builder.Property(f => f.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);
            builder.Property(f => f.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("SYSUTCDATETIME()");
            builder.Property(f => f.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("SYSUTCDATETIME()");
            builder.Property(f => f.DeletedAt).HasColumnName("deleted_at");

            builder.HasIndex(f => new { f.UserId, f.Section });
        }
    }
}
