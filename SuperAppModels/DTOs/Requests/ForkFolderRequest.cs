using System.ComponentModel.DataAnnotations;

namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Request DTO for forking a shared folder (create independent copy)
    /// </summary>
    public class ForkFolderRequest
    {
        /// <summary>
        /// Workspace ID where the folder is currently shared
        /// </summary>
        [Required(ErrorMessage = "Workspace ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Workspace ID must be a positive integer")]
        public int WorkspaceId { get; set; }

        /// <summary>
        /// Folder ID to fork
        /// </summary>
        [Required(ErrorMessage = "Folder ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Folder ID must be a positive integer")]
        public int FolderId { get; set; }
    }
}
