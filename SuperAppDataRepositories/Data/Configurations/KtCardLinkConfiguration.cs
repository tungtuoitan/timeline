using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Data.Configurations
{
    public class KtCardLinkConfiguration : IEntityTypeConfiguration<KtCardLink>
    {
        public void Configure(EntityTypeBuilder<KtCardLink> builder)
        {
            builder.ToTable("card_link", "kt");

            builder.HasKey(l => l.Id);

            builder.Property(l => l.Id).HasColumnName("id").ValueGeneratedOnAdd();
            builder.Property(l => l.SourceCardId).HasColumnName("source_card_id").IsRequired();
            builder.Property(l => l.TargetCardId).HasColumnName("target_card_id").IsRequired();

            builder.HasAlternateKey(l => new { l.SourceCardId, l.TargetCardId });

            builder.HasOne(l => l.SourceCard)
                .WithMany(c => c.SourceLinks)
                .HasForeignKey(l => l.SourceCardId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(l => l.TargetCard)
                .WithMany()
                .HasForeignKey(l => l.TargetCardId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(l => l.SourceCardId);
            builder.HasIndex(l => l.TargetCardId);
        }
    }
}
