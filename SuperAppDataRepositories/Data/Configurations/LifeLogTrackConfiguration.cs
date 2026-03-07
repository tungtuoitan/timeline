using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Data.Configurations
{
    /// <summary>
    /// EF Core configuration for LifeLogTrack entity
    /// Maps to log.track table
    /// </summary>
    public class LifeLogTrackConfiguration : IEntityTypeConfiguration<LifeLogTrack>
    {
        public void Configure(EntityTypeBuilder<LifeLogTrack> builder)
        {
            builder.ToTable("track", "log");

            builder.HasKey(t => t.Id);

            builder.Property(t => t.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            builder.Property(t => t.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            builder.Property(t => t.Name)
                .HasColumnName("name")
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(t => t.Emoji)
                .HasColumnName("emoji")
                .HasMaxLength(10);

            builder.Property(t => t.Description)
                .HasColumnName("description");

            builder.Property(t => t.IsSensitive)
                .HasColumnName("is_sensitive")
                .HasDefaultValue(false)
                .IsRequired();

            builder.Property(t => t.Color)
                .HasColumnName("color")
                .HasMaxLength(50);

            builder.Property(t => t.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("SYSDATETIME()");

            builder.Property(t => t.UpdatedAt)
                .HasColumnName("updated_at")
                .HasDefaultValueSql("SYSDATETIME()");

            builder.Property(t => t.DeletedAt)
                .HasColumnName("deleted_at");

            builder.HasIndex(t => t.UserId);
            builder.HasIndex(t => t.DeletedAt);
        }
    }
}
