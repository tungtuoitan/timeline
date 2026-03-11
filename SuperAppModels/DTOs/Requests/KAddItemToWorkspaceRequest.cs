using System.ComponentModel.DataAnnotations;

namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Request to add an item (tag or note) to a kworkspace
    /// </summary>
    public class KAddItemToWorkspaceRequest
    {
        /// <summary> 
        /// Parent tag ID where the item will be placed (null for root items)
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "Parent tag ID must be positive")]
        public int? ParentTagId { get; set; }  // Nullable for root items

        /// <summary>
        /// Type of child entity (e.g., 'tag', 'note')
        /// </summary>
        [Required(ErrorMessage = "Child type is required")]
        [StringLength(50, ErrorMessage = "Child type cannot exceed 50 characters")] 
        public string ChildType { get; set; } = string.Empty;

        /// <summary>
        /// ID of the child entity (optional if creating new tag)
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "Child ID must be positive")]
        public int? ChildId { get; set; }

        /// <summary>
        /// Tag name (required when ChildType='tag' and ChildId is not provided - will auto-create tag)
        /// </summary>
        [StringLength(255, ErrorMessage = "Tag name cannot exceed 255 characters")]
        public string? TagName { get; set; }

        /// <summary>
        /// Optional relationship type (e.g., 'contains', 'references')
        /// </summary>
        [StringLength(100, ErrorMessage = "Relationship type cannot exceed 100 characters")]
        public string? RelationshipType { get; set; }

        /// <summary>
        /// Custom label for this relationship
        /// </summary>
        [StringLength(500, ErrorMessage = "Label cannot exceed 500 characters")]
        public string? Label { get; set; }

        /// <summary>
        /// Additional notes about this relationship
        /// </summary>
        [StringLength(2000, ErrorMessage = "Notes cannot exceed 2000 characters")]
        public string? Notes { get; set; }

        /// <summary>
        /// Sort order for display (default: 0)
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "Sort order cannot be negative")]
        public int SortOrder { get; set; } = 0;

        /// <summary>
        /// Optional color for visual distinction
        /// </summary>
        [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Color must be in hex format (#RRGGBB)")]
        public string? Color { get; set; }

        /// <summary>
        /// Optional icon identifier
        /// </summary>
        [StringLength(50, ErrorMessage = "Icon cannot exceed 50 characters")]
        public string? Icon { get; set; }

        /// <summary>
        /// Indicates if this workspace created/owns the item (true) or if it's shared from another workspace (false)
        /// Default: true (creating new item)
        /// </summary>
        public bool IsOriginal { get; set; } = true;
    }
}
