using SuperAppModels.DTOs.Responses;

namespace SuperAppServices.Interfaces
{
    /// <summary>
    /// Service interface for Google Drive operations
    /// Uses user's OAuth access token to upload to their personal Drive
    /// </summary>
    public interface IGoogleDriveService
    {
        /// <summary>
        /// Upload file to user's Google Drive
        /// Folder structure: My Drive/SuperApp/{context}/{contextId}/{file}
        /// </summary>
        /// <param name="userAccessToken">User's Google OAuth access token</param>
        /// <param name="fileStream">File stream to upload</param>
        /// <param name="fileName">File name</param>
        /// <param name="mimeType">MIME type of the file</param>
        /// <param name="context">Context type: "project" or "workspace"</param>
        /// <param name="contextId">Context ID (project_id or workspace_id)</param>
        /// <returns>Upload result with file ID and URLs</returns>
        Task<DriveUploadResult> UploadFileAsync(
            string userAccessToken,
            Stream fileStream,
            string fileName,
            string mimeType,
            string context,
            int contextId);

        /// <summary>
        /// Delete file from Google Drive
        /// </summary>
        /// <param name="userAccessToken">User's Google OAuth access token</param>
        /// <param name="fileId">Google Drive file ID</param>
        /// <returns>True if deleted successfully</returns>
        Task<bool> DeleteFileAsync(string userAccessToken, string fileId);

        /// <summary>
        /// Get file info from Google Drive
        /// </summary>
        /// <param name="userAccessToken">User's Google OAuth access token</param>
        /// <param name="fileId">Google Drive file ID</param>
        /// <returns>File info</returns>
        Task<DriveFileInfo?> GetFileInfoAsync(string userAccessToken, string fileId);

        /// <summary>
        /// Refresh access token using refresh token
        /// </summary>
        /// <param name="refreshToken">Google refresh token</param>
        /// <returns>New access token and expiration time</returns>
        Task<(string? AccessToken, DateTime? ExpiresAt)> RefreshAccessTokenAsync(string refreshToken);

        /// <summary>
        /// Get storage quota information from Google Drive
        /// </summary>
        /// <param name="userAccessToken">User's Google OAuth access token</param>
        /// <returns>Storage quota info (total, used, available)</returns>
        Task<DriveQuotaInfo?> GetStorageQuotaAsync(string userAccessToken);

        /// <summary>
        /// Download file content from Google Drive (for proxy serving)
        /// </summary>
        /// <param name="userAccessToken">User's Google OAuth access token</param>
        /// <param name="driveFileId">Google Drive file ID</param>
        /// <returns>File stream with metadata</returns>
        Task<DriveFileDownloadResult?> DownloadFileAsync(string userAccessToken, string driveFileId);
    }
}
