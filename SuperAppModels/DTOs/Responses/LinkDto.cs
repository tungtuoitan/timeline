using System.Text.Json.Serialization;

namespace SuperAppModels.DTOs.Responses
{
    /// <summary>
    /// One item in the "Links" list of a task or project (task #1477).
    /// Usually a link (file with mimeType text/x-uri), but a task may also be linked to an
    /// existing note/file/folder of its project's workspace.
    /// </summary>
    public class LinkDto
    {
        [JsonPropertyName("workspaceItemId")]
        public int WorkspaceItemId { get; set; }

        [JsonPropertyName("workspaceId")]
        public int WorkspaceId { get; set; }

        [JsonPropertyName("parentId")]
        public int? ParentId { get; set; }

        [JsonPropertyName("entityType")]
        public byte EntityType { get; set; }

        [JsonPropertyName("entityId")]
        public int EntityId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("mimeType")]
        public string? MimeType { get; set; }

        [JsonPropertyName("isLink")]
        public bool IsLink { get; set; }

        /// <summary>pro.task_workspace_item.id when linked explicitly; null for a link that only sits in the task folder.</summary>
        [JsonPropertyName("taskWorkspaceItemId")]
        public int? TaskWorkspaceItemId { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime? CreatedAt { get; set; }
    }
}
