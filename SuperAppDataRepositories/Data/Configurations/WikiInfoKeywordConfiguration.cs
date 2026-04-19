using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Data.Configurations
{
    public class WikiInfoKeywordConfiguration : IEntityTypeConfiguration<WikiInfoKeyword>
    {
        public void Configure(EntityTypeBuilder<WikiInfoKeyword> builder)
        {
            builder.ToTable("info_keyword", "wiki");

            builder.HasKey(ik => new { ik.InfoId, ik.KeywordId });

            builder.Property(ik => ik.InfoId)
                .HasColumnName("info_id");

            builder.Property(ik => ik.KeywordId)
                .HasColumnName("keyword_id");

            builder.HasIndex(ik => ik.KeywordId)
                .HasDatabaseName("IX_wiki_info_keyword_keyword");

            builder.HasOne(ik => ik.Info)
                .WithMany(i => i.InfoKeywords)
                .HasForeignKey(ik => ik.InfoId)
                .HasConstraintName("FK_wiki_info_keyword_info")
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(ik => ik.Keyword)
                .WithMany(k => k.InfoKeywords)
                .HasForeignKey(ik => ik.KeywordId)
                .HasConstraintName("FK_wiki_info_keyword_keyword")
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
