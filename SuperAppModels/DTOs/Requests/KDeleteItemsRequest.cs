using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Request DTO for deleting workspace items (kws) by workspace_item IDs
    /// </summary>
    public class KDeleteItemsRequest
    {
        /// <summary>List of workspace_item IDs to delete (with descendants)</summary>
        [Required]
        [MinLength(1, ErrorMessage = "At least one item is required")]
        [MaxLength(500, ErrorMessage = "Maximum 500 items can be deleted at once")]
        [JsonPropertyName("itemIds")]
        public List<int> ItemIds { get; set; } = new();
    }
}
