using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Data.Configurations
{
    /// <summary>
    /// EF Core configuration for Entity lookup table (NOT the EntityType model which has different schema)
    /// Maps to: dbo.entities table
    /// Schema: REBUILD_SIMPLIFIED_SCHEMA.sql
    /// Stores entity type definitions: 1=workspace, 2=folder, 3=note, 4=file
    /// NOTE: This is a simple lookup table, different from EntityType model
    /// </summary>
    public class EntityLookupConfiguration : IEntityTypeConfiguration<EntityLookup>
    {
        public void Configure(EntityTypeBuilder<EntityLookup> builder)
        {
            // Table mapping - dbo schema (Default)
            builder.ToTable("entities", "dbo");

            // Primary key - TINYINT
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedNever(); // TINYINT with explicit values

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
                new EntityLookup { Id = 1, Name = "workspace", Description = "Workspace/Project container" },
                new EntityLookup { Id = 2, Name = "folder", Description = "Folder for organizing items" },
                new EntityLookup { Id = 3, Name = "note", Description = "Note/Document" },
                new EntityLookup { Id = 4, Name = "file", Description = "File attachment" }
            );
        }
    }
    
    /// <summary>
    /// Simple entity lookup model for dbo.entities table
    /// </summary>
    public class EntityLookup
    {
        public byte Id { get; set; } // TINYINT
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
