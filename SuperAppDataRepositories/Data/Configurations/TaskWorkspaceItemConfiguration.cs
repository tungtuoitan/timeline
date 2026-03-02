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
            builder.ToTable("TaskWorkspaceItem", "pro");

            builder.HasKey(t => t.Id);

            builder.Property(t => t.Id)
                .HasColumnName("Id")
                .ValueGeneratedOnAdd();

            builder.Property(t => t.TaskId)
                .HasColumnName("TaskId")
                .IsRequired();

            builder.Property(t => t.WorkspaceItemId)
                .HasColumnName("WorkspaceItemId")
                .IsRequired();

            builder.Property(t => t.ItemType)
                .HasColumnName("ItemType")
                .IsRequired();

            builder.HasIndex(t => t.TaskId);
            builder.HasIndex(t => t.WorkspaceItemId);
        }
    }
}
