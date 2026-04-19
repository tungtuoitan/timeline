using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Data.Configurations
{
    public class WikiKeywordSynonymConfiguration : IEntityTypeConfiguration<WikiKeywordSynonym>
    {
        public void Configure(EntityTypeBuilder<WikiKeywordSynonym> builder)
        {
            builder.ToTable("keyword_synonym", "wiki");

            builder.HasKey(s => s.Id);
            builder.Property(s => s.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            builder.Property(s => s.KeywordId)
                .HasColumnName("keyword_id")
                .IsRequired();

            builder.Property(s => s.Synonym)
                .HasColumnName("synonym")
                .HasMaxLength(255)
                .IsRequired();

            builder.HasIndex(s => new { s.KeywordId, s.Synonym })
                .HasDatabaseName("UQ_wiki_keyword_synonym")
                .IsUnique();

            builder.HasIndex(s => s.KeywordId)
                .HasDatabaseName("IX_wiki_keyword_synonym_keyword");
        }
    }
}
