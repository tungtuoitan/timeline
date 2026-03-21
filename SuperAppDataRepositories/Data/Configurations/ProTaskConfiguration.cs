using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Data.Configurations
{
    /// <summary>
    /// EF Core configuration for ProTask entity
    /// Maps to pro.task table
    /// </summary>
    public class ProTaskConfiguration : IEntityTypeConfiguration<ProTask>
    {
        public void Configure(EntityTypeBuilder<ProTask> builder)
        {
            builder.ToTable("task", "pro");

            builder.HasKey(t => t.Id);

            builder.Property(t => t.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            builder.Property(t => t.ProjectId)
                .HasColumnName("project_id")
                .IsRequired();

            builder.Property(t => t.ParentTaskId)
                .HasColumnName("parent_task_id");

            builder.Property(t => t.Type)
                .HasColumnName("type")
                .HasMaxLength(20)
                .HasDefaultValue("task")
                .IsRequired();

            builder.Property(t => t.TaskType)
                .HasColumnName("task_type")
                .HasMaxLength(50)
                .HasDefaultValue("personal")
                .IsRequired();

            builder.Property(t => t.Title)
                .HasColumnName("title")
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(t => t.Note)
                .HasColumnName("note");

            builder.Property(t => t.Status)
                .HasColumnName("status")
                .HasMaxLength(20)
                .HasDefaultValue("open")
                .IsRequired();

            builder.Property(t => t.Priority)
                .HasColumnName("priority")
                .HasMaxLength(20)
                .HasDefaultValue("low")
                .IsRequired();

            builder.Property(t => t.StartDate)
                .HasColumnName("start_date");

            builder.Property(t => t.EndDate)
            .HasColumnName("end_date");

            builder.Property(t => t.OrderIndex)
                .HasColumnName("order_index")
                .HasDefaultValue(0)
                .IsRequired();

            builder.Property(t => t.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("SYSDATETIME()");

            builder.Property(t => t.UpdatedAt)
                .HasColumnName("updated_at")
                .HasDefaultValueSql("SYSDATETIME()");

            builder.Property(t => t.DeletedAt)
                .HasColumnName("deleted_at");

            builder.Property(t => t.ChecklistJson)
                .HasColumnName("checklist_json");

            builder.Property(t => t.ProcessJson)
                .HasColumnName("process_json");

            // Index for common queries
            builder.HasIndex(t => t.ProjectId);
            builder.HasIndex(t => t.ParentTaskId);
        }
    }
}
