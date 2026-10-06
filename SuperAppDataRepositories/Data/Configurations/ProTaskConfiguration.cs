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

            builder.Property(t => t.Description)
                .HasColumnName("description");

            // TODO(0109): remove legacy "note" alias after old FE is gone
            builder.Ignore(t => t.LegacyNote);

            builder.Property(t => t.Status)
                .HasColumnName("status_code")
                .HasMaxLength(20)
                .HasDefaultValue("open")
                .IsRequired();

            builder.Property(t => t.Priority)
                .HasColumnName("priority")
                .HasMaxLength(20)
                .HasDefaultValue("low")
                .IsRequired();

            builder.Property(t => t.StartDate)
                .HasColumnName("start_date")
                .HasColumnType("date");

            builder.Property(t => t.EndDate)
                .HasColumnName("end_date")
                .HasColumnType("date");

            builder.Property(t => t.OrderIndex)
                .HasColumnName("order_index")
                .HasDefaultValue(0)
                .IsRequired();

            builder.Property(t => t.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.Property(t => t.UpdatedAt)
                .HasColumnName("updated_at")
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.Property(t => t.DeletedAt)
                .HasColumnName("deleted_at");

            builder.Property(t => t.ChecklistJson)
                .HasColumnName("checklist_json");

            builder.Property(t => t.ProcessJson)
                .HasColumnName("process_json");

            builder.Property(t => t.CustomTabsJson)
                .HasColumnName("custom_tabs_json");

            builder.Property(t => t.FolderWorkspaceItemId)
                .HasColumnName("folder_workspace_item_id");

            builder.Property(t => t.IsMilestone)
                .HasColumnName("is_milestone")
                .HasDefaultValue(false)
                .IsRequired();

            builder.Property(t => t.IsSensitive)
                .HasColumnName("is_sensitive")
                .HasDefaultValue(false)
                .IsRequired();

            // Index for common queries
            builder.HasIndex(t => t.ProjectId);
            builder.HasIndex(t => t.ParentTaskId);
        }
    }
}
