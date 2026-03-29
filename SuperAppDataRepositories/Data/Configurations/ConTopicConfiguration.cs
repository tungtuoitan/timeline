using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Data.Configurations
{
    public class ConTopicConfiguration : IEntityTypeConfiguration<ConTopic>
    {
        public void Configure(EntityTypeBuilder<ConTopic> builder)
        {
            builder.ToTable("topic", "disc");
            builder.HasKey(t => t.Id);

            builder.Property(t => t.Id).HasColumnName("id").ValueGeneratedOnAdd();
            builder.Property(t => t.UserId).HasColumnName("user_id").IsRequired();
            builder.Property(t => t.EntityType).HasColumnName("entity_type").HasMaxLength(50);
            builder.Property(t => t.EntityId).HasColumnName("entity_id");
            builder.Property(t => t.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
            builder.Property(t => t.Description).HasColumnName("description");
            builder.Property(t => t.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("SYSDATETIME()");
            builder.Property(t => t.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("SYSDATETIME()");
            builder.Property(t => t.DeletedAt).HasColumnName("deleted_at");

            builder.HasIndex(t => new { t.EntityType, t.EntityId });
            builder.HasIndex(t => t.UserId);
        }
    }
}
