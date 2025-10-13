namespace SuperAppModels.Models
{
    public class Note
    {
        public int NoteId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Type { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsArchived { get; set; }
        
        // Navigation property - not a database column
        public List<Tag> Tags { get; set; } = new List<Tag>();

        public Note()
        {
            CreatedAt = DateTime.UtcNow;
            IsArchived = false;
            Tags = new List<Tag>();
        }

        public Note(string name, int createdBy) : this()
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            CreatedBy = createdBy;
        }

        /// <summary>
        /// Updates the note content with validation and automatic timestamp management
        /// </summary>
        /// <param name="name">New name</param>
        /// <param name="description">New description</param>
        /// <param name="type">New type</param>
        public void Update(string name, string? description = null, string? type = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be empty", nameof(name));

            Name = name;
            Description = description;
            Type = type;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Toggles the archive status with automatic timestamp management
        /// </summary>
        public void ToggleArchive()
        {
            IsArchived = !IsArchived;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}