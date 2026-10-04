using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Data.Configurations
{
    /// <summary>
    /// EF Core configuration for LifeLogLog entity
    /// Maps to log.log table
    /// </summary>
    public class LifeLogLogConfiguration : IEntityTypeConfiguration<LifeLogLog>
    {
        public void Configure(EntityTypeBuilder<LifeLogLog> builder)
        {
            builder.ToTable("log", "log");

            builder.HasKey(l => l.Id);

            builder.Property(l => l.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            builder.Property(l => l.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            builder.Property(l => l.Type)
                .HasColumnName("type")
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(l => l.TrackId)
                .HasColumnName("track_id");

            builder.Property(l => l.Title)
                .HasColumnName("title")
                .HasMaxLength(255);

            builder.Property(l => l.Description)
                .HasColumnName("description");

            builder.Property(l => l.IsSensitive)
                .HasColumnName("is_sensitive")
                .HasDefaultValue(false)
                .IsRequired();

            builder.Property(l => l.Location)
                .HasColumnName("location")
                .HasMaxLength(255);

            builder.Property(l => l.OccurAt)
                .HasColumnName("occur_at");

            builder.Property(l => l.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.Property(l => l.UpdatedAt)
                .HasColumnName("updated_at")
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.Property(l => l.DeletedAt)
                .HasColumnName("deleted_at");

            // Relationships
            builder.HasOne(l => l.Track)
                .WithMany()
                .HasForeignKey(l => l.TrackId)
                .OnDelete(DeleteBehavior.SetNull);

            // Indexes
            builder.HasIndex(l => l.UserId);
            builder.HasIndex(l => l.Type);
            builder.HasIndex(l => l.CreatedAt);
            builder.HasIndex(l => l.TrackId);
        }
    }
}
