using System.Text.Json.Serialization;

namespace SuperAppModels.Models
{
    /// <summary>
    /// Links a task to workspace items (folders/notes)
    /// Maps to pro.TaskWorkspaceItem table
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
        /// 2 = Folder, 3 = Note
        /// </summary>
        [JsonPropertyName("itemType")]
        public int ItemType { get; set; }
    }
}
