namespace SuperAppModels.Models
{
    /// <summary>
    /// EntityTag - Junction table for many-to-many tagging
    /// Allows tagging of any entity (workspace, folder, note, file) with hashtags
    /// Primary table: entity_tags
    /// </summary>
    public class EntityTag : ITimestampEntity
    {
        // Primary Key
        public long EntityTagId { get; set; } // PRIMARY KEY: entity_tag_id

        // Tag reference
        public int TagId { get; set; } // FOREIGN KEY: tag_id

        // Entity (polymorphic: workspace, folder, note, file)
        public string EntityType { get; set; } = string.Empty; // 'workspace', 'folder', 'note', 'file'
        public int EntityId { get; set; } // ID of the entity being tagged

        // Audit
        public int TaggedBy { get; set; } // FOREIGN KEY: user_id (who added this tag)

        // Timestamps
        public DateTime? CreatedAt { get; set; } // created_at
        public DateTime? UpdatedAt { get; set; } // Always same as CreatedAt (no updates on junction table)
        public DateTime? DeletedAt { get; set; } // deleted_at (soft delete)

        // Navigation properties
        public Tag Tag { get; set; } = null!;
        public User TaggedByUser { get; set; } = null!;

        // Polymorphic navigation properties (only ONE will be populated based on EntityType)
        // These cannot be configured in EF Core, must be loaded manually
        // public Workspace? Workspace { get; set; }
        // public Folder? Folder { get; set; }
        // public Note? Note { get; set; }
        // public File? File { get; set; }

        public EntityTag()
        {
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public EntityTag(int tagId, string entityType, int entityId, int taggedBy) : this()
        {
            TagId = tagId;
            EntityType = entityType ?? throw new ArgumentNullException(nameof(entityType));
            EntityId = entityId;
            TaggedBy = taggedBy;
        }

        /// <summary>
        /// Marks the entity tag as deleted (soft delete)
        /// </summary>
        public void SoftDelete()
        {
            DeletedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Checks if this is a workspace tag
        /// </summary>
        public bool IsWorkspaceTag => EntityType == "workspace";

        /// <summary>
        /// Checks if this is a folder tag
        /// </summary>
        public bool IsFolderTag => EntityType == "folder";

        /// <summary>
        /// Checks if this is a note tag
        /// </summary>
        public bool IsNoteTag => EntityType == "note";

        /// <summary>
        /// Checks if this is a file tag
        /// </summary>
        public bool IsFileTag => EntityType == "file";
    }
}
