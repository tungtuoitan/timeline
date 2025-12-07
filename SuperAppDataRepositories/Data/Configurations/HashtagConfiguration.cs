using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Data.Configurations
{
    /// <summary>
    /// EF Core configuration for Hashtag entity
    /// Maps to: dbo.hashtags table
    /// Schema: REBUILD_SIMPLIFIED_SCHEMA.sql
    /// </summary>
    public class HashtagConfiguration : IEntityTypeConfiguration<Hashtag>
    {
        public void Configure(EntityTypeBuilder<Hashtag> builder)
        {
            // Table mapping - dbo schema (Default)
            builder.ToTable("hashtags", "dbo");

            // Primary key
            builder.HasKey(h => h.Id);
            builder.Property(h => h.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            // Properties - EXACTLY match dbo.hashtags schema
            builder.Property(h => h.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            builder.Property(h => h.Name)
                .HasColumnName("name")
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(h => h.UsageCount)
                .HasColumnName("usage_count")
                .HasDefaultValue(0);

            builder.Property(h => h.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(h => h.UpdatedAt)
                .HasColumnName("updated_at");

            builder.Property(h => h.DeletedAt)
                .HasColumnName("deleted_at");

            // Unique constraint
            builder.HasIndex(h => new { h.UserId, h.Name })
                .HasDatabaseName("UQ_hashtags_user_name")
                .IsUnique();

            // Indexes
            builder.HasIndex(h => h.UserId)
                .HasDatabaseName("IX_hashtags_user")
                .HasFilter("[deleted_at] IS NULL");

            builder.HasIndex(h => h.Name)
                .HasDatabaseName("IX_hashtags_name");

            // Relationships
            builder.HasOne(h => h.User)
                .WithMany()
                .HasForeignKey(h => h.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
