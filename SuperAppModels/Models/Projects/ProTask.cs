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

        [JsonPropertyName("taskType")]
        [Column("task_type")]
        public string TaskType { get; set; } = "personal";

        /// <summary>JSON checklist definition and state (ChecklistJSON serialized). Null = no checklist.</summary>
        [JsonPropertyName("checklistJson")]
        [Column("checklist_json")]
        public string? ChecklistJson { get; set; }

        /// <summary>JSON process/step definition and state (same structure as ChecklistJSON). Null = no process.</summary>
        [JsonPropertyName("processJson")]
        [Column("process_json")]
        public string? ProcessJson { get; set; }

        /// <summary>JSON custom tabs: user-created tabs with name/version/content. Null = no custom tabs.</summary>
        [JsonPropertyName("customTabsJson")]
        [Column("custom_tabs_json")]
        public string? CustomTabsJson { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        // TODO(0109): remove legacy "note" alias after old FE is gone
        /// <summary>Read-only legacy alias of Description so an old frontend still displays it. Not stored.</summary>
        [NotMapped]
        [JsonPropertyName("note")]
        public string? LegacyNote => Description;

        [JsonPropertyName("status")]
        public string Status { get; set; } = "open";

        [JsonPropertyName("priority")]
        public string Priority { get; set; } = "low";

        [JsonPropertyName("startDate")]
        public DateOnly? StartDate { get; set; }

        [JsonPropertyName("endDate")]
        public DateOnly? EndDate { get; set; }

        [JsonPropertyName("orderIndex")]
        public int OrderIndex { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("deletedAt")]
        public DateTime? DeletedAt { get; set; }

        /// <summary>
        /// Marks this task as a milestone (e.g. shown with diamond icon, treated as a key date).
        /// Default false.
        /// </summary>
        [JsonPropertyName("isMilestone")]
        [Column("is_milestone")]
        public bool IsMilestone { get; set; }

        /// <summary>
        /// Private data (e.g. a personal habit tracker): homepage APIs only return it with a TOTP
        /// unlock token (TungRoot #1489). Set via PATCH only — upsert keeps the stored value.
        /// </summary>
        [JsonPropertyName("isSensitive")]
        [Column("is_sensitive")]
        public bool IsSensitive { get; set; }

        /// <summary>
        /// Workspace item ID of the folder linked to this task (ws.workspace_items.id)
        /// Set when the first note is created for this task
        /// </summary>
        [JsonPropertyName("folderWorkspaceItemId")]
        public int? FolderWorkspaceItemId { get; set; }

        // Limit dates for warning display (computed from project/parent task)
        // Not stored in database - populated by query join

        [NotMapped]
        [JsonPropertyName("projectStartDate")]
        public DateOnly? ProjectStartDate { get; set; }

        [NotMapped]
        [JsonPropertyName("projectEndDate")]
        public DateOnly? ProjectEndDate { get; set; }

        [NotMapped]
        [JsonPropertyName("parentStartDate")]
        public DateOnly? ParentStartDate { get; set; }

        [NotMapped]
        [JsonPropertyName("parentEndDate")]
        public DateOnly? ParentEndDate { get; set; }
    }
}
