using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Data.Configurations
{
    public class KtKnowledgeConfiguration : IEntityTypeConfiguration<KtKnowledge>
    {
        public void Configure(EntityTypeBuilder<KtKnowledge> builder)
        {
            builder.ToTable("knowledge", "kt");

            builder.HasKey(k => k.Id);

            builder.Property(k => k.Id).HasColumnName("id").ValueGeneratedOnAdd();
            builder.Property(k => k.UserId).HasColumnName("user_id").IsRequired();
            builder.Property(k => k.ParentId).HasColumnName("parent_id");
            builder.Property(k => k.Title).HasColumnName("title").HasMaxLength(255).IsRequired();
            builder.Property(k => k.Description).HasColumnName("description");
            builder.Property(k => k.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("SYSDATETIME()");
            builder.Property(k => k.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("SYSDATETIME()");
            builder.Property(k => k.DeletedAt).HasColumnName("deleted_at");

            builder.HasOne(k => k.Parent)
                .WithMany(k => k.Children)
                .HasForeignKey(k => k.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(k => k.UserId);
            builder.HasIndex(k => k.ParentId);
            builder.HasIndex(k => k.DeletedAt);
        }
    }
}
