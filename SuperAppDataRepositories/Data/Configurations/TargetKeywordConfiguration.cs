using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Data.Configurations
{
    /// <summary>
    /// EF Core configuration for TargetKeyword entity
    /// Maps to pro.TargetKeywords table
    /// </summary>
    public class TargetKeywordConfiguration : IEntityTypeConfiguration<TargetKeyword>
    {
        public void Configure(EntityTypeBuilder<TargetKeyword> builder)
        {
            builder.ToTable("target_keywords", "pro");

            builder.HasKey(t => t.Id);

            builder.Property(t => t.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            builder.Property(t => t.TargetId)
                .HasColumnName("target_id")
                .IsRequired();

            builder.Property(t => t.TargetType)
                .HasColumnName("target_type")
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(t => t.KeywordId)
                .HasColumnName("keyword_id")
                .IsRequired();

            builder.HasIndex(t => new { t.TargetId, t.TargetType });
            builder.HasIndex(t => t.KeywordId);
        }
    }
}
