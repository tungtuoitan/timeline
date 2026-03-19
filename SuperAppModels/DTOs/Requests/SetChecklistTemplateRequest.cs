using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Request to save a checklist text as the default template for a taskType registry entry.
    /// Updates json_detail on the matching dbo.standard_registries row.
    /// </summary>
    public class SetChecklistTemplateRequest
    {
        /// <summary>The taskType registry code (e.g. "vanthiel-coding")</summary>
        [Required]
        [JsonPropertyName("taskTypeCode")]
        public string TaskTypeCode { get; set; } = string.Empty;

        /// <summary>
        /// The checklist template in plain text format:
        ///   # Group Name
        ///   - Item name
        ///   - Optional item (o)
        /// </summary>
        [Required]
        [JsonPropertyName("template")]
        public string Template { get; set; } = string.Empty;
    }
}
