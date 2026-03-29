using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Data.Configurations
{
    public class ConMessageConfiguration : IEntityTypeConfiguration<ConMessage>
    {
        public void Configure(EntityTypeBuilder<ConMessage> builder)
        {
            builder.ToTable("message", "disc");
            builder.HasKey(m => m.Id);

            builder.Property(m => m.Id).HasColumnName("id").ValueGeneratedOnAdd();
            builder.Property(m => m.UserId).HasColumnName("user_id").IsRequired();
            builder.Property(m => m.TopicId).HasColumnName("topic_id");
            builder.Property(m => m.EntityType).HasColumnName("entity_type").HasMaxLength(50);
            builder.Property(m => m.EntityId).HasColumnName("entity_id");
            builder.Property(m => m.ParentId).HasColumnName("parent_id");
            builder.Property(m => m.Type).HasColumnName("type").HasMaxLength(50);
            builder.Property(m => m.Title).HasColumnName("title").HasMaxLength(255);
            builder.Property(m => m.Content).HasColumnName("content");
            builder.Property(m => m.TrackId).HasColumnName("track_id");
            builder.Property(m => m.Location).HasColumnName("location").HasMaxLength(255);
            builder.Property(m => m.OccurAt).HasColumnName("occur_at");
            builder.Property(m => m.IsSensitive).HasColumnName("is_sensitive").HasDefaultValue(false);
            builder.Property(m => m.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("SYSDATETIME()");
            builder.Property(m => m.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("SYSDATETIME()");
            builder.Property(m => m.DeletedAt).HasColumnName("deleted_at");

            builder.HasOne(m => m.Topic)
                .WithMany()
                .HasForeignKey(m => m.TopicId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(m => m.Parent)
                .WithMany(m => m.Replies)
                .HasForeignKey(m => m.ParentId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Ignore(m => m.Replies); // Loaded manually when needed

            builder.HasIndex(m => m.TopicId);
            builder.HasIndex(m => new { m.EntityType, m.EntityId });
            builder.HasIndex(m => m.ParentId);
            builder.HasIndex(m => m.CreatedAt);
        }
    }
}
