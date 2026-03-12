using System.Runtime.Serialization;

namespace SuperAppModels.DTOs.Requests
{
    /// <summary>
    /// Explicit action enum for workspace item batch operations
    /// Follows Microsoft Graph API pattern
    /// </summary>
    public enum KWorkspaceItemAction
    {
        /// <summary>
        /// CREATE new node in workspace
        /// Required: NodeData (name, etc.)
        /// Optional: ParentId (null = root), WorkspaceId
        /// </summary>
        [EnumMember(Value = "CREATE")]
        Create,

        /// <summary>
        /// UPDATE node data (name, description, color, icon)
        /// Required: Id, NodeData
        /// </summary>
        [EnumMember(Value = "UPDATE")]
        Update,

        /// <summary>
        /// MOVE node to new location within same workspace
        /// Required: Id
        /// Optional: ParentId (null = root)
        /// </summary>
        [EnumMember(Value = "MOVE")]
        Move,

        /// <summary>
        /// MOVE node (and all descendants) to another workspace
        /// Required: Id, WorkspaceId (target)
        /// Optional: ParentId (target parent, null = root)
        /// </summary>
        [EnumMember(Value = "MOVECROSS")]
        MoveCross,

        /// <summary>
        /// SOFT DELETE node
        /// Required: Id
        /// </summary>
        [EnumMember(Value = "DELETE")]
        Delete,

        /// <summary>
        /// RESTORE soft-deleted node
        /// Required: Id
        /// </summary>
        [EnumMember(Value = "RESTORE")]
        Restore
    }
}
