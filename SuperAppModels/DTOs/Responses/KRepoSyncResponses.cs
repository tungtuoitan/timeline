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

    /// <summary>One entry in a repo-vs-DB compare result.</summary>
    public class KRepoCompareEntry
    {
        /// <summary>"knowledge" | "node" | "question"</summary>
        [JsonPropertyName("entityType")]
        public string EntityType { get; set; } = "";

        /// <summary>"repo_only" | "db_only" | "modified"</summary>
        [JsonPropertyName("changeType")]
        public string ChangeType { get; set; } = "";

        [JsonPropertyName("dbId")]
        public int? DbId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("knowledgeName")]
        public string? KnowledgeName { get; set; }

        [JsonPropertyName("nodeName")]
        public string? NodeName { get; set; }

        /// <summary>Repo-relative path (for repo_only entries).</summary>
        [JsonPropertyName("repoPath")]
        public string? RepoPath { get; set; }

        /// <summary>DB text (for modified questions) or old name (for renames).</summary>
        [JsonPropertyName("oldText")]
        public string? OldText { get; set; }

        /// <summary>Repo text (for modified/repo_only questions) or new name.</summary>
        [JsonPropertyName("newText")]
        public string? NewText { get; set; }
    }

    public class KRepoCompareDiffResponse
    {
        [JsonPropertyName("entries")]
        public List<KRepoCompareEntry> Entries { get; set; } = [];

        [JsonPropertyName("repoOnlyCount")]
        public int RepoOnlyCount { get; set; }

        [JsonPropertyName("dbOnlyCount")]
        public int DbOnlyCount { get; set; }

        [JsonPropertyName("modifiedCount")]
        public int ModifiedCount { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }
}
