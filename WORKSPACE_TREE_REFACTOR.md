# Workspace Tree Refactor - Support Tags, Notes & Files

**Date:** October 21, 2025  
**Status:** Implementation Guide

---

## Overview

Refactoring `GetWorkspaceTagTree` → `GetWorkspaceTree` to support hierarchical tree with:
- **Tags** (can have children: tags, notes, files)
- **Notes** (leaf nodes - cannot have children)
- **Files** (leaf nodes - cannot have children)

---

## Database Schema Changes

### New Table: `files`

```sql
CREATE TABLE [dbo].[files] (
    [file_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [user_id] INT NOT NULL,
    [name] NVARCHAR(500) NOT NULL,
    [original_filename] NVARCHAR(500) NOT NULL,
    [file_path] NVARCHAR(2000) NOT NULL,
    [file_size] BIGINT NOT NULL,
    [mime_type] NVARCHAR(255) NOT NULL,
    [extension] NVARCHAR(50),
    [description] NVARCHAR(MAX),
    [slug] NVARCHAR(255),
    [is_public] BIT DEFAULT 0,
    [is_archived] BIT DEFAULT 0,
    [download_count] INT DEFAULT 0,
    [created_at] DATETIME2(7) DEFAULT GETUTCDATE(),
    [updated_at] DATETIME2(7),
    [deleted_at] DATETIME2(7),
    
    CONSTRAINT [FK_files_user] FOREIGN KEY ([user_id]) 
        REFERENCES [dbo].[users]([user_id]) ON DELETE CASCADE
);

-- Indexes
CREATE INDEX [IX_files_user] ON [dbo].[files]([user_id]) WHERE [deleted_at] IS NULL;
CREATE INDEX [IX_files_slug] ON [dbo].[files]([slug]) WHERE [deleted_at] IS NULL;
CREATE INDEX [IX_files_created] ON [dbo].[files]([created_at] DESC) WHERE [deleted_at] IS NULL;

-- Trigger for slug generation
CREATE TRIGGER [dbo].[tr_files_generate_slug]
ON [dbo].[files]
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE f
    SET f.slug = LOWER(REPLACE(REPLACE(REPLACE(i.name, ' ', '-'), '.', '-'), '--', '-'))
    FROM [dbo].[files] f
    INNER JOIN inserted i ON f.file_id = i.file_id
    WHERE f.slug IS NULL OR f.slug = '';
END;
```

### Update `workspace_items` to support files

The `workspace_items` table already supports this via `child_type` and `child_id`:
- `child_type` = 'tag' → child is a tag
- `child_type` = 'note' → child is a note  
- `child_type` = 'file' → child is a file (NEW)

No schema change needed, just add business logic to handle files.

---

## API Changes

### Endpoint Rename

**Old:**
```
GET /api/tags/workspace/{workspaceId}/tree
```

**New:**
```
GET /api/workspaces/{workspaceId}/tree
```

OR keep in TagsController:
```
GET /api/tags/workspace/{workspaceId}/tree  (deprecated)
↓
GET /api/workspaces/{workspaceId}/tree
```

---

## DTO Changes

### New: `WorkspaceTreeItemResponse`

```csharp
public class WorkspaceTreeItemResponse
{
    public string ItemType { get; set; } // "tag", "note", "file"
    public int ItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? Color { get; set; }
    public string? Icon { get; set; }
    public int? ParentId { get; set; }
    public string? Path { get; set; }
    public int Level { get; set; }
    public int Position { get; set; }
    
    // Type-specific metadata
    public TagMetadata? TagData { get; set; }
    public NoteMetadata? NoteData { get; set; }
    public FileMetadata? FileData { get; set; }
    
    // Tree structure
    public List<WorkspaceTreeItemResponse> Children { get; set; } = new();
    public bool IsExpanded { get; set; } = false;
    public bool IsSelected { get; set; } = false;
}

public class TagMetadata
{
    public string AccessType { get; set; } = string.Empty; // 'owner' or 'shared'
    public int UsageCount { get; set; }
    public int ChildrenCount { get; set; }
}

public class NoteMetadata
{
    public bool IsArchived { get; set; }
    public bool IsPinned { get; set; }
    public bool IsFavorite { get; set; }
    public DateTime? LastModified { get; set; }
}

public class FileMetadata
{
    public string MimeType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string Extension { get; set; } = string.Empty;
    public int DownloadCount { get; set; }
}
```

### Update: `WorkspaceWithTreeResponse`

```csharp
public class WorkspaceWithTreeResponse
{
    // Workspace properties (unchanged)
    public int WorkspaceId { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    // ... other workspace properties
    
    // Tree items (renamed from Tags)
    public List<WorkspaceTreeItemResponse> Items { get; set; } = new();
    
    // Statistics
    public int TagCount { get; set; }
    public int NoteCount { get; set; }
    public int FileCount { get; set; }
    public int TotalItemCount { get; set; }
}
```

---

## Repository Changes

### New: `IFileRepository`

```csharp
public interface IFileRepository
{
    Task<List<FileInfo>> GetWorkspaceFilesAsync(int workspaceId, int userId);
    Task<FileInfo?> GetFileByIdAsync(int fileId);
    Task<FileInfo> CreateFileAsync(FileInfo file);
    Task<FileInfo> UpdateFileAsync(FileInfo file);
    Task<bool> DeleteFileAsync(int fileId);
}
```

### Update: `IWorkspaceRepository`

```csharp
public interface IWorkspaceRepository
{
    // NEW: Get complete tree with tags, notes, and files
    Task<List<WorkspaceTreeItem>> GetWorkspaceTreeAsync(int workspaceId, int userId);
}
```

---

## Stored Procedure

### New: `usp_s_workspace_tree`

```sql
CREATE PROCEDURE [dbo].[usp_s_workspace_tree]
    @iv_workspace_id INT,
    @iv_user_id INT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Get all items in workspace (tags, notes, files)
    SELECT 
        wi.item_id,
        wi.workspace_id,
        wi.parent_tag_id,
        wi.child_type,
        wi.child_id,
        wi.position,
        wi.depth AS level,
        
        -- Tag data
        t.tag_id,
        t.name AS tag_name,
        t.slug AS tag_slug,
        t.color AS tag_color,
        t.icon AS tag_icon,
        t.path AS tag_path,
        
        -- Note data
        n.note_id,
        n.name AS note_name,
        n.slug AS note_slug,
        n.is_archived AS note_is_archived,
        n.is_pinned AS note_is_pinned,
        n.is_favorite AS note_is_favorite,
        n.updated_at AS note_updated_at,
        
        -- File data
        f.file_id,
        f.name AS file_name,
        f.slug AS file_slug,
        f.mime_type,
        f.file_size,
        f.extension,
        f.download_count
        
    FROM [dbo].[workspace_items] wi
    LEFT JOIN [dbo].[tags] t ON wi.child_type = 'tag' AND wi.child_id = t.tag_id
    LEFT JOIN [dbo].[notes] n ON wi.child_type = 'note' AND wi.child_id = n.note_id
    LEFT JOIN [dbo].[files] f ON wi.child_type = 'file' AND wi.child_id = f.file_id
    
    WHERE wi.workspace_id = @iv_workspace_id
        AND (
            -- User owns workspace OR is member
            EXISTS (
                SELECT 1 FROM [dbo].[workspaces] w
                WHERE w.workspace_id = @iv_workspace_id
                    AND w.user_id = @iv_user_id
                    AND w.deleted_at IS NULL
            )
            OR EXISTS (
                SELECT 1 FROM [dbo].[workspace_members] wm
                WHERE wm.workspace_id = @iv_workspace_id
                    AND wm.user_id = @iv_user_id
                    AND wm.deleted_at IS NULL
            )
        )
    ORDER BY wi.parent_tag_id NULLS FIRST, wi.position, wi.child_id;
END;
```

---

## Implementation Steps

### Phase 1: Database Setup
1. ✅ Create `files` table with indexes and trigger
2. ✅ Create `usp_s_workspace_tree` stored procedure
3. ✅ Test with sample data

### Phase 2: DTOs & Models
1. Create `WorkspaceTreeItemResponse`
2. Create `TagMetadata`, `NoteMetadata`, `FileMetadata`
3. Rename `WorkspaceWithTagTreeResponse` → `WorkspaceWithTreeResponse`
4. Create `FileInfo` entity model

### Phase 3: Repository Layer
1. Create `FileRepository` implementing `IFileRepository`
2. Add `GetWorkspaceTreeAsync()` to `WorkspaceRepository`
3. Update mapping logic to handle mixed tree items

### Phase 4: Application Layer
1. Rename folder: `GetWorkspaceTagTree` → `GetWorkspaceTree`
2. Rename: `GetWorkspaceTagTreeQuery` → `GetWorkspaceTreeQuery`
3. Rename: `GetWorkspaceTagTreeQueryHandler` → `GetWorkspaceTreeQueryHandler`
4. Update handler to build mixed tree with tags, notes, files

### Phase 5: API Layer
1. Update `TagsController.GetWorkspaceTagTree()` → `GetWorkspaceTree()`
2. Update route and response types
3. Update XML documentation

### Phase 6: Testing
1. Test tree with only tags (backward compatibility)
2. Test tree with tags + notes
3. Test tree with tags + files
4. Test tree with all three types
5. Test leaf node validation (notes/files cannot have children)

---

## Business Rules

### Tree Structure Rules
1. **Tags** can have children of any type (tag, note, file)
2. **Notes** are ALWAYS leaf nodes (cannot have children)
3. **Files** are ALWAYS leaf nodes (cannot have children)
4. Maximum tree depth: 10 levels (configurable per workspace)

### Access Control
- User must be workspace owner OR member
- Inherited permissions from workspace
- Individual items may have additional restrictions

### Validation
- Cannot add note/file as parent
- Cannot exceed max depth
- Unique item position within same parent

---

## Frontend Changes Needed

```typescript
interface WorkspaceTreeItem {
    itemType: 'tag' | 'note' | 'file';
    itemId: number;
    name: string;
    slug?: string;
    color?: string;
    icon?: string;
    level: number;
    position: number;
    
    // Type-specific data
    tagData?: TagMetadata;
    noteData?: NoteMetadata;
    fileData?: FileMetadata;
    
    // Tree structure
    children: WorkspaceTreeItem[];
    isExpanded: boolean;
    isSelected: boolean;
}

// Different rendering for each type
function renderTreeItem(item: WorkspaceTreeItem) {
    switch(item.itemType) {
        case 'tag':
            return <TagNode item={item} />;
        case 'note':
            return <NoteNode item={item} />; // No expand button
        case 'file':
            return <FileNode item={item} />; // No expand button
    }
}
```

---

## Migration Notes

### Backward Compatibility
- Keep old endpoint for 1-2 versions: `/api/tags/workspace/{id}/tree`
- Mark as deprecated in Swagger
- Return same format but with deprecation warning header

### Data Migration
No data migration needed - existing `workspace_items` already support mixed types.

---

## Testing Checklist

- [ ] Create `files` table successfully
- [ ] `usp_s_workspace_tree` returns correct data
- [ ] DTOs serialize/deserialize correctly
- [ ] Tree building handles mixed types
- [ ] Leaf nodes (note/file) cannot have children
- [ ] Access control works for all item types
- [ ] Frontend can render mixed tree
- [ ] Performance acceptable with 1000+ items

---

## Questions to Resolve

1. Should files table include version history like notes?
2. Should files support sharing/collaboration?
3. Maximum file size limit?
4. Storage location (local, cloud, blob)?
5. Should we support file folders (separate from tags)?

---

**Next Steps:**
1. Review and approve this design
2. Run SQL scripts to create `files` table
3. Implement backend changes in order (Phase 2-5)
4. Update frontend to consume new API
