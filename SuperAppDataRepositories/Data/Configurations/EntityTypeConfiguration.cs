using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Data.Configurations;

public class EntityTypeConfiguration : IEntityTypeConfiguration<EntityType>
{
    public void Configure(EntityTypeBuilder<EntityType> builder)
    {
        // Table mapping - dbo schema
        builder.ToTable("entity_types", "dbo");

        // Primary key (string-based)
        builder.HasKey(et => et.TypeName);
        builder.Property(et => et.TypeName)
            .HasColumnName("type_name")
            .HasMaxLength(50)
            .IsRequired();

        // Display properties
        builder.Property(et => et.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(et => et.DisplayPlural)
            .HasColumnName("display_plural")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(et => et.Icon)
            .HasColumnName("icon")
            .HasMaxLength(50);

        builder.Property(et => et.Color)
            .HasColumnName("color")
            .HasMaxLength(7); // Hex color format #RRGGBB

        builder.Property(et => et.Description)
            .HasColumnName("description")
            .HasColumnType("text");

        // Technical properties
        builder.Property(et => et.TableName)
            .HasColumnName("table_name")
            .HasMaxLength(100);

        builder.Property(et => et.SupportsVersioning)
            .HasColumnName("supports_versioning")
            .HasDefaultValue(false);

        builder.Property(et => et.SupportsSharing)
            .HasColumnName("supports_sharing")
            .HasDefaultValue(false);

        // Status
        builder.Property(et => et.IsEnabled)
            .HasColumnName("is_enabled")
            .HasDefaultValue(true);

        // Timestamps
        builder.Property(et => et.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(et => et.UpdatedAt)
            .HasColumnName("updated_at");

        // Indexes
        builder.HasIndex(et => et.IsEnabled)
            .HasDatabaseName("ix_entity_types_enabled")
            .HasFilter("[is_enabled] = 1");

        builder.HasIndex(et => et.DisplayName)
            .HasDatabaseName("ix_entity_types_display_name");

        // Entity type is a system configuration table - no foreign key relationships
        // Other entities reference EntityType.TypeName via string values, not navigation properties
    }
}