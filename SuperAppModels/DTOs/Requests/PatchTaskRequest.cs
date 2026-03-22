using System.Text.Json.Serialization;

namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Partial update request for a task.
    /// Only non-null fields will be updated; null fields are left unchanged in DB.
    /// </summary>
    public class PatchTaskRequest
    {
        /// <summary>Description (HTML rich text)</summary>
        [JsonPropertyName("note")]
        public string? Note { get; set; }

        /// <summary>Checklist JSON (serialized ChecklistJSON)</summary>
        [JsonPropertyName("checklistJson")]
        public string? ChecklistJson { get; set; }

        /// <summary>Process/step JSON (same structure as ChecklistJSON)</summary>
        [JsonPropertyName("processJson")]
        public string? ProcessJson { get; set; }

        /// <summary>Custom tabs JSON (serialized CustomTabsJSON)</summary>
        [JsonPropertyName("customTabsJson")]
        public string? CustomTabsJson { get; set; }

        /// <summary>Task status (e.g. "open", "in_progress", "completed")</summary>
        [JsonPropertyName("status")]
        public string? Status { get; set; }
    }
}
