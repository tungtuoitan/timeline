using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Data.Configurations
{
    /// <summary>
    /// EF Core configuration for TaskWorkspaceItem entity
    /// Maps to pro.TaskWorkspaceItem table
    /// </summary>
    public class TaskWorkspaceItemConfiguration : IEntityTypeConfiguration<TaskWorkspaceItem>
    {
        public void Configure(EntityTypeBuilder<TaskWorkspaceItem> builder)
        {
            builder.ToTable("task_workspace_item", "pro");

            builder.HasKey(t => t.Id);

            builder.Property(t => t.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            builder.Property(t => t.TaskId)
                .HasColumnName("task_id")
                .IsRequired();

            builder.Property(t => t.WorkspaceItemId)
                .HasColumnName("workspace_item_id")
                .IsRequired();

            builder.Property(t => t.ItemType)
                .HasColumnName("item_type")
                .IsRequired();

            builder.HasIndex(t => t.TaskId);
            builder.HasIndex(t => t.WorkspaceItemId);
        }
    }
}
