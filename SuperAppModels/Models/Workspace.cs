using System.ComponentModel.DataAnnotations;

namespace SuperAppModels.Models
{
    /// <summary>
    /// User workspace for organizing hierarchical content
    /// Primary table: workspaces
    /// </summary>
    public class Workspace : ITimestampEntity
    {
        // Primary Key - workspace_id
        public int WorkspaceId { get; set; }

        // Owner
        public int UserId { get; set; }

        // Workspace info
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        // Visual properties
        public string? Color { get; set; }
        public string? Icon { get; set; }

        // Workspace type & behavior
        public string Type { get; set; } = "hierarchy";

        // Settings
        public int MaxDepth { get; set; } = 10;

        // Flags
        public bool IsDefault { get; set; } = false;
        public bool IsPublic { get; set; } = false;
        public bool IsTemplate { get; set; } = false;
        public bool IsArchived { get; set; } = false;

        // Statistics (updated by triggers)
        public int TagCount { get; set; } = 0;
        public int RelationshipCount { get; set; } = 0;
        public int MemberCount { get; set; } = 1;

        // Metadata
        public string? Settings { get; set; } // JSON

        // Timestamps (ITimestampEntity)
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastAccessedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        // Navigation properties
        public User User { get; set; } = null!;
        public ICollection<WorkspaceMember> Members { get; set; } = new List<WorkspaceMember>();
        public ICollection<WorkspaceItem> Items { get; set; } = new List<WorkspaceItem>();
        public ICollection<WorkspaceRelationshipType> RelationshipTypes { get; set; } = new List<WorkspaceRelationshipType>();

        public Workspace()
        {
            CreatedAt = DateTime.UtcNow;
            LastAccessedAt = DateTime.UtcNow;
        }

        public Workspace(string name, int userId, string type = "hierarchy") : this()
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            UserId = userId;
            Type = type;
        }

        /// <summary>
        /// Updates workspace information
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
        /// Updates last accessed timestamp
        /// </summary>
        public void UpdateLastAccessed()
        {
            LastAccessedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Archives the workspace
        /// </summary>
        public void Archive()
        {
            IsArchived = true;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Restores the workspace from archive
        /// </summary>
        public void Restore()
        {
            IsArchived = false;
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