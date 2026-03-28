using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Data.Configurations
{
    public class KTestNodeConfiguration : IEntityTypeConfiguration<KTestNodeEntity>
    {
        public void Configure(EntityTypeBuilder<KTestNodeEntity> builder)
        {
            builder.ToTable("test_node", "k");

            builder.HasKey(t => t.Id);
            builder.Property(t => t.Id).HasColumnName("id").ValueGeneratedOnAdd();

            builder.Property(t => t.TestId).HasColumnName("test_id").IsRequired();
            builder.Property(t => t.NodeId).HasColumnName("node_id").IsRequired();

            builder.Property(t => t.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true)
                .IsRequired();

            builder.HasIndex(t => new { t.TestId, t.NodeId })
                .HasDatabaseName("IX_k_test_node_test_node")
                .IsUnique();

            builder.HasIndex(t => t.NodeId)
                .HasDatabaseName("IX_k_test_node_node");

            builder.HasOne(t => t.Test)
                .WithMany(t => t.TestNodes)
                .HasForeignKey(t => t.TestId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
