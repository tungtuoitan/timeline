namespace SuperAppModels.Models
{
    /// <summary>
    /// Polymorphic junction table managing items in workspace (folders, notes, files)
    /// Primary table: ws.workspace_items
    /// Schema: PARENT_ID_REFACTOR_MIGRATION.sql + PathIds migration
    /// </summary>
    public class WorkspaceItemEntity : ITimestampEntity
    {
        // Database columns - EXACTLY match ws.workspace_items schema
        public int Id { get; set; } // id INT IDENTITY(1,1) PRIMARY KEY
        public int WorkspaceId { get; set; } // workspace_id INT NOT NULL
        public int? ParentId { get; set; } // parent_id INT (FK to ws.workspace_items.id - SELF-REFERENCING, NULLABLE for root items)
        public byte EntityType { get; set; } // entity_type TINYINT (2=folder, 3=note, 4=file)
        public int EntityId { get; set; } // entity_id INT (actual folder_id/note_id/file_id from entity tables)

        // Materialized Path columns
        public string PathIds { get; set; } = "/"; // PathIds NVARCHAR(1000) - '/1/5/23/'
        public int PathDepth { get; set; } = 0; // PathDepth INT - 0 for root
        //public string Slug { get; set; } = ""; // Slug NVARCHAR(255) - URL-friendly name

        // Timestamps (ITimestampEntity)
        public DateTime? CreatedAt { get; set; } // created_at DATETIME2 DEFAULT GETUTCDATE()
        public DateTime? UpdatedAt { get; set; } // updated_at DATETIME2
        public DateTime? DeletedAt { get; set; } // deleted_at DATETIME2


        // Navigation properties for EF Core
        public Workspace Workspace { get; set; } = null!;
        public WorkspaceItemEntity? Parent { get; set; } // Parent workspace_item (self-referencing, nullable for root items)

        // Polymorphic navigation (based on EntityType) - NOT MAPPED to database
        // EntityType = 2: Folder
        // EntityType = 3: Note
        // EntityType = 4: File
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public Folder? ChildFolder { get; set; }

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public Note? ChildNote { get; set; }

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public File? ChildFile { get; set; }

        public WorkspaceItemEntity()
        {
            CreatedAt = DateTime.UtcNow;
        }

        public WorkspaceItemEntity(int workspaceId, byte entityType, int entityId, int? parentId = null) : this()
        {
            WorkspaceId = workspaceId;
            EntityType = entityType;
            EntityId = entityId;
            ParentId = parentId;
        }

        /// <summary>
        /// Marks item as deleted (soft delete)
        /// </summary>
        public void SoftDelete()
        {
            DeletedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
