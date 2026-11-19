using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperAppModels.Models.Reflect;

namespace SuperAppDataRepositories.Data.Configurations
{
    public class ReflectPlanItemConfiguration : IEntityTypeConfiguration<ReflectPlanItem>
    {
        public void Configure(EntityTypeBuilder<ReflectPlanItem> builder)
        {
            builder.ToTable("reflect_plan_item", "pro");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.Id).HasColumnName("id").HasMaxLength(36).ValueGeneratedNever();
            builder.Property(r => r.UserId).HasColumnName("user_id").IsRequired();
            builder.Property(r => r.PlanDate).HasColumnName("plan_date").HasColumnType("date").IsRequired();
            builder.Property(r => r.Title).HasColumnName("title").HasMaxLength(400).HasDefaultValue("").IsRequired();
            builder.Property(r => r.Status).HasColumnName("status").HasMaxLength(32).HasDefaultValue("planned").IsRequired();
            builder.Property(r => r.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);
            builder.Property(r => r.Note).HasColumnName("note").HasDefaultValue("").IsRequired();
            builder.Property(r => r.Extra).HasColumnName("extra").HasDefaultValue("{}").IsRequired();
            builder.Property(r => r.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("SYSUTCDATETIME()");
            builder.Property(r => r.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("SYSUTCDATETIME()");
            builder.Property(r => r.DeletedAt).HasColumnName("deleted_at");

            builder.HasIndex(r => new { r.UserId, r.PlanDate, r.SortOrder });
        }
    }
}
