namespace SuperAppModels.DTOs.Responses
{
    /// <summary>
    /// Response for image upload
    /// </summary>
    public class ImageUploadResponse
    {
        public string Url { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public int FileId { get; set; }
    }

    /// <summary>
    /// Response for file attachment upload
    /// </summary>
    public class AttachmentUploadResponse
    {
        public string Url { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string OriginalName { get; set; } = string.Empty;
        public long Size { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public int FileId { get; set; }
    }

    /// <summary>
    /// Google Drive upload result
    /// </summary>
    public class DriveUploadResult
    {
        public bool Success { get; set; }
        public string? FileId { get; set; } // Google Drive file ID
        public string? WebViewLink { get; set; } // URL to view the file
        public string? WebContentLink { get; set; } // URL to download the file
        public long FileSize { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Google Drive quota info
    /// </summary>
    public class DriveQuotaInfo
    {
        public long TotalBytes { get; set; }
        public long UsedBytes { get; set; }
        public long AvailableBytes => TotalBytes - UsedBytes;
    }

    /// <summary>
    /// Google Drive file info
    /// </summary>
    public class DriveFileInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty;
        public long Size { get; set; }
        public string? WebViewLink { get; set; }
        public string? WebContentLink { get; set; }
        public DateTime? CreatedTime { get; set; }
        public DateTime? ModifiedTime { get; set; }
    }

    /// <summary>
    /// Result of downloading file from Google Drive (for proxy serving)
    /// </summary>
    public class DriveFileDownloadResult : IDisposable
    {
        public Stream Stream { get; set; } = null!;
        public string FileName { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty;
        public long FileSize { get; set; }

        public void Dispose()
        {
            Stream?.Dispose();
        }
    }
}
