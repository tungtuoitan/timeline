using System.ComponentModel.DataAnnotations;

namespace SuperAppModels.Models
{
    /// <summary>
    /// Note version history
    /// Primary table: note_versions
    /// </summary>
    public class NoteVersion
    {
        // Primary Key - version_id
        public long VersionId { get; set; }

        // Note reference
        public int NoteId { get; set; }

        // Version info
        public int VersionNumber { get; set; }

        // Content snapshot
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Content { get; set; }

        // Metadata snapshot
        public int WordCount { get; set; }

        // Change tracking
        public string? ChangeSummary { get; set; }
        public int CreatedBy { get; set; }

        // Timestamps
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public Note Note { get; set; } = null!;
        public User Creator { get; set; } = null!;

        public NoteVersion()
        {
            CreatedAt = DateTime.UtcNow;
        }

        public NoteVersion(int noteId, int versionNumber, string name, int createdBy) : this()
        {
            NoteId = noteId;
            VersionNumber = versionNumber;
            Name = name;
            CreatedBy = createdBy;
        }

        /// <summary>
        /// Updates change summary
        /// </summary>
        public void SetChangeSummary(string changeSummary)
        {
            if (string.IsNullOrWhiteSpace(changeSummary))
                throw new ArgumentException("Change summary cannot be empty", nameof(changeSummary));

            if (changeSummary.Length > 500)
                throw new ArgumentException("Change summary cannot exceed 500 characters", nameof(changeSummary));

            ChangeSummary = changeSummary;
        }

        /// <summary>
        /// Checks if this is the first version
        /// </summary>
        public bool IsInitialVersion => VersionNumber == 1;

        /// <summary>
        /// Gets formatted version display
        /// </summary>
        public string VersionDisplay => $"v{VersionNumber}";

        /// <summary>
        /// Gets content size in bytes (UTF-8)
        /// </summary>
        public int ContentSize => string.IsNullOrEmpty(Content) ? 0 : System.Text.Encoding.UTF8.GetByteCount(Content);

        /// <summary>
        /// Checks if version has content changes
        /// </summary>
        public bool HasContentChanges => !string.IsNullOrEmpty(Content);

        /// <summary>
        /// Checks if version has description changes
        /// </summary>
        public bool HasDescriptionChanges => !string.IsNullOrEmpty(Description);
    }
}