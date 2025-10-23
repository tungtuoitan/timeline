namespace SuperAppModels.DTOs.Responses;

/// <summary>
/// File-specific metadata for WorkspaceTreeItemResponse
/// Contains additional information specific to file/document items
/// Files are always leaf nodes and cannot have children
/// </summary>
public class FileMetadata
{
    /// <summary>
    /// Original filename when uploaded (e.g., "report.pdf")
    /// </summary>
    public string OriginalFilename { get; set; } = string.Empty;

    /// <summary>
    /// File extension (e.g., ".pdf", ".docx", ".png")
    /// </summary>
    public string Extension { get; set; } = string.Empty;

    /// <summary>
    /// MIME type of the file (e.g., "application/pdf", "image/png")
    /// </summary>
    public string MimeType { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Human-readable file size (e.g., "2.5 MB", "150 KB")
    /// </summary>
    public string FileSizeFormatted { get; set; } = string.Empty;

    /// <summary>
    /// Path to the file in storage system
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the file
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether the file is publicly accessible
    /// </summary>
    public bool IsPublic { get; set; }

    /// <summary>
    /// File size in bytes (same as FileSize, for backward compatibility)
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// Storage path (same as FilePath, for backward compatibility)
    /// </summary>
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>
    /// Blob storage URL (cloud storage)
    /// </summary>
    public string? BlobUrl { get; set; }

    /// <summary>
    /// Blob container name (cloud storage)
    /// </summary>
    public string? BlobContainerName { get; set; }

    /// <summary>
    /// When the file was uploaded
    /// </summary>
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// When the file metadata was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Whether the file is archived
    /// </summary>
    public bool IsArchived { get; set; }

    /// <summary>
    /// Number of times the file has been downloaded
    /// </summary>
    public int DownloadCount { get; set; }

    /// <summary>
    /// When the file was last downloaded
    /// </summary>
    public DateTime? LastDownloadedAt { get; set; }

    /// <summary>
    /// Thumbnail URL for image/document preview (if available)
    /// </summary>
    public string? ThumbnailUrl { get; set; }
}
