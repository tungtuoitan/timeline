using System.ComponentModel.DataAnnotations;

namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Request DTO for sharing a folder to a workspace
    /// </summary>
    public class ShareFolderRequest
    {
        /// <summary>
        /// Target workspace ID to share the folder into
        /// </summary>
        [Required(ErrorMessage = "Workspace ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Workspace ID must be a positive integer")]
        public int WorkspaceId { get; set; }

        /// <summary>
        /// Folder ID to share
        /// </summary>
        [Required(ErrorMessage = "Folder ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Folder ID must be a positive integer")]
        public int FolderId { get; set; }

        /// <summary>
        /// Parent folder ID in the target workspace (null for root level)
        /// </summary>
        public int? ParentFolderId { get; set; }

        /// <summary>
        /// Sort order in the target location
        /// </summary>
        public int SortOrder { get; set; } = 0;
    }
}
