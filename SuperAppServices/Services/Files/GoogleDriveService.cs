using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SuperAppModels.DTOs.Responses;
using SuperAppServices.Interfaces;

namespace SuperAppServices.Services.Files
{
    /// <summary>
    /// Google Drive service implementation
    /// Uses user's OAuth access token to upload to their personal Drive
    /// </summary>
    public class GoogleDriveService : IGoogleDriveService
    {
        private readonly ILogger<GoogleDriveService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        // Threshold for resumable upload (5MB)
        private const long ResumableUploadThreshold = 5 * 1024 * 1024;

        // Folder MIME type
        private const string FolderMimeType = "application/vnd.google-apps.folder";

        // Root folder name in user's Drive
        private const string RootFolderName = "SuperApp";

        public GoogleDriveService(
            IConfiguration configuration,
            ILogger<GoogleDriveService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        /// <summary>
        /// Create Drive service using user's access token
        /// </summary>
        private DriveService CreateDriveService(string accessToken)
        {
            var credential = GoogleCredential.FromAccessToken(accessToken);

            return new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "SuperApp"
            });
        }

        /// <summary>
        /// Upload file to user's Google Drive
        /// Folder structure: My Drive/SuperApp/{context}/{contextId}/{file}
        /// </summary>
        public async Task<DriveUploadResult> UploadFileAsync(
            string userAccessToken,
            Stream fileStream,
            string fileName,
            string mimeType,
            string context,
            int contextId)
        {
            try
            {
                _logger.LogInformation(
                    "Uploading file: {FileName} ({MimeType}) to {Context}/{ContextId}",
                    fileName, mimeType, context, contextId);

                using var driveService = CreateDriveService(userAccessToken);

                // Ensure folder structure exists: SuperApp/{context}/{contextId}
                var rootFolderId = await EnsureFolderAsync(driveService, "root", RootFolderName);
                var contextFolderId = await EnsureFolderAsync(driveService, rootFolderId, context);
                var targetFolderId = await EnsureFolderAsync(driveService, contextFolderId, contextId.ToString());

                // Create file metadata
                var fileMetadata = new Google.Apis.Drive.v3.Data.File
                {
                    Name = fileName,
                    Parents = new List<string> { targetFolderId }
                };

                // Determine upload method based on file size
                var fileSize = fileStream.Length;
                Google.Apis.Drive.v3.Data.File? uploadedFile;

                if (fileSize > ResumableUploadThreshold)
                {
                    _logger.LogInformation("Using resumable upload for file > 5MB ({FileSize} bytes)", fileSize);
                    uploadedFile = await UploadResumableAsync(driveService, fileMetadata, fileStream, mimeType);
                }
                else
                {
                    _logger.LogInformation("Using simple upload for file <= 5MB ({FileSize} bytes)", fileSize);
                    uploadedFile = await UploadSimpleAsync(driveService, fileMetadata, fileStream, mimeType);
                }

                if (uploadedFile == null)
                {
                    return new DriveUploadResult
                    {
                        Success = false,
                        ErrorMessage = "Upload failed - no file returned"
                    };
                }

                // File stays private - will be served through proxy endpoint
                // Get file details
                var fileDetails = await GetFileDetailsAsync(driveService, uploadedFile.Id);

                _logger.LogInformation(
                    "File uploaded successfully: {FileId}",
                    uploadedFile.Id);

                return new DriveUploadResult
                {
                    Success = true,
                    FileId = uploadedFile.Id,
                    WebViewLink = fileDetails?.WebViewLink,
                    WebContentLink = fileDetails?.WebContentLink,
                    FileSize = fileSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload file: {FileName}", fileName);
                return new DriveUploadResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Simple upload for files <= 5MB
        /// </summary>
        private async Task<Google.Apis.Drive.v3.Data.File?> UploadSimpleAsync(
            DriveService driveService,
            Google.Apis.Drive.v3.Data.File fileMetadata,
            Stream fileStream,
            string mimeType)
        {
            var request = driveService.Files.Create(fileMetadata, fileStream, mimeType);
            request.Fields = "id, name, size, mimeType, webViewLink, webContentLink";

            var result = await request.UploadAsync();

            if (result.Status == UploadStatus.Failed)
            {
                _logger.LogError("Simple upload failed: {Error}", result.Exception?.Message);
                throw result.Exception ?? new Exception("Upload failed");
            }

            return request.ResponseBody;
        }

        /// <summary>
        /// Resumable upload for files > 5MB
        /// </summary>
        private async Task<Google.Apis.Drive.v3.Data.File?> UploadResumableAsync(
            DriveService driveService,
            Google.Apis.Drive.v3.Data.File fileMetadata,
            Stream fileStream,
            string mimeType)
        {
            var request = driveService.Files.Create(fileMetadata, fileStream, mimeType);
            request.Fields = "id, name, size, mimeType, webViewLink, webContentLink";

            // Set chunk size for resumable upload
            request.ChunkSize = ResumableUpload.MinimumChunkSize * 20; // ~5MB chunks

            // Track progress
            request.ProgressChanged += progress =>
            {
                switch (progress.Status)
                {
                    case UploadStatus.Uploading:
                        _logger.LogDebug("Upload progress: {BytesSent} bytes sent", progress.BytesSent);
                        break;
                    case UploadStatus.Completed:
                        _logger.LogInformation("Upload completed: {BytesSent} bytes", progress.BytesSent);
                        break;
                    case UploadStatus.Failed:
                        _logger.LogError("Upload failed: {Error}", progress.Exception?.Message);
                        break;
                }
            };

            var result = await request.UploadAsync();

            if (result.Status == UploadStatus.Failed)
            {
                _logger.LogError("Resumable upload failed: {Error}", result.Exception?.Message);
                throw result.Exception ?? new Exception("Upload failed");
            }

            return request.ResponseBody;
        }

        /// <summary>
        /// Get file details including web links
        /// </summary>
        private async Task<Google.Apis.Drive.v3.Data.File?> GetFileDetailsAsync(DriveService driveService, string fileId)
        {
            try
            {
                var request = driveService.Files.Get(fileId);
                request.Fields = "id, name, size, mimeType, webViewLink, webContentLink, createdTime, modifiedTime";
                return await request.ExecuteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get file details: {FileId}", fileId);
                return null;
            }
        }

        /// <summary>
        /// Download file content from Google Drive (for proxy serving)
        /// </summary>
        public async Task<DriveFileDownloadResult?> DownloadFileAsync(string userAccessToken, string driveFileId)
        {
            try
            {
                using var driveService = CreateDriveService(userAccessToken);

                // Get file metadata first
                var fileRequest = driveService.Files.Get(driveFileId);
                fileRequest.Fields = "id, name, mimeType, size";
                var fileMetadata = await fileRequest.ExecuteAsync();

                if (fileMetadata == null)
                {
                    _logger.LogWarning("File not found in Drive: {FileId}", driveFileId);
                    return null;
                }

                // Download file content
                var stream = new MemoryStream();
                var downloadRequest = driveService.Files.Get(driveFileId);
                var downloadProgress = await downloadRequest.DownloadAsync(stream);

                if (downloadProgress.Status != Google.Apis.Download.DownloadStatus.Completed)
                {
                    _logger.LogError("Failed to download file: {FileId}, Status: {Status}",
                        driveFileId, downloadProgress.Status);
                    return null;
                }

                stream.Position = 0;

                return new DriveFileDownloadResult
                {
                    Stream = stream,
                    FileName = fileMetadata.Name,
                    MimeType = fileMetadata.MimeType,
                    FileSize = fileMetadata.Size ?? 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to download file from Drive: {FileId}", driveFileId);
                return null;
            }
        }

        /// <summary>
        /// Ensures a folder exists, creates it if not
        /// </summary>
        private async Task<string> EnsureFolderAsync(DriveService driveService, string parentFolderId, string folderName)
        {
            try
            {
                // Search for existing folder
                var listRequest = driveService.Files.List();
                listRequest.Q = $"name = '{folderName}' and '{parentFolderId}' in parents and mimeType = '{FolderMimeType}' and trashed = false";
                listRequest.Fields = "files(id, name)";

                var result = await listRequest.ExecuteAsync();

                if (result.Files.Any())
                {
                    _logger.LogDebug("Found existing folder: {FolderName} ({FolderId})", folderName, result.Files[0].Id);
                    return result.Files[0].Id;
                }

                // Create new folder
                var folderMetadata = new Google.Apis.Drive.v3.Data.File
                {
                    Name = folderName,
                    MimeType = FolderMimeType,
                    Parents = new List<string> { parentFolderId }
                };

                var createRequest = driveService.Files.Create(folderMetadata);
                createRequest.Fields = "id";

                var folder = await createRequest.ExecuteAsync();

                _logger.LogInformation("Created new folder: {FolderName} ({FolderId})", folderName, folder.Id);
                return folder.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to ensure folder: {FolderName} in parent {ParentId}", folderName, parentFolderId);
                throw;
            }
        }

        /// <summary>
        /// Delete file from Google Drive
        /// </summary>
        public async Task<bool> DeleteFileAsync(string userAccessToken, string fileId)
        {
            try
            {
                using var driveService = CreateDriveService(userAccessToken);
                await driveService.Files.Delete(fileId).ExecuteAsync();
                _logger.LogInformation("Deleted file from Google Drive: {FileId}", fileId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete file: {FileId}", fileId);
                return false;
            }
        }

        /// <summary>
        /// Get file info from Google Drive
        /// </summary>
        public async Task<DriveFileInfo?> GetFileInfoAsync(string userAccessToken, string fileId)
        {
            try
            {
                using var driveService = CreateDriveService(userAccessToken);
                var file = await GetFileDetailsAsync(driveService, fileId);
                if (file == null) return null;

                return new DriveFileInfo
                {
                    Id = file.Id,
                    Name = file.Name,
                    MimeType = file.MimeType,
                    Size = file.Size ?? 0,
                    WebViewLink = file.WebViewLink,
                    WebContentLink = file.WebContentLink,
                    CreatedTime = file.CreatedTimeDateTimeOffset?.UtcDateTime,
                    ModifiedTime = file.ModifiedTimeDateTimeOffset?.UtcDateTime
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get file info: {FileId}", fileId);
                return null;
            }
        }

        /// <summary>
        /// Refresh access token using refresh token
        /// </summary>
        public async Task<(string? AccessToken, DateTime? ExpiresAt)> RefreshAccessTokenAsync(string refreshToken)
        {
            try
            {
                var clientId = _configuration["OAuth:Google:ClientId"] ?? "";
                var clientSecret = _configuration["OAuth:Google:ClientSecret"] ?? "";

                var client = _httpClientFactory.CreateClient();
                var tokenEndpoint = "https://oauth2.googleapis.com/token";

                var requestData = new Dictionary<string, string>
                {
                    { "client_id", clientId },
                    { "client_secret", clientSecret },
                    { "refresh_token", refreshToken },
                    { "grant_type", "refresh_token" }
                };

                var response = await client.PostAsync(tokenEndpoint, new FormUrlEncodedContent(requestData));

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to refresh token: {Error}", error);
                    return (null, null);
                }

                var content = await response.Content.ReadAsStringAsync();
                var tokenResponse = System.Text.Json.JsonSerializer.Deserialize<RefreshTokenResponse>(content);

                if (tokenResponse == null)
                {
                    return (null, null);
                }

                var expiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
                return (tokenResponse.AccessToken, expiresAt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh access token");
                return (null, null);
            }
        }

        private class RefreshTokenResponse
        {
            [System.Text.Json.Serialization.JsonPropertyName("access_token")]
            public string AccessToken { get; set; } = string.Empty;

            [System.Text.Json.Serialization.JsonPropertyName("expires_in")]
            public int ExpiresIn { get; set; }
        }

        /// <summary>
        /// Get storage quota information from Google Drive
        /// </summary>
        public async Task<DriveQuotaInfo?> GetStorageQuotaAsync(string userAccessToken)
        {
            try
            {
                using var driveService = CreateDriveService(userAccessToken);

                var aboutRequest = driveService.About.Get();
                aboutRequest.Fields = "storageQuota";

                var about = await aboutRequest.ExecuteAsync();

                if (about?.StorageQuota == null)
                {
                    _logger.LogWarning("Failed to get storage quota - null response");
                    return null;
                }

                var quota = about.StorageQuota;

                // Google returns null for unlimited storage (Google Workspace accounts)
                // In that case, set a large default (1TB)
                var totalBytes = quota.Limit ?? 1099511627776L; // 1TB default
                var usedBytes = quota.Usage ?? 0;

                _logger.LogInformation(
                    "Storage quota: Used {UsedMB}MB / {TotalMB}MB, Available: {AvailableMB}MB",
                    usedBytes / 1024 / 1024,
                    totalBytes / 1024 / 1024,
                    (totalBytes - usedBytes) / 1024 / 1024);

                return new DriveQuotaInfo
                {
                    TotalBytes = totalBytes,
                    UsedBytes = usedBytes
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get storage quota");
                return null;
            }
        }
    }
}
