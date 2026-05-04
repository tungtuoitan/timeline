using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Responses;

namespace SuperAppServices.Interfaces
{
    /// <summary>
    /// Service interface for file upload operations
    /// </summary>
    public interface IFileService
    {
        /// <summary>
        /// Upload a file to Google Drive with automatic store selection
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="fileStream">File stream</param>
        /// <param name="fileName">Original file name</param>
        /// <param name="mimeType">MIME type</param>
        /// <param name="context">Context: "project" or "workspace"</param>
        /// <param name="contextId">Context ID</param>
        /// <returns>ResultOptions with upload info</returns>
        Task<ResultOptions> UploadFileAsync(
            int userId,
            Stream fileStream,
            string fileName,
            string mimeType,
            string context,
            int contextId);

        /// <summary>
        /// Delete a file from storage and database
        /// </summary>
        /// <param name="fileId">File ID in database</param>
        /// <param name="userId">User ID (for ownership verification)</param>
        /// <returns>ResultOptions</returns>
        Task<ResultOptions> DeleteFileAsync(int fileId, int userId);

        /// <summary>
        /// Get file by ID
        /// </summary>
        Task<ResultOptions> GetFileByIdAsync(int fileId);

        /// <summary>
        /// Get file content for proxy serving (downloads from Google Drive)
        /// </summary>
        /// <param name="fileId">Database file ID</param>
        /// <param name="userId">User ID for authorization</param>
        /// <returns>File stream with metadata, or null if not found/unauthorized</returns>
        Task<DriveFileDownloadResult?> GetFileContentAsync(int fileId, int userId);
    }
}
