using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Data.Configurations
{
    public class KNodeStatusHistoryConfiguration : IEntityTypeConfiguration<KNodeStatusHistoryEntity>
    {
        public void Configure(EntityTypeBuilder<KNodeStatusHistoryEntity> builder)
        {
            builder.ToTable("node_status_history", "k");
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Id).HasColumnName("id").ValueGeneratedOnAdd();

            builder.Property(p => p.NodeId).HasColumnName("node_id").IsRequired();
            builder.Property(p => p.StatusCode).HasColumnName("status_code").HasMaxLength(32).IsRequired();
            builder.Property(p => p.ChangedAt).HasColumnName("changed_at").HasDefaultValueSql("GETUTCDATE()");
            builder.Property(p => p.UserId).HasColumnName("user_id").IsRequired(false);

            builder.HasIndex(p => new { p.NodeId, p.ChangedAt })
                .HasDatabaseName("IX_k_node_status_history_node_changed");
        }
    }
}
