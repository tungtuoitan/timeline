using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Request DTO for deleting multiple workspace items (ws schema - uses entity type+id)
    /// </summary>
    public class DeleteItemsRequest
    {
        [Required]
        [MinLength(1, ErrorMessage = "At least one item is required")]
        [MaxLength(500, ErrorMessage = "Maximum 500 items can be deleted at once")]
        public List<ItemIdentifier> Items { get; set; } = new();

        [JsonPropertyName("isHardDelete")]
        public bool IsHardDelete { get; set; } = false;

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
