namespace SuperAppModels.Models
{
    public class User : ITimestampEntity
    {
        // Database columns - EXACTLY match urm.users schema from REBUILD_SIMPLIFIED_SCHEMA.sql
        public int Id { get; set; } // id (PRIMARY KEY)
        public string Email { get; set; } = string.Empty; // email (UNIQUE NOT NULL)
        public string? Phone { get; set; } // phone
        public string Password { get; set; } = string.Empty; // password (NOT NULL)
        public string AuthType { get; set; } = "local"; // auth_type ('local', 'google', 'facebook')

        // Status flags
        public bool IsActive { get; set; } = true; // is_active

        // Google OAuth tokens (for Google Drive access)
        public string? GoogleAccessToken { get; set; } // google_access_token
        public string? GoogleRefreshToken { get; set; } // google_refresh_token
        public DateTime? GoogleTokenExpiresAt { get; set; } // google_token_expires_at

        // Timestamps (ITimestampEntity)
        public DateTime? LastLoginAt { get; set; } // last_login_at
        public DateTime? CreatedAt { get; set; } // created_at
        public DateTime? UpdatedAt { get; set; } // updated_at
        public DateTime? DeletedAt { get; set; } // deleted_at (soft delete)

        // Navigation properties for EF Core
        public ICollection<Workspace> Workspaces { get; set; } = new List<Workspace>();
        public ICollection<Note> Notes { get; set; } = new List<Note>();

        public User()
        {
            CreatedAt = DateTime.UtcNow;
            IsActive = true;
            AuthType = "local";
        }

        public User(string email, string password) : this()
        {
            Email = email ?? throw new ArgumentNullException(nameof(email));
            Password = password ?? throw new ArgumentNullException(nameof(password));
        }

        /// <summary>
        /// Records successful login
        /// </summary>
        public void RecordLogin()
        {
            LastLoginAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Deactivates user account (soft delete)
        /// </summary>
        public void Deactivate()
        {
            IsActive = false;
            DeletedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Updates password
        /// </summary>
        public void UpdatePassword(string newPassword)
        {
            Password = newPassword ?? throw new ArgumentNullException(nameof(newPassword));
            UpdatedAt = DateTime.UtcNow;
        }
    }

    // Interface for entities with timestamps (used by EF Core)
    public interface ITimestampEntity
    {
        DateTime? CreatedAt { get; set; }
        DateTime? UpdatedAt { get; set; }
    }
}