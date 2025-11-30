namespace SuperAppModels.Models
{
    /// <summary>
    /// Polymorphic junction table managing items in workspace (folders, notes, files)
    /// Primary table: ws.workspace_items
    /// Schema: REBUILD_SIMPLIFIED_SCHEMA.sql
    /// </summary>
    public class WorkspaceItem : ITimestampEntity
    {
        // Database columns - EXACTLY match ws.workspace_items schema
        public int Id { get; set; } // id INT IDENTITY(1,1) PRIMARY KEY
        public int WorkspaceId { get; set; } // workspace_id INT NOT NULL
        public int? FolderId { get; set; } // folder_id INT (FK to ws.folders.id, NULLABLE for root items)
        public byte ItemType { get; set; } // item_type TINYINT (2=folder, 3=note, 4=file)
        public int ItemId { get; set; } // item_id INT (actual folder_id/note_id/file_id)
        public bool IsOriginal { get; set; } = true; // is_original BIT DEFAULT 1

        // Timestamps (ITimestampEntity)
        public DateTime? CreatedAt { get; set; } // created_at DATETIME2 DEFAULT GETUTCDATE()
        public DateTime? UpdatedAt { get; set; } // updated_at DATETIME2
        public DateTime? DeletedAt { get; set; } // deleted_at DATETIME2

        // Navigation properties for EF Core
        public Workspace Workspace { get; set; } = null!;
        public Folder? Folder { get; set; } // Parent folder (nullable for root items)

        // Polymorphic navigation (based on ItemType)
        // ItemType = 2: Folder
        // ItemType = 3: Note
        // ItemType = 4: File
        public Folder? ChildFolder { get; set; }
        public Note? ChildNote { get; set; }
        public File? ChildFile { get; set; }

        public WorkspaceItem()
        {
            CreatedAt = DateTime.UtcNow;
        }

        public WorkspaceItem(int workspaceId, byte itemType, int itemId, int? folderId = null) : this()
        {
            WorkspaceId = workspaceId;
            ItemType = itemType;
            ItemId = itemId;
            FolderId = folderId;
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
