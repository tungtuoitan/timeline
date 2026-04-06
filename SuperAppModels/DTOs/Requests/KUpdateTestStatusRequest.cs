using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SuperAppModels.DTOs.Requests
{
    public class KUpdateTestStatusRequest
    {
        [Required]
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
    }
}
