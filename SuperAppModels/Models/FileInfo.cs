namespace SuperAppModels.Models;

/// <summary>
/// Entity model representing a file/document in the system
/// Maps to the 'files' table in database
/// Files are always leaf nodes in the workspace tree (cannot have children)
/// </summary>
public class FileInfo : ITimestampEntity
{
    // Database columns - must match exactly with 'files' table schema
    
    /// <summary>
    /// Primary key: file_id (IDENTITY)
    /// </summary>
    public int FileId { get; set; }

    /// <summary>
    /// Foreign key to users table: user_id
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Display name of the file (editable by user)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Original filename when uploaded (e.g., "Q4_Report.pdf")
    /// </summary>
    public string OriginalFilename { get; set; } = string.Empty;

    /// <summary>
    /// Path to the file in storage system
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// MIME type (e.g., "application/pdf", "image/png")
    /// </summary>
    public string MimeType { get; set; } = string.Empty;

    /// <summary>
    /// File extension (e.g., ".pdf", ".docx", ".png")
    /// </summary>
    public string Extension { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the file
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// URL-friendly slug (auto-generated from name by trigger)
    /// </summary>
    public string? Slug { get; set; }

    /// <summary>
    /// Whether the file is publicly accessible
    /// </summary>
    public bool IsPublic { get; set; } = false;

    /// <summary>
    /// Whether the file is archived
    /// </summary>
    public bool IsArchived { get; set; } = false;

    /// <summary>
    /// Whether the file is pinned for quick access
    /// </summary>
    public bool IsPinned { get; set; } = false;

    /// <summary>
    /// Whether the file is marked as favorite
    /// </summary>
    public bool IsFavorite { get; set; } = false;

    /// <summary>
    /// Number of times the file has been downloaded
    /// </summary>
    public int DownloadCount { get; set; } = 0;

    /// <summary>
    /// When the file was last downloaded
    /// </summary>
    public DateTime? LastDownloadedAt { get; set; }

    // Timestamps (ITimestampEntity)
    
    /// <summary>
    /// When the file was uploaded
    /// </summary>
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// When the file metadata was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Soft delete timestamp (null = active)
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    // Navigation properties for EF Core

    /// <summary>
    /// User who owns this file
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// Default constructor - sets initial values
    /// </summary>
    public FileInfo()
    {
        CreatedAt = DateTime.UtcNow;
        IsPublic = false;
        IsArchived = false;
        DownloadCount = 0;
    }

    /// <summary>
    /// Constructor with required fields
    /// </summary>
    public FileInfo(string name, string originalFilename, string filePath, long fileSize, string mimeType, string extension, int userId) : this()
    {
        Name = name;
        OriginalFilename = originalFilename;
        FilePath = filePath;
        FileSize = fileSize;
        MimeType = mimeType;
        Extension = extension;
        UserId = userId;
    }

    /// <summary>
    /// Format file size to human-readable string (e.g., "2.5 MB")
    /// </summary>
    public string GetFormattedFileSize()
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = FileSize;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    /// <summary>
    /// Get file icon based on extension/mime type
    /// </summary>
    public string GetFileIcon()
    {
        return Extension.ToLower() switch
        {
            ".pdf" => "file-pdf",
            ".doc" or ".docx" => "file-word",
            ".xls" or ".xlsx" => "file-excel",
            ".ppt" or ".pptx" => "file-powerpoint",
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".svg" => "file-image",
            ".zip" or ".rar" or ".7z" => "file-archive",
            ".txt" => "file-text",
            ".mp4" or ".avi" or ".mov" => "file-video",
            ".mp3" or ".wav" or ".flac" => "file-audio",
            _ => "file"
        };
    }

    /// <summary>
    /// Check if file is an image
    /// </summary>
    public bool IsImage()
    {
        return MimeType.StartsWith("image/");
    }

    /// <summary>
    /// Check if file is a document
    /// </summary>
    public bool IsDocument()
    {
        string[] documentMimes = {
            "application/pdf",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "application/vnd.ms-excel",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "application/vnd.ms-powerpoint",
            "application/vnd.openxmlformats-officedocument.presentationml.presentation"
        };
        return documentMimes.Contains(MimeType);
    }
}
