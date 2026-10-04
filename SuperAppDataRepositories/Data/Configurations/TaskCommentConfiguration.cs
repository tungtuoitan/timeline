using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Data.Configurations
{
    /// <summary>
    /// EF Core configuration for TaskComment entity
    /// Maps to pro.task_comment table
    /// </summary>
    public class TaskCommentConfiguration : IEntityTypeConfiguration<TaskComment>
    {
        public void Configure(EntityTypeBuilder<TaskComment> builder)
        {
            builder.ToTable("task_comment", "pro");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            builder.Property(c => c.TaskId)
                .HasColumnName("task_id")
                .IsRequired();

            builder.Property(c => c.ParentCommentId)
                .HasColumnName("parent_comment_id");

            builder.Property(c => c.Content)
                .HasColumnName("content")
                .IsRequired();

            builder.Property(c => c.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            builder.Property(c => c.Type)
                .HasColumnName("type")
                .HasMaxLength(20)
                .HasDefaultValue(TaskCommentTypes.Comment)
                .IsRequired();

            builder.Property(c => c.OccurredAt)
                .HasColumnName("occurred_at");

            builder.Property(c => c.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.Property(c => c.UpdatedAt)
                .HasColumnName("updated_at")
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.Property(c => c.DeletedAt)
                .HasColumnName("deleted_at");

            builder.HasIndex(c => c.TaskId);
            builder.HasIndex(c => new { c.TaskId, c.Type });
            builder.HasIndex(c => c.ParentCommentId);
        }
    }
}
