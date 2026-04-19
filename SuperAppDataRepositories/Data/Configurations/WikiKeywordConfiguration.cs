using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Data.Configurations
{
    public class WikiKeywordConfiguration : IEntityTypeConfiguration<WikiKeyword>
    {
        public void Configure(EntityTypeBuilder<WikiKeyword> builder)
        {
            builder.ToTable("keyword", "wiki");

            builder.HasKey(w => w.Id);
            builder.Property(w => w.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            builder.Property(w => w.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            builder.Property(w => w.Name)
                .HasColumnName("name")
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(w => w.Description)
                .HasColumnName("description");

            builder.Property(w => w.IconBase64)
                .HasColumnName("icon_base64");

            builder.Property(w => w.Views)
                .HasColumnName("views")
                .HasDefaultValue(0);

            builder.Property(w => w.Reads)
                .HasColumnName("reads")
                .HasDefaultValue(0);

            builder.Property(w => w.Edits)
                .HasColumnName("edits")
                .HasDefaultValue(0);

            builder.Property(w => w.PosX)
                .HasColumnName("pos_x");

            builder.Property(w => w.PosY)
                .HasColumnName("pos_y");

            builder.Property(w => w.PinnedPosition)
                .HasColumnName("pinned_position")
                .HasDefaultValue(false);

            builder.Property(w => w.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(w => w.UpdatedAt)
                .HasColumnName("updated_at");

            builder.Property(w => w.DeletedAt)
                .HasColumnName("deleted_at");

            builder.HasIndex(w => w.UserId)
                .HasDatabaseName("IX_wiki_keyword_user")
                .HasFilter("[deleted_at] IS NULL");

            builder.HasOne(w => w.User)
                .WithMany()
                .HasForeignKey(w => w.UserId)
                .HasConstraintName("FK_wiki_keyword_users")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(w => w.Synonyms)
                .WithOne(s => s.Keyword)
                .HasForeignKey(s => s.KeywordId)
                .HasConstraintName("FK_wiki_keyword_synonym_keyword")
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
