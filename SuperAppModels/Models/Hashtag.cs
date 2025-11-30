namespace SuperAppModels.Models
{
    /// <summary>
    /// Hashtag entity for tagging workspaces, folders, notes, and files
    /// Primary table: dbo.hashtags
    /// Schema: REBUILD_SIMPLIFIED_SCHEMA.sql
    /// </summary>
    public class Hashtag : ITimestampEntity
    {
        // Database columns - EXACTLY match dbo.hashtags schema
        public int Id { get; set; } // id (PRIMARY KEY)
        public int UserId { get; set; } // user_id (FOREIGN KEY)

        // Hashtag info
        public string Name { get; set; } = string.Empty; // name (100 chars, required)
        public int UsageCount { get; set; } = 0; // usage_count (tracks how many times used)

        // Timestamps (ITimestampEntity)
        public DateTime? CreatedAt { get; set; } // created_at
        public DateTime? UpdatedAt { get; set; } // updated_at
        public DateTime? DeletedAt { get; set; } // deleted_at (soft delete)

        // Navigation properties for EF Core
        public User User { get; set; } = null!;
        public ICollection<EntityHashtag> EntityHashtags { get; set; } = new List<EntityHashtag>();

        public Hashtag()
        {
            CreatedAt = DateTime.UtcNow;
            UsageCount = 0;
        }

        public Hashtag(string name, int userId) : this()
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            UserId = userId;
        }

        /// <summary>
        /// Increments usage count when hashtag is applied to an entity
        /// </summary>
        public void IncrementUsage()
        {
            UsageCount++;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Decrements usage count when hashtag is removed from an entity
        /// </summary>
        public void DecrementUsage()
        {
            if (UsageCount > 0)
            {
                UsageCount--;
                UpdatedAt = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Marks the hashtag as deleted (soft delete)
        /// </summary>
        public void SoftDelete()
        {
            DeletedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
