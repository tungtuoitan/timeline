using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Request for creating or updating a task (upsert operation)
    /// If Id is 0, creates a new task. Otherwise, updates the existing task.
    /// </summary>
    public class UpsertTaskRequest
    {
        /// <summary>
        /// Task ID (0 for create, >0 for update)
        /// </summary>
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [Required(ErrorMessage = "ProjectId is required")]
        [JsonPropertyName("projectId")]
        public int ProjectId { get; set; }

        [JsonPropertyName("parentTaskId")]
        public int? ParentTaskId { get; set; }

        [StringLength(20, ErrorMessage = "Type cannot exceed 20 characters")]
        [JsonPropertyName("type")]
        public string Type { get; set; } = "task";

        [StringLength(50, ErrorMessage = "TaskType cannot exceed 50 characters")]
        [JsonPropertyName("taskType")]
        public string TaskType { get; set; } = "personal";

        [Required(ErrorMessage = "Title is required")]
        [StringLength(500, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 500 characters")]
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("note")]
        public string? Note { get; set; }

        [StringLength(20, ErrorMessage = "Status cannot exceed 20 characters")]
        [JsonPropertyName("status")]
        public string Status { get; set; } = "open";

        [StringLength(20, ErrorMessage = "Priority cannot exceed 20 characters")]
        [JsonPropertyName("priority")]
        public string Priority { get; set; } = "low";

        [JsonPropertyName("startDate")]
        public DateTime? StartDate { get; set; }
        [JsonPropertyName("endDate")]
        public DateTime? EndDate { get; set; }

        [JsonPropertyName("orderIndex")]
        public int OrderIndex { get; set; }

        /// <summary>
        /// Optional: Soft delete timestamp (null = active, DateTime = soft deleted)
        /// Enables soft delete/restore via upsert
        /// </summary>
        [JsonPropertyName("deletedAt")]
        public DateTime? DeletedAt { get; set; }

        /// <summary>
        /// Optional: Workspace item ID of the folder linked to this task
        /// Set when the first note is created for this task
        /// </summary>
        [JsonPropertyName("folderWorkspaceItemId")]
        public int? FolderWorkspaceItemId { get; set; }

        /// <summary>JSON checklist definition and state. Stored as ChecklistJSON object serialized to string.</summary>
        [JsonPropertyName("checklistJson")]
        public string? ChecklistJson { get; set; }

        /// <summary>JSON process/step definition and state. Same structure as ChecklistJSON.</summary>
        [JsonPropertyName("processJson")]
        public string? ProcessJson { get; set; }
    }
}
