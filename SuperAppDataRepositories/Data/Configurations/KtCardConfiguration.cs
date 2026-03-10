using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Data.Configurations
{
    public class KtCardConfiguration : IEntityTypeConfiguration<KtCard>
    {
        public void Configure(EntityTypeBuilder<KtCard> builder)
        {
            builder.ToTable("card", "kt");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedOnAdd();
            builder.Property(c => c.KnowledgeId).HasColumnName("knowledge_id").IsRequired();
            builder.Property(c => c.ParentCardId).HasColumnName("parent_card_id");
            builder.Property(c => c.UserId).HasColumnName("user_id").IsRequired();
            builder.Property(c => c.Keyword).HasColumnName("keyword").HasMaxLength(255).HasDefaultValue("");
            builder.Property(c => c.Title).HasColumnName("title").HasMaxLength(255).IsRequired();
            builder.Property(c => c.Description).HasColumnName("description");
            builder.Property(c => c.IsDefinition).HasColumnName("is_definition").HasDefaultValue(true).IsRequired();
            builder.Property(c => c.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("SYSDATETIME()");
            builder.Property(c => c.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("SYSDATETIME()");
            builder.Property(c => c.DeletedAt).HasColumnName("deleted_at");

            builder.HasOne(c => c.Knowledge)
                .WithMany()
                .HasForeignKey(c => c.KnowledgeId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(c => c.ParentCard)
                .WithMany()
                .HasForeignKey(c => c.ParentCardId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(c => c.KnowledgeId);
            builder.HasIndex(c => c.ParentCardId);
            builder.HasIndex(c => c.UserId);
            builder.HasIndex(c => c.DeletedAt);
        }
    }
}
