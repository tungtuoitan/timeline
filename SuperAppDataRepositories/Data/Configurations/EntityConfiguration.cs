using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Data.Configurations
{
    /// <summary>
    /// EF Core configuration for Entity lookup table
    /// Maps to: dbo.entities table
    /// Schema: REBUILD_SIMPLIFIED_SCHEMA.sql
    /// Stores entity type definitions: 1=workspace, 2=folder, 3=note, 4=file
    /// </summary>
    public class EntityConfiguration : IEntityTypeConfiguration<Entity>
    {
        public void Configure(EntityTypeBuilder<Entity> builder)
        {
            // Table mapping - dbo schema (Default)
            builder.ToTable("entities", "dbo");

            // Primary key - TINYINT
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedNever(); // TINYINT with explicit values (1-4)

            // Properties - EXACTLY match dbo.entities schema
            builder.Property(e => e.Name)
                .HasColumnName("name")
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(e => e.Description)
                .HasColumnName("description")
                .HasMaxLength(255);

            builder.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("GETUTCDATE()");

            // Unique constraint on name
            builder.HasIndex(e => e.Name)
                .HasDatabaseName("UQ_entities_name")
                .IsUnique();

            // Seed data - match REBUILD_SIMPLIFIED_SCHEMA.sql
            builder.HasData(
                new Entity(1, "workspace", "Workspace/Project container"),
                new Entity(2, "folder", "Folder for organizing items"),
                new Entity(3, "note", "Note/Document"),
                new Entity(4, "file", "File attachment")
            );
        }
    }
}
