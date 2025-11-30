namespace SuperAppModels.Models
{
    /// <summary>
    /// Polymorphic junction table linking hashtags to entities (workspaces, folders, notes, files)
    /// Primary table: dbo.entity_hashtags
    /// Schema: REBUILD_SIMPLIFIED_SCHEMA.sql
    /// </summary>
    public class EntityHashtag
    {
        // Database columns - EXACTLY match dbo.entity_hashtags schema
        public int Id { get; set; } // id (PRIMARY KEY)
        public byte EntityType { get; set; } // entity_type TINYINT (1=workspace, 2=folder, 3=note, 4=file)
        public int EntityId { get; set; } // entity_id INT (actual workspace_id/folder_id/note_id/file_id)
        public int HashtagId { get; set; } // hashtag_id INT (FOREIGN KEY to dbo.hashtags)

        // Timestamp
        public DateTime? CreatedAt { get; set; } // created_at

        // Navigation properties for EF Core
        public Hashtag Hashtag { get; set; } = null!;

        // Polymorphic navigation (based on EntityType)
        // EntityType = 1: Workspace
        // EntityType = 2: Folder
        // EntityType = 3: Note
        // EntityType = 4: File
        public Workspace? EntityWorkspace { get; set; }
        public Folder? EntityFolder { get; set; }
        public Note? EntityNote { get; set; }
        public File? EntityFile { get; set; }

        public EntityHashtag()
        {
            CreatedAt = DateTime.UtcNow;
        }

        public EntityHashtag(byte entityType, int entityId, int hashtagId) : this()
        {
            EntityType = entityType;
            EntityId = entityId;
            HashtagId = hashtagId;
        }
    }
}
