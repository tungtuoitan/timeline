using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Data.Configurations
{
    public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
    {
        public void Configure(EntityTypeBuilder<UserProfile> builder)
        {
            // Table mapping - urm schema (User Resource Management)
            builder.ToTable("user_profiles", "urm");

            // Primary key
            builder.HasKey(up => up.Id);
            builder.Property(up => up.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            // Properties - EXACTLY match urm.user_profiles schema
            builder.Property(up => up.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            builder.Property(up => up.FirstName)
                .HasColumnName("first_name")
                .HasMaxLength(100);

            builder.Property(up => up.LastName)
                .HasColumnName("last_name")
                .HasMaxLength(100);

            builder.Property(up => up.AvatarUrl)
                .HasColumnName("avatar_url")
                .HasMaxLength(500);

            builder.Property(up => up.Bio)
                .HasColumnName("bio")
                .HasMaxLength(1000);

            builder.Property(up => up.DateOfBirth)
                .HasColumnName("date_of_birth")
                .HasColumnType("date");

            builder.Property(up => up.Gender)
                .HasColumnName("gender")
                .HasMaxLength(10);

            builder.Property(up => up.Country)
                .HasColumnName("country")
                .HasMaxLength(100);

            builder.Property(up => up.City)
                .HasColumnName("city")
                .HasMaxLength(100);

            builder.Property(up => up.Timezone)
                .HasColumnName("timezone")
                .HasMaxLength(64)
                .HasDefaultValue(SuperAppModels.Time.TimeZones.DefaultId);

            builder.Property(up => up.Language)
                .HasColumnName("language")
                .HasMaxLength(10)
                .HasDefaultValue("en");

            builder.Property(up => up.Filters)
                .HasColumnName("filters")
                .HasColumnType("NVARCHAR(MAX)");

            builder.Property(up => up.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(up => up.UpdatedAt)
                .HasColumnName("updated_at");

            builder.Property(up => up.DeletedAt)
                .HasColumnName("deleted_at");

            // Unique constraint on user_id
            builder.HasIndex(up => up.UserId)
                .HasDatabaseName("UQ_user_profiles_user_id")
                .IsUnique();

            // Foreign key relationship
            builder.HasOne(up => up.User)
                .WithOne()
                .HasForeignKey<UserProfile>(up => up.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
