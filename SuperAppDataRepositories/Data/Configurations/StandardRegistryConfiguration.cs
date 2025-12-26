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
        builder.Property(sr => sr.Code)
            .HasColumnName("code")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(sr => sr.Description)
            .HasColumnName("description")
            .HasMaxLength(500);

        builder.Property(sr => sr.Type)
            .HasColumnName("type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(sr => sr.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(sr => sr.Json_detail)
            .HasColumnName("json_detail")
            .HasColumnType("NVARCHAR(MAX)");

        builder.Property(sr => sr.CreatedBy)
            .HasColumnName("created_by")
            .HasMaxLength(255);

        builder.Property(sr => sr.CreatedDate)
            .HasColumnName("created_date");

        builder.Property(sr => sr.LastModifiedBy)
            .HasColumnName("last_modified_by")
            .HasMaxLength(255);

        builder.Property(sr => sr.LastModifiedDate)
            .HasColumnName("last_modified_date");

        // Indexes
        builder.HasIndex(sr => new { sr.Type, sr.Code })
            .HasDatabaseName("IX_standard_registries_type_code");

        builder.HasIndex(sr => sr.Type)
            .HasDatabaseName("IX_standard_registries_type");

        // Note: No unique constraint needed - status_code fields don't have FK references
    }
}