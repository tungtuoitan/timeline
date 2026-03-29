using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperAppAPI.Extensions;
using SuperAppModels.DTOs;
using SuperAppServices.Interfaces;

namespace SuperAppAPI.Controllers
{
    /// <summary>
    /// Controller for file upload operations
    /// Handles image and file attachments for rich text editors
    /// Files are uploaded to Google Drive with automatic store selection
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FileController : ControllerBase
    {
        private readonly ILogger<FileController> _logger;
        private readonly IFileService _fileService;

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

        public FileController(
            ILogger<FileController> logger,
            IFileService fileService)
        {
            _logger = logger;
            _fileService = fileService;
        }

        /// <summary>
        /// Upload an image file (for rich text editor)
        /// POST /api/file/image
        /// </summary>
        /// <param name="file">Image file to upload</param>
        /// <param name="context">Context type: "project" or "workspace"</param>
        /// <param name="contextId">Context ID (project_id or workspace_id)</param>
        /// <returns>URL of the uploaded image</returns>
        [HttpPost("image")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status507InsufficientStorage)]
        [RequestSizeLimit(MaxImageSize)]
        public async Task<IActionResult> UploadImage(
            IFormFile file,
            [FromForm] string? context = null,
            [FromForm] int contextId = 0)
        {
            try
            {
                var userIdStr = User.GetUserId();
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId) || userId == 0)
                {
                    return Unauthorized(new ResultOptions { Success = false, Message = "Invalid user token" });
                }

                if (file == null || file.Length == 0)
                {
                    return BadRequest(new ResultOptions { Success = false, Message = "No file provided" });
                }

                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!AllowedImageExtensions.Contains(extension))
                {
                    return BadRequest(new ResultOptions
                    {
                        Success = false,
                        Message = $"Invalid file type. Allowed types: {string.Join(", ", AllowedImageExtensions)}"
                    });
                }

                if (file.Length > MaxImageSize)
                {
                    return BadRequest(new ResultOptions
                    {
                        Success = false,
                        Message = $"File too large. Maximum size: {MaxImageSize / 1024 / 1024}MB"
                    });
                }

                // Upload via FileService
                using var stream = file.OpenReadStream();
                var result = await _fileService.UploadFileAsync(
                    userId,
                    stream,
                    file.FileName,
                    file.ContentType,
                    context ?? "general",
                    contextId);

                if (!result.Success)
                {
                    return StatusCode(result.Status ?? 500, result);
                }

                _logger.LogInformation("Image uploaded successfully: {FileName}", file.FileName);

                // Return in expected format for frontend
                return Ok(new
                {
                    success = true,
                    message = "Image uploaded successfully",
                    data = result.Object
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image");
                return StatusCode(500, new ResultOptions { Success = false, Message = "Failed to upload image" });
            }
        }

        /// <summary>
        /// Upload a file attachment (for rich text editor)
        /// POST /api/file/attachment
        /// </summary>
        /// <param name="file">File to upload</param>
        /// <param name="context">Context type: "project" or "workspace"</param>
        /// <param name="contextId">Context ID (project_id or workspace_id)</param>
        /// <returns>URL and metadata of the uploaded file</returns>
        [HttpPost("attachment")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status507InsufficientStorage)]
        [RequestSizeLimit(MaxFileSize)]
        public async Task<IActionResult> UploadAttachment(
            IFormFile file,
            [FromForm] string? context = null,
            [FromForm] int contextId = 0)
        {
            try
            {
                var userIdStr = User.GetUserId();
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId) || userId == 0)
                {
                    return Unauthorized(new ResultOptions { Success = false, Message = "Invalid user token" });
                }

                if (file == null || file.Length == 0)
                {
                    return BadRequest(new ResultOptions { Success = false, Message = "No file provided" });
                }

                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                var allAllowedExtensions = AllowedImageExtensions.Concat(AllowedFileExtensions).ToArray();

                if (!allAllowedExtensions.Contains(extension))
                {
                    return BadRequest(new ResultOptions
                    {
                        Success = false,
                        Message = $"Invalid file type. Allowed types: {string.Join(", ", allAllowedExtensions)}"
                    });
                }

                if (file.Length > MaxFileSize)
                {
                    return BadRequest(new ResultOptions
                    {
                        Success = false,
                        Message = $"File too large. Maximum size: {MaxFileSize / 1024 / 1024}MB"
                    });
                }

                // Upload via FileService
                using var stream = file.OpenReadStream();
                var result = await _fileService.UploadFileAsync(
                    userId,
                    stream,
                    file.FileName,
                    file.ContentType,
                    context ?? "general",
                    contextId);

                if (!result.Success)
                {
                    return StatusCode(result.Status ?? 500, result);
                }

                _logger.LogInformation("Attachment uploaded successfully: {FileName}", file.FileName);

                // Return in expected format for frontend
                return Ok(new
                {
                    success = true,
                    message = "File uploaded successfully",
                    data = result.Object
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading attachment");
                return StatusCode(500, new ResultOptions { Success = false, Message = "Failed to upload file" });
            }
        }

        /// <summary>
        /// Delete a file
        /// DELETE /api/file/{id}
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteFile(int id)
        {
            try
            {
                var userIdStr = User.GetUserId();
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId) || userId == 0)
                {
                    return Unauthorized(new ResultOptions { Success = false, Message = "Invalid user token" });
                }

                var result = await _fileService.DeleteFileAsync(id, userId);

                if (!result.Success)
                {
                    return StatusCode(result.Status ?? 500, result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file: {FileId}", id);
                return StatusCode(500, new ResultOptions { Success = false, Message = "Failed to delete file" });
            }
        }

        /// <summary>
        /// Get file by ID
        /// GET /api/file/{id}
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetFile(int id)
        {
            try
            {
                var result = await _fileService.GetFileByIdAsync(id);

                if (!result.Success)
                {
                    return StatusCode(result.Status ?? 500, result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file: {FileId}", id);
                return StatusCode(500, new ResultOptions { Success = false, Message = "Failed to get file" });
            }
        }

        /// <summary>
        /// Proxy endpoint to serve file content from Google Drive
        /// GET /api/file/{id}/content
        /// Returns the actual file content (image, PDF, etc.)
        /// Requires JWT Authorization header
        /// </summary>
        [HttpGet("{id}/content")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetFileContent(int id)
        {
            try
            {
                var userIdStr = User.GetUserId();
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId) || userId == 0)
                {
                    return Unauthorized(new ResultOptions { Success = false, Message = "Invalid user token" });
                }

                var downloadResult = await _fileService.GetFileContentAsync(id, userId);

                if (downloadResult == null)
                {
                    return NotFound(new ResultOptions { Success = false, Message = "File not found or access denied" });
                }

                // Return file stream with appropriate headers
                Response.Headers.Append("Cache-Control", "private, max-age=3600");

                return File(downloadResult.Stream, downloadResult.MimeType, downloadResult.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error serving file content: {FileId}", id);
                return StatusCode(500, new ResultOptions { Success = false, Message = "Failed to serve file" });
            }
        }
    }
}
