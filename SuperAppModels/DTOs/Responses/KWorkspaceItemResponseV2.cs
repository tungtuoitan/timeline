namespace SuperAppModels.DTOs.Responses;

/// <summary>
/// Workspace item response V2 - flat structure with entity data in 'Data' property
/// Represents a single item (folder/note/file) in the workspace tree
///
/// STRUCTURE:
/// - Root level: workspace_items table properties (id, workspaceId, parentId, entityType, etc.)
/// - Data property: Full entity data (FolderData | NoteData | FileData)
///
/// This structure makes it clear which properties come from workspace_items table
/// and which come from the actual entity tables (folders/notes/files)
/// </summary>
public class KWorkspaceItemResponseV2
{
    // ============================================
    // FROM workspace_items TABLE
    // ============================================

    /// <summary>Workspace item ID (workspace_items.id)</summary>
     public int Id { get; set; }

    /// <summary>Workspace ID (workspace_items.workspace_id)</summary>
    public int WorkspaceId { get; set; }

    /// <summary>Parent workspace_item ID (workspace_items.parent_id → workspace_items.id) - SELF-REFERENCING, null for root items</summary>
    public int? ParentId { get; set; }

    /// <summary>Entity type: 2=folder, 3=note, 4=file (workspace_items.entity_type)</summary>
    public byte EntityType { get; set; }

    /// <summary>Entity ID - references folders.id | notes.id | files.id (workspace_items.entity_id)</summary>
    public int EntityId { get; set; }

    /// <summary>Created timestamp (workspace_items.created_at)</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Updated timestamp (workspace_items.updated_at)</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Soft delete timestamp (workspace_items.deleted_at)</summary>
    public DateTime? DeletedAt { get; set; }


    // ============================================
    // COMPUTED PROPERTIES
    // ============================================

    /// <summary>Tree depth level (0 = root)</summary>
    public int Level { get; set; }

    /// <summary>Position in current level for sorting</summary>
    public int Position { get; set; }

    /// <summary>Access type: "owner" or "shared"</summary>
    public string AccessType { get; set; } = "owner";

    /// <summary>True if original, false if copied</summary>
    public bool IsOriginal { get; set; } = true;

    // ============================================
    // ENTITY DATA (polymorphic)
    // ============================================

    /// <summary>
    /// Full entity data - type depends on EntityType:
    /// - EntityType = 2: FolderData
    /// - EntityType = 3: NoteData
    /// - EntityType = 4: FileData
    ///
    /// Example usage:
    /// if (item.EntityType == 2) {
    ///     var folder = (FolderData)item.Data;
    ///     Console.WriteLine(folder.Name);
    /// }
    /// </summary>
    public object Data { get; set; } = null!;

    // ============================================
    // UI STATE (frontend only, not from database)
    // ============================================

    /// <summary>UI state: expanded/collapsed (frontend only)</summary>
    public bool IsExpanded { get; set; } = false;

    /// <summary>UI state: selected (frontend only)</summary>
    public bool IsSelected { get; set; } = false;
}
