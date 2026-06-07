using System.Text.Json.Serialization;

namespace SuperAppModels.DTOs.Responses
{
    public class KRepoSyncStatusResponse
    {
        [JsonPropertyName("repoUrl")]
        public string? RepoUrl { get; set; }

        [JsonPropertyName("branch")]
        public string? Branch { get; set; }

        [JsonPropertyName("statusCode")]
        public string StatusCode { get; set; } = "idle";

        [JsonPropertyName("statusMessage")]
        public string? StatusMessage { get; set; }

        [JsonPropertyName("lastPushAt")]
        public DateTime? LastPushAt { get; set; }

        [JsonPropertyName("lastCheckAt")]
        public DateTime? LastCheckAt { get; set; }

        [JsonPropertyName("isConfigured")]
        public bool IsConfigured { get; set; }
    }

    public class KRepoSyncDiffItem
    {
        [JsonPropertyName("nodeId")]
        public int NodeId { get; set; }

        [JsonPropertyName("nodeName")]
        public string NodeName { get; set; } = "";

        [JsonPropertyName("questionId")]
        public int? QuestionId { get; set; }

        [JsonPropertyName("oldText")]
        public string? OldText { get; set; }

        [JsonPropertyName("newText")]
        public string? NewText { get; set; }

        [JsonPropertyName("changeType")]
        public string ChangeType { get; set; } = "modified"; // added | modified | removed
    }

    public class KRepoSyncDiffResponse
    {
        [JsonPropertyName("items")]
        public List<KRepoSyncDiffItem> Items { get; set; } = [];
    }
}
