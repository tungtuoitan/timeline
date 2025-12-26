namespace SuperAppModels.Models
{
    /// <summary>
    /// File attachment entity
    /// Primary table: ws.files
    /// Schema: REBUILD_SIMPLIFIED_SCHEMA.sql
    /// </summary>
    public class File : ITimestampEntity
    {
        // Database columns - EXACTLY match ws.files schema
        public int Id { get; set; } // id (PRIMARY KEY)
        public int UserId { get; set; } // user_id (FOREIGN KEY)

        // File info
        public string Name { get; set; } = string.Empty; // name (255 chars, required)
        public string? Url { get; set; } // url (1000 chars)
        public long? FileSize { get; set; } // file_size (BIGINT)
        public string? MimeType { get; set; } // mime_type (100 chars)
        public string? Extension { get; set; } // extension (20 chars)

        // Timestamps (ITimestampEntity)
        public DateTime? CreatedAt { get; set; } // created_at
        public DateTime? UpdatedAt { get; set; } // updated_at
        public DateTime? DeletedAt { get; set; } // deleted_at (soft delete)

        // Copy tracking (for future copy feature)
        public string? CopyInfo { get; set; } // copy_info NVARCHAR(MAX) - JSON metadata

        // Navigation properties for EF Core
        public User User { get; set; } = null!;
        public ICollection<WorkspaceItem> WorkspaceItems { get; set; } = new List<WorkspaceItem>();

        public File()
        {
            CreatedAt = DateTime.UtcNow;
        }

        public File(string name, int userId) : this()
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            UserId = userId;
        }

        /// <summary>
        /// Updates the file metadata
        /// </summary>
        public void Update(string name, string? url = null, long? fileSize = null, string? mimeType = null, string? extension = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be empty", nameof(name));

            Name = name;
            if (url != null) Url = url;
            if (fileSize.HasValue) FileSize = fileSize;
            if (mimeType != null) MimeType = mimeType;
            if (extension != null) Extension = extension;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Marks the file as deleted (soft delete)
        /// </summary>
        public void SoftDelete()
        {
            DeletedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets human-readable file size
        /// </summary>
        public string GetFileSizeFormatted()
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
