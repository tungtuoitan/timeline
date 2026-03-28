using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Data.Configurations
{
    public class KTestConfiguration : IEntityTypeConfiguration<KTestEntity>
    {
        public void Configure(EntityTypeBuilder<KTestEntity> builder)
        {
            builder.ToTable("test", "k");

            builder.HasKey(t => t.Id);
            builder.Property(t => t.Id).HasColumnName("id").ValueGeneratedOnAdd();

            builder.Property(t => t.KnowledgeId).HasColumnName("knowledge_id").IsRequired();
            builder.Property(t => t.UserId).HasColumnName("user_id").IsRequired();
            builder.Property(t => t.Title).HasColumnName("title").HasMaxLength(500).IsRequired();
            builder.Property(t => t.Level).HasColumnName("level").HasDefaultValue(1);
            builder.Property(t => t.Mode).HasColumnName("mode").HasMaxLength(50).HasDefaultValue("standard");
            builder.Property(t => t.Status).HasColumnName("status").HasMaxLength(50).HasDefaultValue("active");

            builder.Property(t => t.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("GETUTCDATE()");
            builder.Property(t => t.UpdatedAt).HasColumnName("updated_at");
            builder.Property(t => t.DeletedAt).HasColumnName("deleted_at");

            builder.HasIndex(t => new { t.KnowledgeId, t.UserId, t.DeletedAt })
                .HasDatabaseName("IX_k_test_knowledge_user")
                .HasFilter("[deleted_at] IS NULL");

            builder.HasOne(t => t.Knowledge)
                .WithMany()
                .HasForeignKey(t => t.KnowledgeId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
