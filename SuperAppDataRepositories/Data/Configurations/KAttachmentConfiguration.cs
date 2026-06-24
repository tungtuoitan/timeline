using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Data.Configurations
{
    public class KAttachmentConfiguration : IEntityTypeConfiguration<KAttachmentEntity>
    {
        public void Configure(EntityTypeBuilder<KAttachmentEntity> builder)
        {
            builder.ToTable("attachment", "k");

            builder.HasKey(a => a.Id);
            builder.Property(a => a.Id).HasColumnName("id").ValueGeneratedOnAdd();
            builder.Property(a => a.UserId).HasColumnName("user_id").IsRequired();
            builder.Property(a => a.Title).HasColumnName("title").HasMaxLength(500).IsRequired();
            builder.Property(a => a.Type).HasColumnName("type").HasMaxLength(50).HasDefaultValue("code");
            builder.Property(a => a.Language).HasColumnName("language").HasMaxLength(50).IsRequired(false);
            builder.Property(a => a.Content).HasColumnName("content").HasColumnType("nvarchar(max)").IsRequired(false);
            builder.Property(a => a.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);
            builder.Property(a => a.CreatedAt).HasColumnName("created_at");
            builder.Property(a => a.UpdatedAt).HasColumnName("updated_at").IsRequired(false);
            builder.Property(a => a.DeletedAt).HasColumnName("deleted_at").IsRequired(false);

            builder.HasIndex(a => a.UserId).HasDatabaseName("IX_k_attachment_user_id");
        }
    }

    public class KAttachmentLinkConfiguration : IEntityTypeConfiguration<KAttachmentLinkEntity>
    {
        public void Configure(EntityTypeBuilder<KAttachmentLinkEntity> builder)
        {
            builder.ToTable("attachment_link", "k");

            builder.HasKey(l => l.Id);
            builder.Property(l => l.Id).HasColumnName("id").ValueGeneratedOnAdd();
            builder.Property(l => l.AttachmentId).HasColumnName("attachment_id").IsRequired();
            builder.Property(l => l.EntityType).HasColumnName("entity_type").HasMaxLength(20).IsRequired();
            builder.Property(l => l.EntityId).HasColumnName("entity_id").IsRequired();
            builder.Property(l => l.CreatedAt).HasColumnName("created_at");

            builder.HasIndex(l => new { l.EntityType, l.EntityId }).HasDatabaseName("IX_k_att_link_entity");
            builder.HasIndex(l => new { l.AttachmentId, l.EntityType, l.EntityId })
                .HasDatabaseName("IX_k_att_link_unique").IsUnique();
        }
    }
}
