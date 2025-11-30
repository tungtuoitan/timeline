using System.ComponentModel.DataAnnotations;

namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Request DTO for moving multiple workspace items (VSCode-style multi-select)
    /// </summary>
    public class MoveItemsRequest
    {
        /// <summary>
        /// List of items to move (supports multi-select: folders, notes, files)
        /// </summary>
        [Required(ErrorMessage = "Items list is required")]
        [MinLength(1, ErrorMessage = "At least one item is required")]
        [MaxLength(500, ErrorMessage = "Maximum 500 items can be moved at once")]
        public List<ItemIdentifier> Items { get; set; } = new();

        /// <summary>
        /// Target folder ID (null = move to root level)
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "Target folder ID must be positive")]
        public int? TargetFolderId { get; set; }

        /// <summary>
        /// Target workspace ID (null = same workspace, value = move to different workspace)
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "Target workspace ID must be positive")]
        public int? TargetWorkspaceId { get; set; }

        /// <summary>
        /// Item identifier (type + id)
        /// </summary>
        public class ItemIdentifier
        {
            /// <summary>
            /// Item type: 2=folder, 3=note, 4=file
            /// </summary>
            [Required(ErrorMessage = "Item type is required")]
            [Range(2, 4, ErrorMessage = "Item type must be 2 (folder), 3 (note), or 4 (file)")]
            public byte ItemType { get; set; }

            /// <summary>
            /// Item ID
            /// </summary>
            [Required(ErrorMessage = "Item ID is required")]
            [Range(1, int.MaxValue, ErrorMessage = "Item ID must be positive")]
            public int ItemId { get; set; }
        }
    }
}
