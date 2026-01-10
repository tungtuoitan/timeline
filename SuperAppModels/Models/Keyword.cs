namespace SuperAppModels.Models
{
    /// <summary>
    /// Keyword for markdown editor
    /// Stores all keywords (workspace/folder/note/heading/external) with PathIds design
    /// Primary table: dbo.Keywords
    /// </summary>
    public class Keyword : ITimestampEntity
    {
        // Database columns
        public int Id { get; set; }
        public int UserId { get; set; }

        // Keyword info
        public string Name { get; set; } = string.Empty;
        public int NameIndex { get; set; } = 1;
        public string Type { get; set; } = string.Empty; // external/workspace/folder/note/file/h1-h6

        // Polymorphic references (only 1 should have value)
        public int? WorkspaceId { get; set; } // FK to workspaces (for workspace type only)
        public int? TargetItemId { get; set; } // FK to workspace_items (for folder/note/file)
        public int? NoteItemId { get; set; } // FK to workspace_items (parent note for headings)
        public string? ExternalUrl { get; set; } // Full URL for external type

        // Path components
        public string? PathIds { get; set; } // Copy from workspace_items (NULL for heading/external)
        public string? HeadingPath { get; set; } // 'h1-Intro/h2-Setup' (heading only)

        // Cached Link (NO LongLink - render runtime)
        public string Link { get; set; } = string.Empty;

        public string? Description { get; set; }

        // Timestamps
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? HardDeletedAt { get; set; } // Soft delete for headings when removed from note

        // Navigation properties
        public User User { get; set; } = null!;
        public Workspace? Workspace { get; set; }
        public WorkspaceItemEntity? TargetItem { get; set; }
        public WorkspaceItemEntity? NoteItem { get; set; }

        public Keyword()
        {
            CreatedAt = DateTime.UtcNow;
        }

        public Keyword(
            string name,
            int nameIndex,
            string link,
            string type,
            int userId,
            int? workspaceId = null,
            int? targetItemId = null,
            int? noteItemId = null,
            string? externalUrl = null,
            string? pathIds = null,
            string? headingPath = null) : this()
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Link = link ?? throw new ArgumentNullException(nameof(link));
            Type = type ?? throw new ArgumentNullException(nameof(type));
            NameIndex = nameIndex;
            UserId = userId;
            WorkspaceId = workspaceId;
            TargetItemId = targetItemId;
            NoteItemId = noteItemId;
            ExternalUrl = externalUrl;
            PathIds = pathIds;
            HeadingPath = headingPath;
        }

        /// <summary>
        /// Updates the keyword with validation and automatic timestamp management
        /// </summary>
        public void Update(
            string name,
            int nameIndex,
            string link,
            string type,
            string? description = null,
            int? workspaceId = null,
            int? targetItemId = null,
            int? noteItemId = null,
            string? externalUrl = null,
            string? pathIds = null,
            string? headingPath = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be empty", nameof(name));
            if (string.IsNullOrWhiteSpace(link))
                throw new ArgumentException("Link cannot be empty", nameof(link));
            if (string.IsNullOrWhiteSpace(type))
                throw new ArgumentException("Type cannot be empty", nameof(type));

            Name = name;
            NameIndex = nameIndex;
            Link = link;
            Type = type;
            Description = description;
            WorkspaceId = workspaceId;
            TargetItemId = targetItemId;
            NoteItemId = noteItemId;
            ExternalUrl = externalUrl;
            PathIds = pathIds;
            HeadingPath = headingPath;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
