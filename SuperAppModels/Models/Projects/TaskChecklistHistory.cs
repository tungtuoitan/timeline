using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SuperAppModels.Models
{
    /// <summary>
    /// Daily checklist snapshot for repeat-checklist (build-habit) tasks.
    /// Maps to pro.task_checklist_history table.
    /// </summary>
    public class TaskChecklistHistory
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("taskId")]
        public int TaskId { get; set; }

        [JsonPropertyName("date")]
        [Column("date", TypeName = "date")]
        public DateOnly Date { get; set; }

        [JsonPropertyName("checklistSnapshot")]
        [Column("checklist_snapshot")]
        public string ChecklistSnapshot { get; set; } = string.Empty;

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }
    }
}
