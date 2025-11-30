namespace SuperAppModels.Models;

/// <summary>
/// Entity model representing a file/document in the system
/// Maps to the 'files' table in ws schema - EXACTLY matches REBUILD_SIMPLIFIED_SCHEMA.sql
/// Files are always leaf nodes in the workspace tree (cannot have children)
/// </summary>
public class FileInfo : ITimestampEntity
{
    // Database columns - EXACTLY match ws.files schema from REBUILD_SIMPLIFIED_SCHEMA.sql
    
    /// <summary>
    /// Primary key: id (IDENTITY)
    /// </summary>
    public int FileId { get; set; }

    /// <summary>
    /// Foreign key to users table: user_id
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Display name of the file (255 chars, required)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// URL to the file (1000 chars)
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// File size in bytes (nullable)
    /// </summary>
    public long? FileSize { get; set; }

    /// <summary>
    /// MIME type (e.g., "application/pdf", "image/png") - 100 chars
    /// </summary>
    public string? MimeType { get; set; }

    /// <summary>
    /// File extension (e.g., ".pdf", ".docx", ".png") - 20 chars
    /// </summary>
    public string? Extension { get; set; }

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
    }

    /// <summary>
    /// Constructor with required fields
    /// </summary>
    public FileInfo(string name, int userId) : this()
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        UserId = userId;
    }

    /// <summary>
    /// Updates file metadata
    /// </summary>
    public void Update(string? name = null, string? url = null, long? fileSize = null, string? mimeType = null, string? extension = null)
    {
        if (name != null) Name = name;
        if (url != null) Url = url;
        if (fileSize != null) FileSize = fileSize;
        if (mimeType != null) MimeType = mimeType;
        if (extension != null) Extension = extension;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the file as deleted with soft delete pattern
    /// </summary>
    public void SoftDelete()
    {
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Restores a soft-deleted file
    /// </summary>
    public void Restore()
    {
        DeletedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }
}
