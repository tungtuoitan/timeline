namespace SuperAppModels.Models
{
    /// <summary>
    /// Domain model representing the many-to-many relationship between Notes and Tags
    /// </summary>
    public class NoteTag
    {
        /// <summary>
        /// Note identifier
        /// </summary>
        public int NoteId { get; set; }

        /// <summary>
        /// Tag identifier
        /// </summary>
        public int TagId { get; set; }

        /// <summary>
        /// When the note-tag association was created (UTC)
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Email of the user who created the association
        /// </summary>
        public string? CreatedBy { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public NoteTag()
        {
            CreatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Constructor with required fields
        /// </summary>
        /// <param name="noteId">Note ID</param>
        /// <param name="tagId">Tag ID</param>
        /// <param name="createdBy">Email of the creator</param>
        public NoteTag(int noteId, int tagId, string createdBy) : this()
        {
            NoteId = noteId;
            TagId = tagId;
            CreatedBy = createdBy;
        }
    }
}