using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Request DTO for moving multiple workspace items (ws schema - uses entity type+id)
    /// </summary>
    public class MoveItemsRequest
    {
        [Required]
        [MinLength(1, ErrorMessage = "At least one item is required")]
        [MaxLength(500, ErrorMessage = "Maximum 500 items can be moved at once")]
        public List<ItemIdentifier> Items { get; set; } = new();

        [Range(1, int.MaxValue, ErrorMessage = "Target parent ID must be positive")]
        public int? TargetParentId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Target workspace ID must be positive")]
        public int? TargetWorkspaceId { get; set; }

        public class ItemIdentifier
        {
            [Required]
            [Range(2, 4)]
            [JsonPropertyName("type")]
            public byte Type { get; set; }

            [Required]
            [Range(1, int.MaxValue)]
            [JsonPropertyName("id")]
            public int Id { get; set; }
        }
    }
}
