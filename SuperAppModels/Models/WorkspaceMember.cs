using System.ComponentModel.DataAnnotations;

namespace SuperAppModels.Models
{
    /// <summary>
    /// Workspace collaboration and access control
    /// Primary table: workspace_members
    /// </summary>
    public class WorkspaceMember
    {
        // Primary Key - member_id
        public long MemberId { get; set; }

        // Relationship
        public int WorkspaceId { get; set; }
        public int UserId { get; set; }

        // Permission level
        public string Role { get; set; } = "viewer";

        // Invitation tracking
        public int? InvitedBy { get; set; }
        public string InvitationStatus { get; set; } = "active";

        // Metadata
        public string? CustomPermissions { get; set; } // JSON

        // Timestamps
        public DateTime? InvitedAt { get; set; }
        public DateTime? JoinedAt { get; set; }
        public DateTime? LastAccessedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        // Navigation properties
        public Workspace Workspace { get; set; } = null!;
        public User User { get; set; } = null!;
        public User? Inviter { get; set; }

        public WorkspaceMember()
        {
            InvitedAt = DateTime.UtcNow;
        }

        public WorkspaceMember(int workspaceId, int userId, string role = "viewer", int? invitedBy = null) : this()
        {
            WorkspaceId = workspaceId;
            UserId = userId;
            Role = role;
            InvitedBy = invitedBy;
        }

        /// <summary>
        /// Accepts the workspace invitation
        /// </summary>
        public void AcceptInvitation()
        {
            InvitationStatus = "active";
            JoinedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Declines the workspace invitation
        /// </summary>
        public void DeclineInvitation()
        {
            InvitationStatus = "declined";
        }

        /// <summary>
        /// Updates the member role
        /// </summary>
        public void UpdateRole(string newRole)
        {
            if (string.IsNullOrWhiteSpace(newRole))
                throw new ArgumentException("Role cannot be empty", nameof(newRole));

            Role = newRole;
        }

        /// <summary>
        /// Updates last accessed timestamp
        /// </summary>
        public void UpdateLastAccessed()
        {
            LastAccessedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Removes member from workspace (soft delete)
        /// </summary>
        public void Remove()
        {
            InvitationStatus = "removed";
            DeletedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Checks if member has owner role
        /// </summary>
        public bool IsOwner => Role == "owner";

        /// <summary>
        /// Checks if member has editor or owner role
        /// </summary>
        public bool CanEdit => Role == "owner" || Role == "editor";

        /// <summary>
        /// Checks if member has any access (viewer, editor, or owner)
        /// </summary>
        public bool CanView => Role == "owner" || Role == "editor" || Role == "viewer";
    }
}