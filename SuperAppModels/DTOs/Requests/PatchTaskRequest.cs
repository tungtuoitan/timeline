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

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("priority")]
        public string? Priority { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("taskType")]
        public string? TaskType { get; set; }

        [JsonPropertyName("startDate")]
        public DateTime? StartDate { get; set; }

        [JsonPropertyName("endDate")]
        public DateTime? EndDate { get; set; }

        [JsonPropertyName("orderIndex")]
        public int? OrderIndex { get; set; }

        [JsonPropertyName("isMilestone")]
        public bool? IsMilestone { get; set; }

        /// <summary>Move task to another project (must be owned by the caller)</summary>
        [JsonPropertyName("projectId")]
        public int? ProjectId { get; set; }

        /// <summary>Set parent task (must be owned by the caller)</summary>
        [JsonPropertyName("parentTaskId")]
        public int? ParentTaskId { get; set; }

        /// <summary>
        /// Fields to set to null — null in the body means "unchanged", so clearing needs this.
        /// Allowed: note, checklistJson, processJson, customTabsJson, startDate, endDate, parentTaskId.
        /// </summary>
        [JsonPropertyName("clearFields")]
        public List<string>? ClearFields { get; set; }

        public static readonly HashSet<string> ClearableFields = new(StringComparer.OrdinalIgnoreCase)
        {
            "note", "checklistJson", "processJson", "customTabsJson", "startDate", "endDate", "parentTaskId"
        };
    }
}
