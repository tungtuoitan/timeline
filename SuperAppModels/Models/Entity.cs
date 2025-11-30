namespace SuperAppModels.Models
{
    /// <summary>
    /// Entity lookup table (workspace, folder, note, file)
    /// Primary table: dbo.entities
    /// Schema: REBUILD_SIMPLIFIED_SCHEMA.sql
    /// This is a read-only lookup table with fixed values
    /// </summary>
    public class Entity
    {
        // Database columns - EXACTLY match dbo.entities schema
        public byte Id { get; set; } // id TINYINT PRIMARY KEY (1=workspace, 2=folder, 3=note, 4=file)
        public string Name { get; set; } = string.Empty; // name (50 chars, UNIQUE)
        public string? Description { get; set; } // description (255 chars)
        public DateTime? CreatedAt { get; set; } // created_at

        // Constants for entity types
        public const byte WORKSPACE = 1;
        public const byte FOLDER = 2;
        public const byte NOTE = 3;
        public const byte FILE = 4;

        public Entity()
        {
            CreatedAt = DateTime.UtcNow;
        }

        public Entity(byte id, string name, string? description = null) : this()
        {
            Id = id;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description;
        }

        /// <summary>
        /// Gets entity name by ID
        /// </summary>
        public static string GetName(byte id)
        {
            return id switch
            {
                WORKSPACE => "workspace",
                FOLDER => "folder",
                NOTE => "note",
                FILE => "file",
                _ => throw new ArgumentException($"Invalid entity type: {id}", nameof(id))
            };
        }

        /// <summary>
        /// Gets entity ID by name
        /// </summary>
        public static byte GetId(string name)
        {
            return name?.ToLowerInvariant() switch
            {
                "workspace" => WORKSPACE,
                "folder" => FOLDER,
                "note" => NOTE,
                "file" => FILE,
                _ => throw new ArgumentException($"Invalid entity name: {name}", nameof(name))
            };
        }
    }
}
