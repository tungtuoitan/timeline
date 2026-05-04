using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SuperAppModels.DTOs.Requests
{
    public class MoveCrossRequest
    {
        [Required]
        [MinLength(1, ErrorMessage = "At least one item ID is required")]
        [JsonPropertyName("itemIds")]
        public List<int> ItemIds { get; set; } = new();

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "TargetWorkspaceId must be positive")]
        [JsonPropertyName("targetWorkspaceId")]
        public int TargetWorkspaceId { get; set; }

        [JsonPropertyName("parentId")]
        public int? ParentId { get; set; }
    }
}
