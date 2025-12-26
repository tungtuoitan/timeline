namespace SuperAppModels.Models
{
    /// <summary>
    /// Folder entity (renamed from Tag)
    /// Folders are containers for organizing items in workspaces
    /// Can be shared across multiple workspaces
    /// </summary>
    public class Folder : ITimestampEntity
    {
        // Database columns - EXACTLY match ws.folders schema from REBUILD_SIMPLIFIED_SCHEMA.sql
        public int Id { get; set; } // id (PRIMARY KEY)
        public int UserId { get; set; } // user_id (FOREIGN KEY)

        // Folder info
        public string Name { get; set; } = string.Empty; // name (255 chars, required)
        public string? Description { get; set; } // description (NVARCHAR(MAX))
        public string? Color { get; set; } = "#F59E0B"; // color (hex format, default amber)
        public string? Icon { get; set; } = "📁"; // icon (default folder emoji)

        // Timestamps (ITimestampEntity)
        public DateTime? CreatedAt { get; set; } // created_at
        public DateTime? UpdatedAt { get; set; } // updated_at
        public DateTime? DeletedAt { get; set; } // deleted_at (soft delete)

        // Copy tracking (for future copy feature)
        public string? CopyInfo { get; set; } // copy_info NVARCHAR(MAX) - JSON metadata

        // Navigation properties for EF Core
        public User User { get; set; } = null!;
        public ICollection<WorkspaceItemEntity> WorkspaceItems { get; set; } = new List<WorkspaceItemEntity>();

        public Folder()
        {
            CreatedAt = DateTime.UtcNow;
            Color = "#F59E0B"; // Default amber color
            Icon = "📁"; // Default folder emoji
        }

        public Folder(string name, int userId) : this()
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            UserId = userId;
        }

        /// <summary>
        /// Updates the folder content with validation and automatic timestamp management
        /// </summary>
        public void Update(string name, string? description = null, string? color = null, string? icon = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be empty", nameof(name));

            Name = name;
            Description = description;
            if (color != null) Color = color;
            if (icon != null) Icon = icon;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Marks the folder as deleted with soft delete pattern
        /// </summary>
        public void SoftDelete()
        {
            DeletedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Restores a soft-deleted folder
        /// </summary>
        private static string GenerateSlug(string name)
        {
            return name.ToLowerInvariant()
                .Replace(" ", "-")
                .Replace("_", "-")
                // Remove special characters
                .Where(c => char.IsLetterOrDigit(c) || c == '-')
                .Aggregate(new System.Text.StringBuilder(), (sb, c) => sb.Append(c))
                .ToString();
        }
    }
}
