namespace SuperAppModels.DTOs.Responses
{
    /// <summary>
    /// Represents a workspace link to an entity (Note/File)
    /// Used to show which workspaces reference a particular entity
    /// </summary>
    public class KWorkspaceLinkDTO
    {
        /// <summary>
        /// Workspace ID
        /// </summary>
        public int WorkspaceId { get; set; }

        /// <summary>
        /// Workspace name
        /// </summary>
        public string WorkspaceName { get; set; } = string.Empty;

        /// <summary>
        /// workspace_items.id - The specific workspace item linking to this entity
        /// </summary>
        public int WorkspaceItemId { get; set; } 
    }
}
