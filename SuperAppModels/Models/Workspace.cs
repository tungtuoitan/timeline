namespace SuperAppModels.Models
{
    /// <summary>
    /// User workspace for organizing hierarchical content
    /// Primary table: ws.workspaces
    /// Schema: REBUILD_SIMPLIFIED_SCHEMA.sql
    /// </summary>
    public class Workspace : ITimestampEntity
    {
        // Database columns - EXACTLY match ws.workspaces schema
        public int Id { get; set; } // id (PRIMARY KEY)
        public int UserId { get; set; } // user_id (FOREIGN KEY)

        // Workspace info
        public string Name { get; set; } = string.Empty; // name (255 chars, required)
        public string? Description { get; set; } // description (1000 chars)

        // Status (no FK reference)
        public string? StatusCode { get; set; } // status_code (simple string, no FK)

        // Timestamps (ITimestampEntity)
        public DateTime? CreatedAt { get; set; } // created_at
        public DateTime? UpdatedAt { get; set; } // updated_at
        public DateTime? DeletedAt { get; set; } // deleted_at (soft delete)


        // Navigation properties for EF Core
        public User User { get; set; } = null!;
        public ICollection<WorkspaceItemEntity> Items { get; set; } = new List<WorkspaceItemEntity>();

        public Workspace()
        {
            CreatedAt = DateTime.UtcNow;
        }

        public Workspace(string name, int userId) : this()
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            UserId = userId;
        }

        /// <summary>
        /// Updates workspace information
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
        /// Marks workspace as deleted (soft delete)
        /// </summary>
        public void SoftDelete()
        {
            DeletedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
