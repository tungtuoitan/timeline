using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Data.Configurations
{
    public class KPointHistoryConfiguration : IEntityTypeConfiguration<KPointHistoryEntity>
    {
        public void Configure(EntityTypeBuilder<KPointHistoryEntity> builder)
        {
            builder.ToTable("point_history", "k");
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Id).HasColumnName("id").ValueGeneratedOnAdd();

            builder.Property(p => p.TestId).HasColumnName("test_id").IsRequired();
            builder.Property(p => p.UserId).HasColumnName("user_id").IsRequired();
            builder.Property(p => p.NodeId).HasColumnName("node_id").IsRequired(false);
            builder.Property(p => p.AnswerText).HasColumnName("answer_text").HasColumnType("nvarchar(max)").IsRequired(false);
            builder.Property(p => p.Point).HasColumnName("point").HasDefaultValue(0);
            builder.Property(p => p.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("GETUTCDATE()");

            builder.HasIndex(p => new { p.TestId, p.UserId })
                .HasDatabaseName("IX_k_point_history_test_user");

            builder.HasIndex(p => new { p.UserId, p.NodeId })
                .HasDatabaseName("IX_k_point_history_user_node");

            builder.HasOne(p => p.Test)
                .WithMany(t => t.PointHistory)
                .HasForeignKey(p => p.TestId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
