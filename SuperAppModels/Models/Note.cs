namespace SuperAppModels.Models
{
    /// <summary>
    /// Domain model representing a Note entity
    /// </summary>
    public class Note
    {
        /// <summary>
        /// Note unique identifier
        /// </summary>
        public int NoteId { get; set; }

        /// <summary>
        /// Note name/title
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Note description/content
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Comma-separated tags for the note
        /// </summary>
        public string? Tags { get; set; }

        /// <summary>
        /// Note type/category
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        /// Email of the user who created the note
        /// </summary>
        public string? CreatedBy { get; set; }

        /// <summary>
        /// When the note was created (UTC)
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// When the note was last updated (UTC)
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Whether the note is archived
        /// </summary>
        public bool IsArchived { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public Note()
        {
            CreatedAt = DateTime.UtcNow;
            IsArchived = false;
        }

        /// <summary>
        /// Constructor with required fields
        /// </summary>
        /// <param name="name">Note name</param>
        /// <param name="createdBy">Email of the creator</param>
        public Note(string name, string createdBy) : this()
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            CreatedBy = createdBy ?? throw new ArgumentNullException(nameof(createdBy));
        }

        /// <summary>
        /// Updates the note content
        /// </summary>
        /// <param name="name">New name</param>
        /// <param name="description">New description</param>
        /// <param name="tags">New tags</param>
        /// <param name="type">New type</param>
        public void Update(string name, string? description = null, string? tags = null, string? type = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be empty", nameof(name));

            Name = name;
            Description = description;
            Tags = tags;
            Type = type;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Toggles the archive status
        /// </summary>
        public void ToggleArchive()
        {
            IsArchived = !IsArchived;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}