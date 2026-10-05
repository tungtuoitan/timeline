using System.Text.Json.Serialization;

namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Add a link to a task/project (task #1477). Either a new URL (url + optional name) or,
    /// for tasks only, an existing workspace item (workspaceItemId) of the same workspace.
    /// </summary>
    public class AddLinkRequest
    {
        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("workspaceItemId")]
        public int? WorkspaceItemId { get; set; }
    }

    /// <summary>
    /// Rename a file (any file) and/or change its URL (links only).
    /// </summary>
    public class UpdateFileRequest
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }
    }
}
