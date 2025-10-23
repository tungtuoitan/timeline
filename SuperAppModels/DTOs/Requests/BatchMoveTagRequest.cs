using System.ComponentModel.DataAnnotations;

namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Request model for batch moving multiple tags to a new parent/position
    /// </summary>
    public class BatchMoveTagRequest
    {
        /// <summary>
        /// Array of tag IDs to move
        /// </summary>
        [Required(ErrorMessage = "TagIds is required")]
        [MinLength(1, ErrorMessage = "At least one tag ID must be provided")]
        public int[] TagIds { get; set; } = Array.Empty<int>();

        /// <summary>
        /// New parent tag ID (null for root level)
        /// </summary>
        public int? NewParentId { get; set; }

        /// <summary>
        /// Starting index position for the first item (subsequent items will be placed sequentially)
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "StartIndex must be non-negative")]
        public int StartIndex { get; set; } = 0;
    }
}
