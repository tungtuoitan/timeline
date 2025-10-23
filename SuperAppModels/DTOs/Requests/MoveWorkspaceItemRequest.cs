using System.ComponentModel.DataAnnotations;

namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Request to move a workspace item to a different parent tag
    /// </summary>
    public class MoveWorkspaceItemRequest
    {
        /// <summary>
        /// New parent tag ID (null for root level)
        /// </summary>
        public int? NewParentTagId { get; set; }

        /// <summary>
        /// Optional: New sort order after moving (defaults to appending to end)
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "Sort order must be non-negative")]
        public int? SortOrder { get; set; }
    }
}
