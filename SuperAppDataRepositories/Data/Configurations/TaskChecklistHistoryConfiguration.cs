using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Data.Configurations
{
    /// <summary>
    /// EF Core configuration for TaskChecklistHistory entity
    /// Maps to pro.task_checklist_history table
    /// </summary>
    public class TaskChecklistHistoryConfiguration : IEntityTypeConfiguration<TaskChecklistHistory>
    {
        public void Configure(EntityTypeBuilder<TaskChecklistHistory> builder)
        {
            builder.ToTable("task_checklist_history", "pro");

            builder.HasKey(h => h.Id);

            builder.Property(h => h.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            builder.Property(h => h.TaskId)
                .HasColumnName("task_id")
                .IsRequired();

            builder.Property(h => h.Date)
                .HasColumnName("date")
                .HasColumnType("date")
                .IsRequired();

            builder.Property(h => h.ChecklistSnapshot)
                .HasColumnName("checklist_snapshot")
                .IsRequired();

            builder.Property(h => h.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("SYSUTCDATETIME()");

            // Indexes
            builder.HasIndex(h => h.TaskId);
            builder.HasIndex(h => new { h.TaskId, h.Date }).IsUnique();
        }
    }
}
