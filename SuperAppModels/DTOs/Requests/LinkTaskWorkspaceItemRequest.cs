using System.Text.Json.Serialization;

namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Request to link a workspace item (folder/note) to a task
    /// </summary>
    public class LinkTaskWorkspaceItemRequest
    {
        [JsonPropertyName("workspaceItemId")]
        public int WorkspaceItemId { get; set; }

        /// <summary>
        /// 2 = Folder, 3 = Note
        /// </summary>
        [JsonPropertyName("itemType")]
        public int ItemType { get; set; }
    }
}
