using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Request DTO for deleting multiple workspace items (folders, notes, files)
    /// </summary>
    public class DeleteItemsRequest
    {
        /// <summary>
        /// List of items to delete (supports multi-select: folders, notes, files)
        /// </summary>
        [Required(ErrorMessage = "Items list is required")]
        [MinLength(1, ErrorMessage = "At least one item is required")]
        [MaxLength(500, ErrorMessage = "Maximum 500 items can be deleted at once")]
        public List<ItemIdentifier> Items { get; set; } = new();

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
            [JsonPropertyName("type")]
            public byte Type { get; set; }

            /// <summary>
            /// Item ID
            /// </summary>
            [Required(ErrorMessage = "Item ID is required")]
            [Range(1, int.MaxValue, ErrorMessage = "Item ID must be positive")]
            [JsonPropertyName("id")]
            public int Id { get; set; }
        }
    }
}
