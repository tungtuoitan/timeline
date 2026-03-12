using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Request DTO for moving workspace items (kws) by workspace_item IDs
    /// </summary>
    public class KMoveItemsRequest
    {
        /// <summary>List of workspace_item IDs to move</summary>
        [Required]
        [MinLength(1, ErrorMessage = "At least one item is required")]
        [MaxLength(500, ErrorMessage = "Maximum 500 items can be moved at once")]
        [JsonPropertyName("itemIds")]
        public List<int> ItemIds { get; set; } = new();

        /// <summary>Target parent workspace_item ID (null = move to root)</summary>
        [JsonPropertyName("targetParentId")]
        public int? TargetParentId { get; set; }

        /// <summary>Target workspace ID (null = same workspace)</summary>
        [JsonPropertyName("targetWorkspaceId")]
        public int? TargetWorkspaceId { get; set; }
    }
}
