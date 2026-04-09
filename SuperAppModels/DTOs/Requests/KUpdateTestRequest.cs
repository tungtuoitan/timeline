using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SuperAppModels.DTOs.Requests
{
    /// <summary>Update test title and/or node assignment</summary>
    public class KUpdateTestRequest
    {
        [StringLength(500, MinimumLength = 1)]
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        /// <summary>Move test to a different entity node. Use -1 to unlink from node.</summary>
        [JsonPropertyName("nodeId")]
        public int? NodeId { get; set; }
    }
}
