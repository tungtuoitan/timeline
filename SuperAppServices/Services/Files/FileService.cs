using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Data;
using SuperAppDataRepositories.Ins;
using SuperAppModels.Utils;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Responses;
using SuperAppModels.Models;
using SuperAppServices.Interfaces;

namespace SuperAppServices.Services.Files
{
    /// <summary>
    /// File service for handling file uploads to user's Google Drive
    /// </summary>
    public class FileService : IFileService
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserRepository _userRepository;
        private readonly IGoogleDriveService _googleDriveService;
        private readonly ILogger<FileService> _logger;

        // Allowed image extensions
        private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg" };

        // Allowed file extensions for attachments
        private static readonly string[] AllowedFileExtensions = {
            ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
            ".txt", ".csv", ".zip", ".rar", ".7z"
        };

        // Max file sizes
        private const long MaxImageSize = 10 * 1024 * 1024; // 10MB for images
        private const long MaxFileSize = 50 * 1024 * 1024;  // 50MB for files

        public FileService(
            ApplicationDbContext context,
            IUserRepository userRepository,
            IGoogleDriveService googleDriveService,
            ILogger<FileService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _googleDriveService = googleDriveService ?? throw new ArgumentNullException(nameof(googleDriveService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Upload a file to user's Google Drive
        /// </summary>
        public async Task<ResultOptions> UploadFileAsync(
            int userId,
            Stream fileStream,
            string fileName,
            string mimeType,
            string context,
            int contextId)
        {
            try
            {
                _logger.LogInformation(
                    "Starting file upload: {FileName} ({MimeType}) for user {UserId}, context: {Context}/{ContextId}",
                    fileName, mimeType, userId, context, contextId);

                // Validate context
                var validContexts = new[] { "project", "workspace", "general" };
                if (string.IsNullOrWhiteSpace(context) || !validContexts.Contains(context))
                {
                    return ResultOptions.Fail($"Invalid context. Must be one of: {string.Join(", ", validContexts)}.", 400);
                }

                // contextId is required only for project/workspace contexts
                if ((context == "project" || context == "workspace") && contextId <= 0)
                {
                    return ResultOptions.Fail("Invalid context ID.", 400);
                }

                // Validate file size
                var fileSize = fileStream.Length;
                var extension = Path.GetExtension(fileName).ToLowerInvariant();
                var isImage = AllowedImageExtensions.Contains(extension);

                if (isImage && fileSize > MaxImageSize)
                {
                    return ResultOptions.Fail($"Image file too large. Maximum size: {MaxImageSize / 1024 / 1024}MB", 400);
                }

                if (!isImage && fileSize > MaxFileSize)
                {
                    return ResultOptions.Fail($"File too large. Maximum size: {MaxFileSize / 1024 / 1024}MB", 400);
                }

                // Validate file extension
                var allAllowedExtensions = AllowedImageExtensions.Concat(AllowedFileExtensions).ToArray();
                if (!allAllowedExtensions.Contains(extension))
                {
                    return ResultOptions.Fail(
                        $"Invalid file type. Allowed types: {string.Join(", ", allAllowedExtensions)}", 400);
                }

                // Get user with Google tokens
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return ResultOptions.Fail("User not found", 404);
                }

                // Check if user has Google tokens
                if (string.IsNullOrEmpty(user.GoogleAccessToken))
                {
                    return ResultOptions.Fail("Google Drive not connected. Please login with Google again.", 401);
                }

                // Check if token is expired and refresh if needed
                var accessToken = user.GoogleAccessToken;
                if (user.GoogleTokenExpiresAt.HasValue && user.GoogleTokenExpiresAt.Value <= DateTime.UtcNow)
                {
                    if (string.IsNullOrEmpty(user.GoogleRefreshToken))
                    {
                        return ResultOptions.Fail("Google token expired. Please login with Google again.", 401);
                    }

                    _logger.LogInformation("Refreshing expired Google access token for user {UserId}", userId);
                    var (newToken, expiresAt) = await _googleDriveService.RefreshAccessTokenAsync(user.GoogleRefreshToken);

                    if (string.IsNullOrEmpty(newToken))
                    {
                        return ResultOptions.Fail("Failed to refresh Google token. Please login with Google again.", 401);
                    }

                    // Update user with new token
                    user.GoogleAccessToken = newToken;
                    user.GoogleTokenExpiresAt = expiresAt;
                    await _context.SaveChangesAsync();

                    accessToken = newToken;
                }

                // Upload to Google Drive
                var uniqueFileName = GenerateUniqueFileName(fileName);
                var uploadResult = await _googleDriveService.UploadFileAsync(
                    accessToken,
                    fileStream,
                    uniqueFileName,
                    mimeType,
                    context,
                    contextId);

                if (!uploadResult.Success)
                {
                    _logger.LogError("Google Drive upload failed: {Error}", uploadResult.ErrorMessage);

                    // Check if error is due to insufficient storage
                    var errorMsg = uploadResult.ErrorMessage ?? "";
                    if (errorMsg.Contains("storage", StringComparison.OrdinalIgnoreCase) ||
                        errorMsg.Contains("quota", StringComparison.OrdinalIgnoreCase) ||
                        errorMsg.Contains("space", StringComparison.OrdinalIgnoreCase))
                    {
                        return ResultOptions.Fail(
                            "Không đủ dung lượng Google Drive. Vui lòng dọn dẹp Drive hoặc nâng cấp dung lượng.",
                            507); // 507 Insufficient Storage
                    }

                    return ResultOptions.Fail(uploadResult.ErrorMessage ?? "Upload to Google Drive failed", 500);
                }

                // Create file record in database
                var file = new SuperAppModels.Models.File
                {
                    UserId = userId,
                    Name = fileName,
                    Url = uploadResult.WebViewLink,
                    FileSize = fileSize,
                    MimeType = mimeType,
                    Extension = extension,
                    GoogleDriveFileId = uploadResult.FileId,
                    StatusCode = "active",
                    CreatedAt = VietnamDateTime.Now()
                };

                _context.Files.Add(file);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "File uploaded successfully: ID={FileId}, DriveId={DriveFileId}",
                    file.Id, uploadResult.FileId);

                // Return response based on file type
                if (isImage)
                {
                    return new ResultOptions
                    {
                        Success = true,
                        Message = "Image uploaded successfully",
                        Object = new ImageUploadResponse
                        {
                            Url = uploadResult.WebContentLink ?? uploadResult.WebViewLink ?? "",
                            FileName = file.Name,
                            FileId = file.Id
                        },
                        Status = 200
                    };
                }
                else
                {
                    return new ResultOptions
                    {
                        Success = true,
                        Message = "File uploaded successfully",
                        Object = new AttachmentUploadResponse
                        {
                            Url = uploadResult.WebContentLink ?? uploadResult.WebViewLink ?? "",
                            FileName = uniqueFileName,
                            OriginalName = fileName,
                            Size = fileSize,
                            ContentType = mimeType,
                            FileId = file.Id
                        },
                        Status = 200
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file: {FileName}", fileName);
                return ResultOptions.Fail(ex.Message, 500);
            }
        }

        /// <summary>
        /// Delete a file from storage and database
        /// </summary>
        public async Task<ResultOptions> DeleteFileAsync(int fileId, int userId)
        {
            try
            {
                var file = await _context.Files
                    .FirstOrDefaultAsync(f => f.Id == fileId);

                if (file == null)
                {
                    return ResultOptions.Fail("File not found", 404);
                }

                // Verify ownership
                if (file.UserId != userId)
                {
                    return ResultOptions.Fail("Not authorized to delete this file", 403);
                }

                // Delete from Google Drive if it has a Drive file ID
                if (!string.IsNullOrEmpty(file.GoogleDriveFileId))
                {
                    var user = await _context.Users.FindAsync(userId);
                    if (user != null && !string.IsNullOrEmpty(user.GoogleAccessToken))
                    {
                        var deleted = await _googleDriveService.DeleteFileAsync(user.GoogleAccessToken, file.GoogleDriveFileId);
                        if (!deleted)
                        {
                            _logger.LogWarning(
                                "Failed to delete file from Google Drive: {DriveFileId}. Continuing with database deletion.",
                                file.GoogleDriveFileId);
                        }
                    }
                }

                // Soft delete the file record
                file.DeletedAt = VietnamDateTime.Now();
                file.UpdatedAt = VietnamDateTime.Now();
                await _context.SaveChangesAsync();

                _logger.LogInformation("File deleted: ID={FileId}, DriveId={DriveFileId}", fileId, file.GoogleDriveFileId);

                return new ResultOptions
                {
                    Success = true,
                    Message = "File deleted successfully",
                    Status = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file: {FileId}", fileId);
                return ResultOptions.Fail(ex.Message, 500);
            }
        }

        /// <summary>
        /// Get file by ID
        /// </summary>
        public async Task<ResultOptions> GetFileByIdAsync(int fileId)
        {
            try
            {
                var file = await _context.Files
                    .AsNoTracking()
                    .FirstOrDefaultAsync(f => f.Id == fileId && f.DeletedAt == null);

                if (file == null)
                {
                    return ResultOptions.Fail("File not found", 404);
                }

                return new ResultOptions
                {
                    Success = true,
                    Object = file,
                    Status = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file: {FileId}", fileId);
                return ResultOptions.Fail(ex.Message, 500);
            }
        }

        /// <summary>
        /// Get file content for proxy serving (downloads from Google Drive)
        /// </summary>
        /// <param name="fileId">Database file ID</param>
        /// <param name="userId">User ID for authorization</param>
        /// <returns>File stream with metadata, or null if not found/unauthorized</returns>
        public async Task<DriveFileDownloadResult?> GetFileContentAsync(int fileId, int userId)
        {
            try
            {
                // Get file record
                var file = await _context.Files
                    .AsNoTracking()
                    .FirstOrDefaultAsync(f => f.Id == fileId && f.DeletedAt == null);

                if (file == null)
                {
                    _logger.LogWarning("File not found: {FileId}", fileId);
                    return null;
                }

                // Check authorization - user must own the file
                // TODO: Add more permission checks if needed (e.g., shared files, workspace members)
                if (file.UserId != userId)
                {
                    _logger.LogWarning("User {UserId} not authorized to access file {FileId}", userId, fileId);
                    return null;
                }

                // Get Google Drive file ID
                if (string.IsNullOrEmpty(file.GoogleDriveFileId))
                {
                    _logger.LogWarning("File has no Google Drive ID: {FileId}", fileId);
                    return null;
                }

                // Get user's access token
                var user = await _context.Users.FindAsync(userId);
                if (user == null || string.IsNullOrEmpty(user.GoogleAccessToken))
                {
                    _logger.LogWarning("User has no Google access token: {UserId}", userId);
                    return null;
                }

                // Check if token is expired and refresh if needed
                var accessToken = user.GoogleAccessToken;
                if (user.GoogleTokenExpiresAt.HasValue && user.GoogleTokenExpiresAt.Value <= DateTime.UtcNow)
                {
                    if (string.IsNullOrEmpty(user.GoogleRefreshToken))
                    {
                        _logger.LogWarning("User's Google token expired and no refresh token: {UserId}", userId);
                        return null;
                    }

                    _logger.LogInformation("Refreshing expired Google access token for user {UserId}", userId);
                    var (newToken, expiresAt) = await _googleDriveService.RefreshAccessTokenAsync(user.GoogleRefreshToken);

                    if (string.IsNullOrEmpty(newToken))
                    {
                        _logger.LogWarning("Failed to refresh Google token for user {UserId}", userId);
                        return null;
                    }

                    // Update user with new token
                    user.GoogleAccessToken = newToken;
                    user.GoogleTokenExpiresAt = expiresAt;
                    await _context.SaveChangesAsync();

                    accessToken = newToken;
                }

                // Download from Google Drive
                var downloadResult = await _googleDriveService.DownloadFileAsync(accessToken, file.GoogleDriveFileId);

                if (downloadResult == null)
                {
                    _logger.LogWarning("Failed to download file from Google Drive: {DriveFileId}", file.GoogleDriveFileId);
                    return null;
                }

                return downloadResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file content: {FileId}", fileId);
                return null;
            }
        }

        /// <summary>
        /// Generate a unique file name
        /// </summary>
        private static string GenerateUniqueFileName(string originalFileName)
        {
            var extension = Path.GetExtension(originalFileName);
            var nameWithoutExt = Path.GetFileNameWithoutExtension(originalFileName);
            // Truncate name if too long
            if (nameWithoutExt.Length > 50)
            {
                nameWithoutExt = nameWithoutExt.Substring(0, 50);
            }
            return $"{nameWithoutExt}_{Guid.NewGuid():N}{extension}";
        }
    }
}
