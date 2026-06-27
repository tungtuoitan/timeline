using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Data.Configurations
{
    public class KQuestionConfiguration : IEntityTypeConfiguration<KQuestionEntity>
    {
        public void Configure(EntityTypeBuilder<KQuestionEntity> builder)
        {
            builder.ToTable("question", "k");

            builder.HasKey(q => q.Id);
            builder.Property(q => q.Id).HasColumnName("id").ValueGeneratedOnAdd();

            builder.Property(q => q.NodeId).HasColumnName("node_id").IsRequired(false);
            builder.Property(q => q.Name).HasColumnName("name").HasMaxLength(500).IsRequired();
            builder.Property(q => q.Description).HasColumnName("description").HasColumnType("nvarchar(max)").IsRequired(false);
            builder.Property(q => q.Context).HasColumnName("context").HasColumnType("nvarchar(max)").IsRequired(false);
            builder.Property(q => q.Directives).HasColumnName("directives").HasColumnType("nvarchar(max)").IsRequired(false);
            builder.Property(q => q.StatusCode).HasColumnName("status_code").HasMaxLength(50).HasDefaultValue("learning");
            builder.Property(q => q.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);

            // SRS columns
            builder.Property(q => q.SrsInterval).HasColumnName("srs_interval").HasDefaultValue(0);
            builder.Property(q => q.SrsEaseFactor).HasColumnName("srs_ease_factor").HasDefaultValue(2.5);
            builder.Property(q => q.SrsRepetitions).HasColumnName("srs_repetitions").HasDefaultValue(0);
            builder.Property(q => q.SrsNextReviewAt).HasColumnName("srs_next_review_at").IsRequired(false);

            builder.Property(q => q.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("GETUTCDATE()");
            builder.Property(q => q.UpdatedAt).HasColumnName("updated_at").IsRequired(false);
            builder.Property(q => q.DeletedAt).HasColumnName("deleted_at").IsRequired(false);

            builder.HasIndex(q => q.NodeId).HasDatabaseName("IX_k_question_node");

            builder.HasOne(q => q.Node)
                .WithMany()
                .HasForeignKey(q => q.NodeId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
