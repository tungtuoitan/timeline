using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperAppModels.Models.Finance;

namespace SuperAppDataRepositories.Data.Configurations
{
    public class FinTransactionConfiguration : IEntityTypeConfiguration<FinTransaction>
    {
        public void Configure(EntityTypeBuilder<FinTransaction> builder)
        {
            builder.ToTable("fin_transaction", "pro");

            builder.HasKey(t => t.Id);

            builder.Property(t => t.Id).HasColumnName("id").ValueGeneratedOnAdd();
            builder.Property(t => t.UserId).HasColumnName("user_id").IsRequired();
            builder.Property(t => t.Account).HasColumnName("account").HasMaxLength(50).IsRequired();
            builder.Property(t => t.OccurredAt).HasColumnName("occurred_at").IsRequired();
            builder.Property(t => t.Asset).HasColumnName("asset").HasMaxLength(20).IsUnicode(false).IsRequired();
            builder.Property(t => t.Amount).HasColumnName("amount").HasColumnType("decimal(28,10)").IsRequired();
            builder.Property(t => t.ValueVnd).HasColumnName("value_vnd").HasColumnType("decimal(19,0)");
            builder.Property(t => t.Kind).HasColumnName("kind").HasMaxLength(10).IsUnicode(false).IsRequired();
            builder.Property(t => t.Category).HasColumnName("category").HasMaxLength(100);
            builder.Property(t => t.Counterparty).HasColumnName("counterparty").HasMaxLength(200);
            builder.Property(t => t.Description).HasColumnName("description").HasMaxLength(1000);
            builder.Property(t => t.GroupKey).HasColumnName("group_key").HasMaxLength(100).IsUnicode(false);
            builder.Property(t => t.Source).HasColumnName("source").HasMaxLength(20).IsUnicode(false).IsRequired();
            builder.Property(t => t.ExternalId).HasColumnName("external_id").HasMaxLength(200);
            builder.Property(t => t.RawJson).HasColumnName("raw_json");
            builder.Property(t => t.Note).HasColumnName("note").HasMaxLength(1000);
            builder.Property(t => t.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("SYSUTCDATETIME()");
            builder.Property(t => t.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("SYSUTCDATETIME()");
            builder.Property(t => t.DeletedAt).HasColumnName("deleted_at");

            builder.HasIndex(t => new { t.UserId, t.OccurredAt });
        }
    }

    public class FinPriceConfiguration : IEntityTypeConfiguration<FinPrice>
    {
        public void Configure(EntityTypeBuilder<FinPrice> builder)
        {
            builder.ToTable("fin_price_cache", "pro");

            builder.HasKey(p => new { p.Asset, p.Quote, p.Date });

            builder.Property(p => p.Date).HasColumnName("date").HasColumnType("date").IsRequired();
            builder.Property(p => p.Asset).HasColumnName("asset").HasMaxLength(20).IsUnicode(false).IsRequired();
            builder.Property(p => p.Quote).HasColumnName("quote").HasMaxLength(10).IsUnicode(false).IsRequired();
            builder.Property(p => p.Price).HasColumnName("price").HasColumnType("decimal(28,10)").IsRequired();
            builder.Property(p => p.Source).HasColumnName("source").HasMaxLength(20).IsUnicode(false).IsRequired();
            builder.Property(p => p.FetchedAt).HasColumnName("fetched_at").HasDefaultValueSql("SYSUTCDATETIME()");
        }
    }
}
