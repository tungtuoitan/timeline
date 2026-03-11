using System.ComponentModel.DataAnnotations;

namespace SuperAppModels.Models
{
    /// <summary>
    /// Custom relationship types for workspace-specific workflows
    /// Primary table: workspace_relationship_types
    /// </summary>
    public class KWorkspaceRelationshipType : ITimestampEntity
    {
        // Primary Key - relationship_type_id
        public int RelationshipTypeId { get; set; }

        // Workspace
        public int WorkspaceId { get; set; }

        // Type definition
        public string TypeName { get; set; } = string.Empty;
         public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }

        // Visual properties
        public string? Icon { get; set; }
        public string? Color { get; set; }
        public string LineStyle { get; set; } = "solid";
        public int LineWidth { get; set; } = 2;

        // Relationship behavior
        public bool IsBidirectional { get; set; } = false;
        public bool AllowsCycles { get; set; } = false;
        public int? MaxDepth { get; set; }

        // Validation rules
        public string? ValidationRules { get; set; } // JSON

        // Display order
        public int SortOrder { get; set; } = 0;

        // Timestamps (ITimestampEntity)
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        // Navigation properties
        public Workspace Workspace { get; set; } = null!;

        public KWorkspaceRelationshipType()
        {
            CreatedAt = DateTime.UtcNow;
        }

        public KWorkspaceRelationshipType(int workspaceId, string typeName, string displayName) : this()
        {
            WorkspaceId = workspaceId;
            TypeName = typeName ?? throw new ArgumentNullException(nameof(typeName));
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        }

        /// <summary>
        /// Updates relationship type configuration
        /// </summary>
        public void Update(string displayName, string? description = null, string? icon = null, string? color = null)
        {
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            Description = description;
            if (icon != null) Icon = icon;
            if (color != null) Color = color;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Updates visual properties
        /// </summary>
        public void UpdateVisualProperties(string lineStyle, int lineWidth, string? color = null)
        {
            LineStyle = lineStyle;
            LineWidth = lineWidth;
            if (color != null) Color = color;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Updates relationship behavior
        /// </summary>
        public void UpdateBehavior(bool isBidirectional, bool allowsCycles, int? maxDepth = null)
        {
            IsBidirectional = isBidirectional;
            AllowsCycles = allowsCycles;
            MaxDepth = maxDepth;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Marks relationship type as deleted (soft delete)
        /// </summary>
        public void SoftDelete()
        {
            DeletedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}