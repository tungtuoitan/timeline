using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Data.Configurations
{
    /// <summary>
    /// EF Core configuration for KNodeEntity → k.node table
    /// Self-contained node: name/description/color/icon stored directly.
    /// </summary>
    public class KNodeConfiguration : IEntityTypeConfiguration<KNodeEntity>
    {
        public void Configure(EntityTypeBuilder<KNodeEntity> builder)
        {
            builder.ToTable("node", "k");

            builder.HasKey(n => n.Id);
            builder.Property(n => n.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            builder.Property(n => n.KnowledgeId)
                .HasColumnName("knowledge_id")
                .IsRequired();

            builder.Property(n => n.ParentId)
                .HasColumnName("parent_id")
                .IsRequired(false);

            // Node data
            builder.Property(n => n.Name)
                .HasColumnName("name")
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(n => n.Description)
                .HasColumnName("description")
                .HasColumnType("nvarchar(max)")
                .IsRequired(false);

            builder.Property(n => n.Color)
                .HasColumnName("color")
                .HasMaxLength(7)
                .HasDefaultValue("#F59E0B")
                .IsRequired(false);

            builder.Property(n => n.Icon)
                .HasColumnName("icon")
                .HasMaxLength(50)
                .HasDefaultValue("📁")
                .IsRequired(false);

            // Type code — "draft" (default) | "shortcut"
            builder.Property(n => n.TypeCode)
                .HasColumnName("type_code")
                .HasMaxLength(50)
                .HasDefaultValue("draft")
                .IsRequired();

            // Shortcut columns
            builder.Property(n => n.RefTargetId)
                .HasColumnName("ref_target_id")
                .IsRequired(false);

            builder.Property(n => n.RefTargetKnowledgeId)
                .HasColumnName("ref_target_knowledge_id")
                .IsRequired(false);

            // Materialized path
            builder.Property(n => n.PathIds)
                .HasColumnName("PathIds")
                .HasMaxLength(1000)
                .HasDefaultValue("/");

            builder.Property(n => n.PathDepth)
                .HasColumnName("PathDepth")
                .HasDefaultValue(0);

            // Timestamps
            builder.Property(n => n.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(n => n.UpdatedAt)
                .HasColumnName("updated_at");

            builder.Property(n => n.DeletedAt)
                .HasColumnName("deleted_at");

            // Indexes
            builder.HasIndex(n => new { n.KnowledgeId, n.DeletedAt })
                .HasDatabaseName("IX_k_node_knowledge")
                .HasFilter("[deleted_at] IS NULL");

            builder.HasIndex(n => n.ParentId)
                .HasDatabaseName("IX_k_node_parent")
                .HasFilter("[deleted_at] IS NULL");

            builder.HasIndex(n => new { n.PathIds, n.PathDepth })
                .HasDatabaseName("IX_k_node_path")
                .HasFilter("[deleted_at] IS NULL");

            // Relationships
            builder.HasOne(n => n.Knowledge)
                .WithMany(k => k.Nodes)
                .HasForeignKey(n => n.KnowledgeId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(n => n.Parent)
                .WithMany()
                .HasForeignKey(n => n.ParentId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);
        }
    }
}
