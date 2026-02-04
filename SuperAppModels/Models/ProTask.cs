using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SuperAppModels.Models
{
    /// <summary>
    /// Task entity for Personal Productivity App
    /// Maps to pro.task table
    /// Named ProTask to avoid conflict with System.Threading.Tasks.Task
    /// </summary>
    public class ProTask
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("projectId")]
        public int ProjectId { get; set; }

        [JsonPropertyName("parentTaskId")]
        public int? ParentTaskId { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = "task";

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("note")]
        public string? Note { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = "open";

        [JsonPropertyName("priority")]
        public string Priority { get; set; } = "low";

        [JsonPropertyName("startDate")]
        public DateTime? StartDate { get; set; }

        [JsonPropertyName("endDate")]
        public DateTime? EndDate { get; set; }

        [JsonPropertyName("orderIndex")]
        public int OrderIndex { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("deletedAt")]
        public DateTime? DeletedAt { get; set; }

        // Limit dates for warning display (computed from project/parent task)
        // Not stored in database - populated by query join

        [NotMapped]
        [JsonPropertyName("projectStartDate")]
        public DateTime? ProjectStartDate { get; set; }

        [NotMapped]
        [JsonPropertyName("projectEndDate")]
        public DateTime? ProjectEndDate { get; set; }

        [NotMapped]
        [JsonPropertyName("parentStartDate")]
        public DateTime? ParentStartDate { get; set; }

        [NotMapped]
        [JsonPropertyName("parentEndDate")]
        public DateTime? ParentEndDate { get; set; }
    }
}
