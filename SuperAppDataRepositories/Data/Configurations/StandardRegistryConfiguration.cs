using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Data.Configurations;

public class StandardRegistryConfiguration : IEntityTypeConfiguration<StandardRegistry>
{
    public void Configure(EntityTypeBuilder<StandardRegistry> builder)
    {
        // Table mapping - dbo schema
        builder.ToTable("standard_registries", "dbo");

        // Primary key
        builder.HasKey(sr => sr.Id);
        builder.Property(sr => sr.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        // Properties - EXACTLY match dbo.standard_registries schema
        builder.Property(sr => sr.TypeCode)
            .HasColumnName("type_code")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(sr => sr.Description)
            .HasColumnName("description")
            .HasMaxLength(500);

        builder.Property(sr => sr.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        // Timestamps
        builder.Property(sr => sr.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(sr => sr.UpdatedAt)
            .HasColumnName("updated_at");

        // Indexes
        builder.HasIndex(sr => sr.TypeCode)
            .HasDatabaseName("UQ_standard_registries_type_code")
            .IsUnique();
    }
}