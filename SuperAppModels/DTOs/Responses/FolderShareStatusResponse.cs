namespace SuperAppModels.DTOs.Responses
{
    /// <summary>
    /// Response DTO for folder share status
    /// </summary>
    public class FolderShareStatusResponse
    {
        /// <summary>
        /// Link type in current workspace ('owned' or 'shared')
        /// </summary>
        public string LinkType { get; set; } = string.Empty;

        /// <summary>
        /// Total number of workspaces containing this folder
        /// </summary>
        public int TotalWorkspacesCount { get; set; }

        /// <summary>
        /// Whether the folder can be forked in current workspace
        /// (true if link_type = 'shared')
        /// </summary>
        public bool CanFork { get; set; }

        /// <summary>
        /// List of workspaces containing this folder
        /// </summary>
        public List<WorkspaceInfo> Workspaces { get; set; } = new();
    }

    public class WorkspaceInfo
    {
        public int WorkspaceId { get; set; }
        public string WorkspaceName { get; set; } = string.Empty;
        public string LinkType { get; set; } = string.Empty;
    }
}
