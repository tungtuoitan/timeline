namespace SuperAppModels.DTOs.Responses;

/// <summary>
/// File entity data - full data from files table
/// Used in WorkspaceItemResponseV2 as the Data property for files
/// </summary>
public class FileData
{
    /// <summary>File ID (files.id)</summary>
    public int Id { get; set; }

    /// <summary>User ID who owns the file (files.user_id)</summary>
    public int UserId { get; set; }

    /// <summary>File name (files.name)</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>File URL (files.url)</summary>
    public string? Url { get; set; }

    /// <summary>File size in bytes (files.file_size)</summary>
    public long? FileSize { get; set; }

    /// <summary>MIME type (files.mime_type)</summary>
    public string? MimeType { get; set; }

    /// <summary>File extension (files.extension)</summary>
    public string? Extension { get; set; }

    /// <summary>Status code (files.status_code)</summary>
    public string? StatusCode { get; set; }

    /// <summary>Created timestamp (files.created_at)</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Updated timestamp (files.updated_at)</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Soft delete timestamp (files.deleted_at)</summary>
    public DateTime? DeletedAt { get; set; }


    /// <summary>Human-readable file size</summary>
    public string FileSizeFormatted
    {
        get
        {
            if (!FileSize.HasValue || FileSize == 0) return "0 B";

            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            var order = 0;
            var size = (double)FileSize.Value;

            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }

            return $"{size:0.##} {sizes[order]}";
        }
    }
}
