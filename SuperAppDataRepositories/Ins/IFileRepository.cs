using SuperAppModels.Models;
using FileInfo = SuperAppModels.Models.FileInfo;

namespace SuperAppDataRepositories.Ins
{
    /// <summary>
    /// Repository interface for file/document operations
    /// Supports CRUD operations and workspace integration for files table
    /// </summary>
    public interface IFileRepository
    {
        /// <summary>
        /// Gets a file by ID with user ownership validation
        /// </summary>
        /// <param name="fileId">File identifier</param>
        /// <param name="userId">User identifier for ownership check</param>
        /// <returns>FileInfo if found and owned by user, null otherwise</returns>
        Task<FileInfo?> GetFileByIdAsync(int fileId, int userId);

        /// <summary>
        /// Gets all files for a specific user
        /// </summary>
        /// <param name="userId">User identifier</param>
        /// <param name="includeArchived">Include archived files (default: false)</param>
        /// <param name="searchText">Optional search text for file name or description</param>
        /// <returns>List of files owned by the user</returns>
        Task<List<FileInfo>> GetFilesByUserAsync(int userId, bool includeArchived = false, string? searchText = null);

        /// <summary>
        /// Gets all files in a workspace (via workspace_items table)
        /// </summary>
        /// <param name="workspaceId">Workspace identifier</param>
        /// <param name="userId">User identifier for permission check</param>
        /// <returns>List of files in the workspace</returns>
        Task<List<FileInfo>> GetFilesByWorkspaceAsync(int workspaceId, int userId);

        /// <summary>
        /// Creates a new file record
        /// </summary>
        /// <param name="file">File entity to create</param>
        /// <param name="parentTagId">Optional parent tag ID for workspace organization</param>
        /// <param name="workspaceId">Optional workspace ID to add file to</param>
        /// <returns>Created file with generated FileId and slug</returns>
        Task<FileInfo> CreateFileAsync(FileInfo file, int? parentTagId = null, int? workspaceId = null);

        /// <summary>
        /// Updates an existing file's metadata
        /// </summary>
        /// <param name="file">File entity with updated properties</param>
        /// <param name="userId">User identifier for ownership validation</param>
        /// <returns>Updated file entity</returns>
        Task<FileInfo> UpdateFileAsync(FileInfo file, int userId);

        /// <summary>
        /// Soft deletes a file (sets DeletedAt timestamp)
        /// </summary>
        /// <param name="fileId">File identifier</param>
        /// <param name="userId">User identifier for ownership validation</param>
        /// <returns>True if deleted, false if not found or not owned</returns>
        Task<bool> DeleteFileAsync(int fileId, int userId);

        /// <summary>
        /// Permanently deletes a file record and removes physical file
        /// </summary>
        /// <param name="fileId">File identifier</param>
        /// <param name="userId">User identifier for ownership validation</param>
        /// <returns>True if deleted, false if not found or not owned</returns>
        Task<bool> PermanentDeleteFileAsync(int fileId, int userId);

        /// <summary>
        /// Archives a file (sets IsArchived flag)
        /// </summary>
        /// <param name="fileId">File identifier</param>
        /// <param name="userId">User identifier for ownership validation</param>
        /// <returns>True if archived, false if not found or not owned</returns>
        Task<bool> ArchiveFileAsync(int fileId, int userId);

        /// <summary>
        /// Unarchives a file (clears IsArchived flag)
        /// </summary>
        /// <param name="fileId">File identifier</param>
        /// <param name="userId">User identifier for ownership validation</param>
        /// <returns>True if unarchived, false if not found or not owned</returns>
        Task<bool> UnarchiveFileAsync(int fileId, int userId);

        /// <summary>
        /// Increments download count and updates LastDownloadedAt timestamp
        /// </summary>
        /// <param name="fileId">File identifier</param>
        /// <returns>Updated download count</returns>
        Task<int> IncrementDownloadCountAsync(int fileId);

        /// <summary>
        /// Checks if a file exists and is owned by the user
        /// </summary>
        /// <param name="fileId">File identifier</param>
        /// <param name="userId">User identifier</param>
        /// <returns>True if file exists and is owned by user</returns>
        Task<bool> ExistsAsync(int fileId, int userId);

        /// <summary>
        /// Gets total storage size used by a user's files
        /// </summary>
        /// <param name="userId">User identifier</param>
        /// <returns>Total bytes used by all user's files</returns>
        Task<long> GetTotalStorageSizeAsync(int userId);
    }
}
