namespace SuperAppModels.Models
{
    public class Note : ITimestampEntity
    {
        // Database columns - EXACTLY match dbo.notes schema from REBUILD_SIMPLIFIED_SCHEMA.sql
        public int Id { get; set; } // id (PRIMARY KEY)
        public int UserId { get; set; } // user_id (FOREIGN KEY)
        
        // Content - SIMPLIFIED
        public string Name { get; set; } = string.Empty; // name (255 chars, required)
        public string? Description { get; set; } // description (NVARCHAR(MAX))

        // Status (no FK reference)
        public string? StatusCode { get; set; } // status_code (simple string, no FK)

        // Timestamps (ITimestampEntity)
        public DateTime? CreatedAt { get; set; } // created_at
        public DateTime? UpdatedAt { get; set; } // updated_at
        public DateTime? DeletedAt { get; set; } // deleted_at (soft delete)

        // Copy tracking (for future copy feature)
        public string? CopyInfo { get; set; } // copy_info NVARCHAR(MAX) - JSON metadata

        // Navigation properties for EF Core
        public User User { get; set; } = null!;

        public Note()
        {
            CreatedAt = DateTime.UtcNow;
        }

        public Note(string name, int userId) : this()
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            UserId = userId;
        }

        /// <summary>
        /// Updates the note content with validation and automatic timestamp management
        /// </summary>
        public void Update(string name, string? description = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be empty", nameof(name));

            Name = name;
            Description = description;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Marks the note as deleted with soft delete pattern
        /// </summary>
        public void SoftDelete()
        {
            DeletedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}