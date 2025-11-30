using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Data.Configurations;

public class StandardRegistryConfiguration : IEntityTypeConfiguration<StandardRegistry>
{
    public void Configure(EntityTypeBuilder<StandardRegistry> builder)
    {
        // Table mapping - dbo schema
        builder.ToTable("standard_registry", "dbo");

        // Primary key
        builder.HasKey(sr => sr.Id);
        builder.Property(sr => sr.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        // Registry properties
        builder.Property(sr => sr.Code)
            .HasColumnName("code")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(sr => sr.Description)
            .HasColumnName("description")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(sr => sr.Type)
            .HasColumnName("type")
            .HasMaxLength(100)
            .IsRequired();

        // Status
        builder.Property(sr => sr.Active)
            .HasColumnName("active")
            .HasDefaultValue(1);

        // Timestamps
        builder.Property(sr => sr.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(sr => sr.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(sr => sr.CreatedBy)
            .HasColumnName("created_by")
            .HasMaxLength(255);

        // Indexes
        builder.HasIndex(sr => sr.Code)
            .HasDatabaseName("ix_standard_registry_code")
            .IsUnique();

        builder.HasIndex(sr => sr.Type)
            .HasDatabaseName("ix_standard_registry_type");

        builder.HasIndex(sr => sr.Active)
            .HasDatabaseName("ix_standard_registry_active")
            .HasFilter("active = 1");

        // Composite index for type/code lookups
        builder.HasIndex(sr => new { sr.Type, sr.Code })
            .HasDatabaseName("ix_standard_registry_type_code");
    }
}