using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Data.Configurations
{
    public class KKnowledgeConfiguration : IEntityTypeConfiguration<KKnowledge>
    {
        public void Configure(EntityTypeBuilder<KKnowledge> builder)
        {
            builder.ToTable("knowledge", "k");

            builder.HasKey(w => w.Id);
            builder.Property(w => w.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            builder.Property(w => w.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            builder.Property(w => w.Name)
                .HasColumnName("name")
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(w => w.Description)
                .HasColumnName("description")
                .HasMaxLength(1000);

            builder.Property(w => w.StatusCode)
                .HasColumnName("status_code")
                .HasMaxLength(50);

            builder.Property(w => w.ImageBase64)
                .HasColumnName("image_base64");

            builder.Property(w => w.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(w => w.UpdatedAt)
                .HasColumnName("updated_at");

            builder.Property(w => w.DeletedAt)
                .HasColumnName("deleted_at");

            builder.HasIndex(w => w.UserId)
                .HasDatabaseName("IX_k_knowledge_user")
                .HasFilter("[deleted_at] IS NULL");

            builder.HasOne(w => w.User)
                .WithMany(u => u.KKnowledges)
                .HasForeignKey(w => w.UserId)
                .HasConstraintName("FK_k_knowledge_users_user_id")
                .OnDelete(DeleteBehavior.Restrict);

            // Note: KKnowledge ↔ KNodeEntity relationship is configured in KNodeConfiguration
        }
    }
}
