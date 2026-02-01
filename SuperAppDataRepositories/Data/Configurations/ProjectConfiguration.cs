using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Data.Configurations
{
    /// <summary>
    /// EF Core configuration for Project entity
    /// Maps to pro.project table
    /// </summary>
    public class ProjectConfiguration : IEntityTypeConfiguration<Project>
    {
        public void Configure(EntityTypeBuilder<Project> builder)
        {
            builder.ToTable("project", "pro");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            builder.Property(p => p.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            builder.Property(p => p.Name)
                .HasColumnName("name")
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(p => p.Description)
                .HasColumnName("description");

            builder.Property(p => p.Status)
                .HasColumnName("status")
                .HasMaxLength(50)
                .HasDefaultValue("open")
                .IsRequired();

            builder.Property(p => p.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("SYSDATETIME()");

            builder.Property(p => p.UpdatedAt)
                .HasColumnName("updated_at")
                .HasDefaultValueSql("SYSDATETIME()");

            builder.Property(p => p.DeletedAt)
                .HasColumnName("deleted_at");

            // Index for user_id for faster queries
            builder.HasIndex(p => p.UserId);
        }
    }
}
