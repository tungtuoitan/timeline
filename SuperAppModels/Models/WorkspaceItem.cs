using System.ComponentModel.DataAnnotations;

namespace SuperAppModels.Models
{
    /// <summary>
    /// Polymorphic junction table managing items in workspace (folders, notes, files)
    /// Primary table: ws.workspace_items
    /// Schema: REBUILD_SIMPLIFIED_SCHEMA.sql
    /// </summary>
    public class WorkspaceItem : ITimestampEntity
    {
        // Primary Key
        public long ItemId { get; set; } // maps to: id INT IDENTITY(1,1) PRIMARY KEY

        // Workspace reference
        public int WorkspaceId { get; set; } // maps to: workspace_id INT NOT NULL

        // Parent folder (for hierarchy) - NULLABLE for root items
        public int? FolderId { get; set; } // maps to: folder_id INT (FK to ws.folders.id)

        // Polymorphic item reference
        public string ItemType { get; set; } = string.Empty; // maps to: item_type TINYINT (2=folder, 3=note, 4=file)
        public int ChildId { get; set; } // maps to: item_id INT (actual folder_id/note_id/file_id)

        // Ownership tracking
        public bool IsOriginal { get; set; } = true; // maps to: is_original BIT DEFAULT 1

        // Timestamps (ITimestampEntity)
        public DateTime? CreatedAt { get; set; } // maps to: created_at DATETIME2 DEFAULT GETUTCDATE()
        public DateTime? UpdatedAt { get; set; } // maps to: updated_at DATETIME2
        public DateTime? DeletedAt { get; set; } // maps to: deleted_at DATETIME2

        // Navigation properties
        public Workspace Workspace { get; set; } = null!;
        public Folder? Folder { get; set; } // Parent folder
        
        // Polymorphic navigation (based on ItemType)
        public Folder? ChildFolder { get; set; } // when ItemType = "2"
        public Note? ChildNote { get; set; }     // when ItemType = "3"
        public FileInfo? ChildFile { get; set; } // when ItemType = "4"

        public WorkspaceItem()
        {
            CreatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Marks item as deleted (soft delete)
        /// </summary>
        public void SoftDelete()
        {
            DeletedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Checks if this workspace created/owns the item
        /// </summary>
        public bool IsOwner => IsOriginal;

        /// <summary>
        /// Checks if this item is shared from another workspace
        /// </summary>
        public bool IsShared => !IsOriginal;
    }
}