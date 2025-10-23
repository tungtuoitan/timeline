using System.ComponentModel.DataAnnotations;

namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Request to update workspace item metadata (label, notes, color, icon, sort order)
    /// </summary>
    public class UpdateWorkspaceItemRequest
    {
        /// <summary>
        /// Custom label for this item in the workspace (optional)
        /// </summary>
        [StringLength(200, ErrorMessage = "Label cannot exceed 200 characters")]
        public string? Label { get; set; }

        /// <summary>
        /// Notes about this item in the workspace context (optional)
        /// </summary>
        [StringLength(2000, ErrorMessage = "Notes cannot exceed 2000 characters")]
        public string? Notes { get; set; }

        /// <summary>
        /// Custom color for this item in the workspace (hex format: #RRGGBB)
        /// </summary>
        [StringLength(7, ErrorMessage = "Color must be in hex format (#RRGGBB)")]
        [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Color must be in hex format (#RRGGBB)")]
        public string? Color { get; set; }

        /// <summary>
        /// Custom icon for this item in the workspace (emoji or icon name)
        /// </summary>
        [StringLength(50, ErrorMessage = "Icon cannot exceed 50 characters")]
        public string? Icon { get; set; }

        /// <summary>
        /// Sort order within parent (optional, for reordering items)
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "Sort order must be non-negative")]
        public int? SortOrder { get; set; }
    }
}
