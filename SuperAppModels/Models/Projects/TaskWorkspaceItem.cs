using System.Text.Json.Serialization;

namespace SuperAppModels.Models
{
    /// <summary>
    /// Links a task to workspace items (folders/notes/files/links)
    /// Maps to pro.task_workspace_item
    /// </summary>
    public class TaskWorkspaceItem
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("taskId")]
        public int TaskId { get; set; }

        [JsonPropertyName("workspaceItemId")]
        public int WorkspaceItemId { get; set; }

        /// <summary>
        /// entity_type of the linked workspace item: 2 = Folder, 3 = Note, 4 = File/link (#1477)
        /// </summary>
        [JsonPropertyName("itemType")]
        public int ItemType { get; set; }
    }
}
