using System.ComponentModel.DataAnnotations;

namespace SuperAppModels.Models
{
    /// <summary>
    /// UNIFIED table managing ALL items in workspace (tags AND notes)
    /// Primary table: workspace_items
    /// Handles both tag-to-tag and tag-to-note relationships
    /// </summary>
    public class WorkspaceItem : ITimestampEntity
    {
        // Primary Key - item_id
        public long ItemId { get; set; }

        // Workspace context
        public int WorkspaceId { get; set; }

        // Parent (always a tag)
        public int ParentTagId { get; set; }

        // Child (can be tag or any entity)
        public string ChildType { get; set; } = string.Empty; // 'tag', 'note', etc.
        public int ChildId { get; set; }

        // Relationship metadata
        public string? RelationshipType { get; set; }
        public string? Label { get; set; }
        public string? Notes { get; set; }

        // Materialized path for tree queries
        public string? ItemPath { get; set; } // e.g., '/1/5/12/'
        public int Depth { get; set; } = 0; // 0 = root level

        // Display properties
        public int SortOrder { get; set; } = 0;
        public string? Color { get; set; }
        public string? Icon { get; set; }

        // Audit
        public int AddedBy { get; set; }

        // Timestamps (ITimestampEntity)
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        // Navigation properties
        public Workspace Workspace { get; set; } = null!;
        public Tag ParentTag { get; set; } = null!;
        public User AddedByUser { get; set; } = null!;

        // Dynamic navigation properties based on ChildType
        public Tag? ChildTag { get; set; }
        public Note? ChildNote { get; set; }

        public WorkspaceItem()
        {
            CreatedAt = DateTime.UtcNow;
        }

        public WorkspaceItem(int workspaceId, int parentTagId, string childType, int childId, int addedBy) : this()
        {
            WorkspaceId = workspaceId;
            ParentTagId = parentTagId;
            ChildType = childType ?? throw new ArgumentNullException(nameof(childType));
            ChildId = childId;
            AddedBy = addedBy;
        }

        /// <summary>
        /// Updates relationship metadata
        /// </summary>
        public void UpdateRelationship(string? relationshipType = null, string? label = null, string? notes = null)
        {
            if (relationshipType != null) RelationshipType = relationshipType;
            if (label != null) Label = label;
            if (notes != null) Notes = notes;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Updates display properties
        /// </summary>
        public void UpdateDisplay(int? sortOrder = null, string? color = null, string? icon = null)
        {
            if (sortOrder.HasValue) SortOrder = sortOrder.Value;
            if (color != null) Color = color;
            if (icon != null) Icon = icon;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Updates materialized path and depth for tree structure
        /// </summary>
        public void UpdatePath(string itemPath, int depth)
        {
            ItemPath = itemPath;
            Depth = depth;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Marks item as deleted (soft delete)
        /// </summary>
        public void SoftDelete()
        {
            DeletedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Checks if this item represents a tag-to-tag relationship
        /// </summary>
        public bool IsTagToTagRelationship => ChildType == "tag";

        /// <summary>
        /// Checks if this item represents a tag-to-note relationship
        /// </summary>
        public bool IsTagToNoteRelationship => ChildType == "note";
    }
}