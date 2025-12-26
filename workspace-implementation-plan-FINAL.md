# Workspace System - Implementation Plan FINAL

**Ngày:** 2025-12-25 (Updated with clarifications)
**Tác giả:** Claude Analysis
**Status:** READY FOR IMPLEMENTATION

---

## 📋 EXECUTIVE SUMMARY

### ✅ Plan đã được clarify đầy đủ
- Bin logic: Multiple bins (workspace bin vs entity bin)
- Permission model: Editor không thể delete file gốc
- Frontend filtering: Load all, filter on client
- Move/Copy logic: Rõ ràng cho cùng/khác owner
- CopyInfo design: Single JSON column

### 🎯 Key Changes từ version trước
1. ~~Removed: sp_GetWorkspaceTree~~ → Frontend filtering
2. Updated: Permission matrix (Editor can restore workspace_item)
3. Simplified: Move logic (just update workspace_id + parent_id)
4. New: CopyInfo JSON column design

---

## 1️⃣ CLARIFIED REQUIREMENTS

### 1.1 Bin Logic - Multiple Bin Types

#### **3 loại Bin khác nhau:**

```typescript
// Type 1: Workspace Bin
// Show workspace_items with deletedAt = x trong workspace đó
GET /api/workspaces/{id}/bin
→ workspace_items WHERE workspace_id = {id} AND deleted_at IS NOT NULL

// Type 2: NoteGrid Bin (Global)
// Show notes with deletedAt = x của owner
GET /api/notes/bin?userId={id}
→ notes WHERE user_id = {id} AND deleted_at IS NOT NULL

// Type 3: FileGrid Bin (Global)
GET /api/files/bin?userId={id}
→ files WHERE user_id = {id} AND deleted_at IS NOT NULL
```

#### **Restore behavior:**

| Action | Effect | Permission |
|--------|--------|------------|
| Restore F1 từ Workspace Bin | `workspace_item.deletedAt = null` | Workspace editor/owner |
| Restore F1 từ NoteGrid Bin | `note.deletedAt = null` | Note owner only |
| Permanent delete từ Workspace Bin | Delete workspace_item | Workspace owner only |
| Permanent delete từ NoteGrid Bin | Delete note + cascade workspace_items | Note owner only |

**Key insight:** Restore ở workspace ≠ Restore ở NoteGrid!

---

### 1.2 Permission Matrix - Updated

| Action | Workspace Owner | Workspace Editor | Workspace Viewer | Item Owner (not in workspace) |
|--------|----------------|------------------|------------------|-------------------------------|
| View workspace items | ✅ | ✅ | ✅ | ❌ |
| Add item to workspace | ✅ | ✅ | ❌ | ❌ |
| Edit item content | ✅ | ✅ | ❌ | ✅ (own items) |
| Delete workspace_item | ✅ | ✅ | ❌ | ❌ |
| Restore workspace_item | ✅ | ✅ | ❌ | ❌ |
| Soft delete item (to NoteGrid Bin) | ✅ | ❌ | ❌ | ✅ (own items) |
| Restore from NoteGrid Bin | ✅ | ❌ | ❌ | ✅ (own items) |
| Permanent delete workspace_item | ✅ | ❌ | ❌ | ❌ |
| Permanent delete item | ✅ | ❌ | ❌ | ✅ (own items) |
| Manage workspace members | ✅ | ❌ | ❌ | ❌ |

**Key changes:**
- ✅ Editor CAN restore workspace_item (trong workspace bin)
- ❌ Editor CANNOT delete item gốc (chỉ owner)
- ❌ Editor CANNOT permanent delete

---

### 1.3 Frontend Filtering - Simplified

**Current approach:** Backend returns ALL, Frontend filters

```typescript
// Backend API - NO filtering
GET /api/workspaces/{id}/items
→ Returns ALL workspace_items (including deleted, invalid parent)

// Frontend filtering
function getVisibleItems(allItems: WorkspaceItem[]) {
  const deletedIds = new Set(
    allItems.filter(i => i.deletedAt != null).map(i => i.id)
  );

  const deletedEntityIds = new Set(
    allItems.filter(i => i.entityDeletedAt != null)
      .map(i => `${i.itemType}-${i.itemId}`)
  );

  // Recursive function to check if any ancestor is deleted
  function hasDeletedAncestor(item: WorkspaceItem): boolean {
    if (!item.parentId) return false;

    const parent = allItems.find(i =>
      i.itemType === 2 && i.itemId === item.parentId
    );

    if (!parent) return true;  // Parent not found = invalid
    if (deletedIds.has(parent.id)) return true;  // Parent deleted

    return hasDeletedAncestor(parent);  // Check grandparent
  }

  return allItems.filter(item => {
    // Filter 1: Item itself not deleted
    if (item.deletedAt != null) return false;

    // Filter 2: Entity not deleted
    const entityKey = `${item.itemType}-${item.itemId}`;
    if (deletedEntityIds.has(entityKey)) return false;

    // Filter 3: No deleted ancestor
    if (hasDeletedAncestor(item)) return false;

    return true;
  });
}
```

**Implication:** NO need for `sp_GetWorkspaceTree` stored procedure!

---

### 1.4 Move vs Copy Logic - Clarified

#### **Move (same owner):**
```csharp
// Simply update workspace_item
public async Task<ResultOptions> MoveItemToWorkspace(
    int itemId,
    int fromWorkspaceId,
    int toWorkspaceId,
    int? newParentId)
{
    var item = await _context.WorkspaceItems.FindAsync(itemId);

    // Validate both workspaces have same owner
    var fromWs = await GetWorkspace(fromWorkspaceId);
    var toWs = await GetWorkspace(toWorkspaceId);

    if (fromWs.UserId != toWs.UserId)
        return Error("Cannot move between different owners");

    // Update
    item.WorkspaceId = toWorkspaceId;
    item.ParentId = newParentId;
    item.UpdatedAt = DateTime.UtcNow;

    await _context.SaveChangesAsync();
    return Success();
}
```

#### **Copy same owner:**
```csharp
// Just create new workspace_item (NO clone entity)
public async Task<ResultOptions> CopyItemToWorkspace(
    int workspaceId,
    byte itemType,
    int itemId,
    int? parentId)
{
    var workspace = await GetWorkspace(workspaceId);
    var item = await GetItem(itemType, itemId);

    if (item.UserId == workspace.UserId)
    {
        // SAME OWNER: Just create workspace_item
        var newWorkspaceItem = new WorkspaceItemEntity
        {
            WorkspaceId = workspaceId,
            ParentId = parentId,
            ItemType = itemType,
            ItemId = itemId,  // ✅ Same itemId
            CopyInfo = null,  // ✅ NULL for same owner
            CreatedAt = DateTime.UtcNow
        };

        _context.WorkspaceItems.Add(newWorkspaceItem);
        await _context.SaveChangesAsync();

        return Success(newWorkspaceItem);
    }
    else
    {
        // DIFFERENT OWNER: Clone entity + create workspace_item
        return await CloneAndCopyItem(workspaceId, itemType, itemId, parentId);
    }
}
```

#### **Copy different owner:**
```csharp
// Clone entity + create workspace_item
public async Task<ResultOptions> CloneAndCopyItem(
    int workspaceId,
    byte itemType,
    int itemId,
    int? parentId)
{
    var workspace = await GetWorkspace(workspaceId);
    var sourceItem = await GetItem(itemType, itemId);

    // Clone entity
    var clonedItem = itemType switch
    {
        3 => await CloneNote((Note)sourceItem, workspace.UserId),
        4 => await CloneFile((File)sourceItem, workspace.UserId),
        2 => await CloneFolder((Folder)sourceItem, workspace.UserId),
        _ => throw new ArgumentException("Invalid item type")
    };

    // Create workspace_item
    var workspaceItem = new WorkspaceItemEntity
    {
        WorkspaceId = workspaceId,
        ParentId = parentId,
        ItemType = itemType,
        ItemId = clonedItem.Id,  // ✅ New cloned itemId
        CopyInfo = JsonSerializer.Serialize(new CopyMetadata
        {
            SourceItemId = itemId,
            SourceItemType = itemType,
            SourceOwnerId = sourceItem.UserId,
            CopiedAt = DateTime.UtcNow,
            CopyType = "cross-owner"
        }),
        CreatedAt = DateTime.UtcNow
    };

    _context.WorkspaceItems.Add(workspaceItem);
    await _context.SaveChangesAsync();

    return Success(workspaceItem);
}

private async Task<Note> CloneNote(Note source, int newOwnerId)
{
    var cloned = new Note
    {
        UserId = newOwnerId,
        Name = source.Name,
        Description = source.Description,
        CopyInfo = JsonSerializer.Serialize(new CopyMetadata
        {
            SourceItemId = source.Id,
            SourceItemType = 3,
            SourceOwnerId = source.UserId,
            CopiedAt = DateTime.UtcNow,
            SourceName = source.Name
        }),
        CreatedAt = DateTime.UtcNow
    };

    _context.Notes.Add(cloned);
    await _context.SaveChangesAsync();

    return cloned;
}
```

**Key insight:**
- Same owner → Reuse item (just create workspace_item)
- Different owner → Clone item (new entity + workspace_item)

---

### 1.5 CopyInfo Design - Single JSON Column

#### **Schema Design:**

```sql
-- Add CopyInfo to ALL relevant tables
ALTER TABLE dbo.notes ADD copy_info NVARCHAR(MAX) NULL;
ALTER TABLE ws.files ADD copy_info NVARCHAR(MAX) NULL;
ALTER TABLE ws.folders ADD copy_info NVARCHAR(MAX) NULL;
ALTER TABLE ws.workspace_items ADD copy_info NVARCHAR(MAX) NULL;
ALTER TABLE ws.workspaces ADD copy_info NVARCHAR(MAX) NULL;

-- Indexes for finding copied items
CREATE INDEX IX_notes_copy_info ON dbo.notes(copy_info)
WHERE copy_info IS NOT NULL;

CREATE INDEX IX_files_copy_info ON ws.files(copy_info)
WHERE copy_info IS NOT NULL;

CREATE INDEX IX_folders_copy_info ON ws.folders(copy_info)
WHERE copy_info IS NOT NULL;
```

#### **CopyInfo JSON Structure:**

```typescript
interface CopyMetadata {
  // Source tracking
  sourceItemId: number;
  sourceItemType: number;  // 1=workspace, 2=folder, 3=note, 4=file
  sourceOwnerId: number;
  sourceName?: string;

  // Copy metadata
  copiedAt: string;  // ISO datetime
  copyType: 'same-owner' | 'cross-owner' | 'workspace-copy' | 'structure-copy';

  // Structure tracking (for folder copy with children)
  isStructureCopy?: boolean;
  parentCopyId?: number;  // If this is part of a structure copy
  childrenCopied?: number;  // Count of children copied

  // Sync metadata (future use)
  lastSyncAt?: string;
  syncEnabled?: boolean;
  checksum?: string;
  version?: string;

  // Custom metadata
  notes?: string;
  tags?: string[];
}
```

#### **Example scenarios:**

**Scenario 1: Copy single note (cross-owner)**
```json
// note.copy_info
{
  "sourceItemId": 123,
  "sourceItemType": 3,
  "sourceOwnerId": 456,
  "sourceName": "Original Note Title",
  "copiedAt": "2025-12-25T10:30:00Z",
  "copyType": "cross-owner"
}

// workspace_item.copy_info = null (just a normal link)
```

**Scenario 2: Copy folder structure (same owner)**
```json
// Folder A
// workspace_item.copy_info
{
  "sourceItemId": 10,
  "sourceItemType": 2,
  "sourceOwnerId": 1,
  "copiedAt": "2025-12-25T10:30:00Z",
  "copyType": "same-owner",
  "isStructureCopy": true,
  "childrenCopied": 5
}

// folder.copy_info = null (same owner, reuse entity)
```

**Scenario 3: Copy folder structure (cross-owner)**
```json
// New cloned Folder A
// folder.copy_info
{
  "sourceItemId": 10,
  "sourceItemType": 2,
  "sourceOwnerId": 456,
  "sourceName": "Projects",
  "copiedAt": "2025-12-25T10:30:00Z",
  "copyType": "cross-owner",
  "isStructureCopy": true,
  "childrenCopied": 5
}

// Child Note (cloned)
// note.copy_info
{
  "sourceItemId": 123,
  "sourceItemType": 3,
  "sourceOwnerId": 456,
  "parentCopyId": 10,  // Part of structure copy
  "copiedAt": "2025-12-25T10:30:00Z",
  "copyType": "structure-copy"
}

// workspace_item.copy_info
{
  "sourceItemId": 50,  // Original workspace_item id
  "sourceItemType": 3,
  "sourceOwnerId": 456,
  "copiedAt": "2025-12-25T10:30:00Z",
  "copyType": "cross-owner",
  "isStructureCopy": true,
  "parentCopyId": 40  // Parent workspace_item in structure
}
```

**Scenario 4: Copy entire workspace**
```json
// workspace.copy_info
{
  "sourceItemId": 1,  // Original workspace id
  "sourceItemType": 1,
  "sourceOwnerId": 456,
  "sourceName": "Original Workspace",
  "copiedAt": "2025-12-25T10:30:00Z",
  "copyType": "cross-owner",
  "isStructureCopy": true,
  "childrenCopied": 25
}
```

**Key insight:**
- Same owner → `copy_info = null` in entity, may have in workspace_item
- Cross owner → `copy_info` in both entity and workspace_item
- Structure copy → Track parent/children relationship

---

## 2️⃣ UPDATED EDGE CASES

### 🟢 RESOLVED Edge Cases

#### ~~EDGE CASE #1: Restore from Bin~~ ✅ RESOLVED
**Resolution:**
- Workspace Bin restore → `workspace_item.deletedAt = null`
- NoteGrid Bin restore → `note.deletedAt = null`
- Hai hành động độc lập, không ảnh hưởng nhau

#### ~~EDGE CASE #4: Query performance~~ ✅ RESOLVED
**Resolution:** Frontend filtering, no need for stored procedure

#### ~~EDGE CASE #6: Move between workspaces~~ ✅ RESOLVED
**Resolution:** Update `workspace_id` + `parent_id`, validate same owner

---

### 🔴 NEW Critical Edge Cases

#### **EDGE CASE #10: Copy structure with deep hierarchy**

**Scenario:**
```
Copy Folder A (cross-owner)
  ├─ Folder B
  │   ├─ Folder C
  │   │   └─ Note N1
  │   └─ File F1
  └─ Note N2

Total: 3 folders, 2 notes, 1 file
```

**Vấn đề:**
- Cần clone 6 entities + create 6 workspace_items
- Phải maintain parent-child relationship
- Transaction có thể fail giữa chừng

**Giải pháp:**

```csharp
public async Task<ResultOptions> CopyFolderStructure(
    int sourceFolderId,
    int targetWorkspaceId,
    int? targetParentId,
    int currentUserId)
{
    using var transaction = await _context.Database.BeginTransactionAsync();

    try
    {
        // 1. Get source workspace_items (all descendants)
        var sourceWorkspaceItems = await GetAllDescendants(sourceFolderId);

        // 2. Create mapping: old ID → new ID
        var idMapping = new Dictionary<(byte itemType, int itemId), int>();
        var parentMapping = new Dictionary<int, int>();  // old workspace_item.id → new

        // 3. Clone entities in order (BFS to maintain parent-child)
        var queue = new Queue<WorkspaceItemEntity>();
        var root = sourceWorkspaceItems.First(i => i.ItemId == sourceFolderId);
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            // Clone entity if cross-owner
            int newItemId;
            if (await IsCrossOwner(current, targetWorkspaceId))
            {
                newItemId = await CloneEntity(current.ItemType, current.ItemId, currentUserId);
                idMapping[(current.ItemType, current.ItemId)] = newItemId;
            }
            else
            {
                newItemId = current.ItemId;  // Reuse for same owner
            }

            // Get new parent_id from mapping
            int? newParentId = current.ParentId.HasValue &&
                              parentMapping.ContainsKey(current.Id)
                ? parentMapping[current.Id]
                : targetParentId;

            // Create workspace_item
            var newWorkspaceItem = new WorkspaceItemEntity
            {
                WorkspaceId = targetWorkspaceId,
                ParentId = newParentId,
                ItemType = current.ItemType,
                ItemId = newItemId,
                CopyInfo = JsonSerializer.Serialize(new CopyMetadata
                {
                    SourceItemId = current.ItemId,
                    SourceItemType = current.ItemType,
                    SourceOwnerId = current.Workspace.UserId,
                    CopiedAt = DateTime.UtcNow,
                    CopyType = "structure-copy",
                    ParentCopyId = current.ParentId
                }),
                CreatedAt = DateTime.UtcNow
            };

            _context.WorkspaceItems.Add(newWorkspaceItem);
            await _context.SaveChangesAsync();  // Get new ID

            // Update mapping for children
            var children = sourceWorkspaceItems
                .Where(i => i.ParentId == current.ItemId);

            foreach (var child in children)
            {
                parentMapping[child.Id] = newWorkspaceItem.ItemId;
                queue.Enqueue(child);
            }
        }

        await transaction.CommitAsync();

        return Success($"Copied {sourceWorkspaceItems.Count} items");
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        return Error($"Copy failed: {ex.Message}");
    }
}
```

**Priority:** 🔥 HIGH
**Effort:** High

---

#### **EDGE CASE #11: Duplicate item in same workspace**

**Scenario:**
```
Workspace W1:
  - Note N1 (workspace_item id=10, item_id=100)

User tries to add Note N1 again
  → workspace_item id=20, item_id=100 (DUPLICATE!)
```

**Vấn đề:**
Database constraint `UNIQUE (workspace_id, item_type, item_id)` sẽ fail!

**Clarification cần:**
- Có cho phép duplicate không?
- Nếu không → Validate trước khi add
- Nếu có → Remove unique constraint

**Giải pháp đề xuất:**

**Option A: KHÔNG cho phép duplicate** (Recommend)
```csharp
public async Task<ResultOptions> AddItemToWorkspace(...)
{
    // Check duplicate
    var exists = await _context.WorkspaceItems
        .AnyAsync(wi => wi.WorkspaceId == workspaceId
                     && wi.ItemType == itemType
                     && wi.ItemId == itemId
                     && wi.DeletedAt == null);

    if (exists)
    {
        return new ResultOptions
        {
            Success = false,
            Message = "Item already exists in this workspace",
            Status = 409
        };
    }

    // Proceed...
}
```

**Option B: Cho phép duplicate (like shortcuts)**
```sql
-- Remove unique constraint
ALTER TABLE ws.workspace_items
DROP CONSTRAINT UQ_workspace_items_unique;

-- Add index instead
CREATE INDEX IX_workspace_items_item
ON ws.workspace_items(workspace_id, item_type, item_id);
```

**Decision needed:** Option A or B?

**Priority:** 🔥 CRITICAL
**Effort:** Low

---

#### **EDGE CASE #12: Copy workspace_item với parent không tồn tại**

**Scenario:**
```
Copy workspace_item {
  parent_id: 10 (Folder A trong workspace cũ)
  item_id: 100
}

Sang workspace mới
→ Folder A (id=10) không tồn tại trong workspace mới!
```

**Giải pháp:**
```csharp
// Khi copy structure, phải copy parent trước children
// Sử dụng BFS (như EDGE CASE #10)

// Nếu copy single item, reset parent_id
public async Task<ResultOptions> CopySingleItem(...)
{
    var newWorkspaceItem = new WorkspaceItemEntity
    {
        WorkspaceId = targetWorkspaceId,
        ParentId = null,  // ✅ Reset to root level
        ItemType = itemType,
        ItemId = newItemId,
        // ...
    };
}
```

**Priority:** 🟡 IMPORTANT
**Effort:** Low

---

#### **EDGE CASE #13: Workspace member delete item → Item vào bin của ai?**

**Scenario:**
```
Workspace W1 (owner = User A)
User B (editor) delete Note N1 trong workspace
  → workspace_item.deletedAt = X ✅

User B vào NoteGrid Bin → Không thấy Note N1 (vì owner = User A)
User A vào NoteGrid Bin → Có thấy Note N1
```

**Clarification:**
- Workspace Bin của W1 → Show workspace_items deleted (B có thể restore)
- NoteGrid Bin → Show notes của owner (chỉ A thấy)

**Behavior này đã đúng theo plan!** ✅

**Priority:** ✅ CONFIRMED
**Effort:** N/A (already correct)

---

## 3️⃣ DATABASE SCHEMA - FINAL

### Migration Script

```sql
-- =============================================
-- Workspace System Schema Updates
-- Date: 2025-12-25
-- =============================================

USE [SuperApp-dev];
GO

BEGIN TRANSACTION;

-- =============================================
-- 1. REMOVE is_original column
-- =============================================
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('ws.workspace_items')
    AND name = 'is_original'
)
BEGIN
    -- Drop index first
    IF EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'IX_workspace_items_original')
    BEGIN
        DROP INDEX IX_workspace_items_original ON ws.workspace_items;
        PRINT '  ✓ Dropped index: IX_workspace_items_original';
    END

    -- Drop column
    ALTER TABLE ws.workspace_items DROP COLUMN is_original;
    PRINT '  ✓ Dropped column: is_original';
END
ELSE
BEGIN
    PRINT '  ○ Column is_original already removed';
END

-- =============================================
-- 2. ADD copy_info columns
-- =============================================

-- Notes
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.notes')
    AND name = 'copy_info'
)
BEGIN
    ALTER TABLE dbo.notes ADD copy_info NVARCHAR(MAX) NULL;
    PRINT '  ✓ Added column: dbo.notes.copy_info';
END

-- Files
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('ws.files')
    AND name = 'copy_info'
)
BEGIN
    ALTER TABLE ws.files ADD copy_info NVARCHAR(MAX) NULL;
    PRINT '  ✓ Added column: ws.files.copy_info';
END

-- Folders
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('ws.folders')
    AND name = 'copy_info'
)
BEGIN
    ALTER TABLE ws.folders ADD copy_info NVARCHAR(MAX) NULL;
    PRINT '  ✓ Added column: ws.folders.copy_info';
END

-- Workspace Items
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('ws.workspace_items')
    AND name = 'copy_info'
)
BEGIN
    ALTER TABLE ws.workspace_items ADD copy_info NVARCHAR(MAX) NULL;
    PRINT '  ✓ Added column: ws.workspace_items.copy_info';
END

-- Workspaces
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('ws.workspaces')
    AND name = 'copy_info'
)
BEGIN
    ALTER TABLE ws.workspaces ADD copy_info NVARCHAR(MAX) NULL;
    PRINT '  ✓ Added column: ws.workspaces.copy_info';
END

-- =============================================
-- 3. ADD performance indexes
-- =============================================

-- Index for finding copied notes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_notes_copy_info')
BEGIN
    CREATE INDEX IX_notes_copy_info ON dbo.notes(copy_info)
    WHERE copy_info IS NOT NULL;
    PRINT '  ✓ Created index: IX_notes_copy_info';
END

-- Index for finding copied files
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_files_copy_info')
BEGIN
    CREATE INDEX IX_files_copy_info ON ws.files(copy_info)
    WHERE copy_info IS NOT NULL;
    PRINT '  ✓ Created index: IX_files_copy_info';
END

-- Index for finding copied folders
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_folders_copy_info')
BEGIN
    CREATE INDEX IX_folders_copy_info ON ws.folders(copy_info)
    WHERE copy_info IS NOT NULL;
    PRINT '  ✓ Created index: IX_folders_copy_info';
END

-- Improve parent lookup performance
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_workspace_items_parent_lookup')
BEGIN
    CREATE INDEX IX_workspace_items_parent_lookup
    ON ws.workspace_items(workspace_id, parent_id, deleted_at)
    WHERE deleted_at IS NULL;
    PRINT '  ✓ Created index: IX_workspace_items_parent_lookup';
END

-- Improve item filtering
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_workspace_items_item_lookup')
BEGIN
    CREATE INDEX IX_workspace_items_item_lookup
    ON ws.workspace_items(item_type, item_id, deleted_at)
    WHERE deleted_at IS NULL;
    PRINT '  ✓ Created index: IX_workspace_items_item_lookup';
END

COMMIT TRANSACTION;

PRINT '';
PRINT '=============================================';
PRINT '✅ Schema update completed successfully!';
PRINT '=============================================';
GO

-- =============================================
-- VERIFY CHANGES
-- =============================================
PRINT '';
PRINT 'Verifying schema changes...';
PRINT '';

-- Check removed column
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('ws.workspace_items')
    AND name = 'is_original'
)
    PRINT '  ✓ Confirmed: is_original removed';
ELSE
    PRINT '  ✗ ERROR: is_original still exists';

-- Check added columns
SELECT
    t.name AS table_name,
    c.name AS column_name,
    ty.name AS data_type,
    c.max_length
FROM sys.tables t
INNER JOIN sys.columns c ON t.object_id = c.object_id
INNER JOIN sys.types ty ON c.user_type_id = ty.user_type_id
WHERE c.name = 'copy_info'
  AND t.name IN ('notes', 'files', 'folders', 'workspace_items', 'workspaces')
ORDER BY t.name;

-- Check indexes
SELECT
    i.name AS index_name,
    t.name AS table_name,
    i.type_desc
FROM sys.indexes i
INNER JOIN sys.tables t ON i.object_id = t.object_id
WHERE i.name LIKE '%copy_info%'
   OR i.name LIKE '%parent_lookup%'
   OR i.name LIKE '%item_lookup%'
ORDER BY t.name, i.name;

PRINT '';
PRINT 'Verification complete!';
GO
```

---

## 4️⃣ EF CORE MODELS - FINAL

### Updated Models

```csharp
// =============================================
// Note.cs
// =============================================
public class Note : ITimestampEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Timestamps
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Copy tracking
    public string? CopyInfo { get; set; }  // JSON

    // Navigation
    public User User { get; set; } = null!;

    // Helper method
    public bool IsCopy => !string.IsNullOrEmpty(CopyInfo);
}


// =============================================
// File.cs
// =============================================
public class File : ITimestampEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Url { get; set; }
    public long? FileSize { get; set; }
    public string? MimeType { get; set; }
    public string? Extension { get; set; }

    // Timestamps
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Copy tracking
    public string? CopyInfo { get; set; }  // JSON

    // Navigation
    public User User { get; set; } = null!;

    // Helper
    public bool IsCopy => !string.IsNullOrEmpty(CopyInfo);
}


// =============================================
// Folder.cs
// =============================================
public class Folder : ITimestampEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Color { get; set; } = "#F59E0B";
    public string? Icon { get; set; } = "📁";

    // Timestamps
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Copy tracking
    public string? CopyInfo { get; set; }  // JSON

    // Navigation
    public User User { get; set; } = null!;
    public ICollection<WorkspaceItemEntity> WorkspaceItems { get; set; } = new List<WorkspaceItemEntity>();

    // Helper
    public bool IsCopy => !string.IsNullOrEmpty(CopyInfo);
}


// =============================================
// WorkspaceItemEntity.cs
// =============================================
public class WorkspaceItemEntity : ITimestampEntity
{
    public int Id { get; set; }
    public int WorkspaceId { get; set; }
    public int? ParentId { get; set; }
    public byte ItemType { get; set; }
    public int ItemId { get; set; }

    // REMOVED: public bool IsOriginal { get; set; }

    // Timestamps
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Copy tracking
    public string? CopyInfo { get; set; }  // JSON

    // Navigation
    public Workspace Workspace { get; set; } = null!;
    public Folder? Folder { get; set; }

    // Polymorphic navigation
    [NotMapped]
    public Folder? ChildFolder { get; set; }

    [NotMapped]
    public Note? ChildNote { get; set; }

    [NotMapped]
    public File? ChildFile { get; set; }

    // Helper
    public bool IsCopy => !string.IsNullOrEmpty(CopyInfo);
}


// =============================================
// Workspace.cs
// =============================================
public class Workspace : ITimestampEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Timestamps
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Copy tracking
    public string? CopyInfo { get; set; }  // JSON

    // Navigation
    public User User { get; set; } = null!;
    public ICollection<WorkspaceItemEntity> Items { get; set; } = new List<WorkspaceItemEntity>();

    // Helper
    public bool IsCopy => !string.IsNullOrEmpty(CopyInfo);
}


// =============================================
// DTOs/CopyMetadata.cs - NEW
// =============================================
namespace SuperAppModels.DTOs
{
    public class CopyMetadata
    {
        // Source tracking
        public int SourceItemId { get; set; }
        public byte SourceItemType { get; set; }  // 1=workspace, 2=folder, 3=note, 4=file
        public int SourceOwnerId { get; set; }
        public string? SourceName { get; set; }

        // Copy metadata
        public DateTime CopiedAt { get; set; }
        public string CopyType { get; set; } = "cross-owner";  // same-owner | cross-owner | workspace-copy | structure-copy

        // Structure tracking
        public bool IsStructureCopy { get; set; }
        public int? ParentCopyId { get; set; }
        public int ChildrenCopied { get; set; }

        // Sync metadata (future)
        public DateTime? LastSyncAt { get; set; }
        public bool SyncEnabled { get; set; }
        public string? Checksum { get; set; }
        public string? Version { get; set; }

        // Custom
        public string? Notes { get; set; }
        public List<string>? Tags { get; set; }
    }
}
```

### Updated EF Configurations

```csharp
// NoteConfiguration.cs - ADD
builder.Property(n => n.CopyInfo)
    .HasColumnName("copy_info")
    .HasColumnType("nvarchar(max)");

// FileConfiguration.cs - ADD
builder.Property(f => f.CopyInfo)
    .HasColumnName("copy_info")
    .HasColumnType("nvarchar(max)");

// FolderConfiguration.cs - ADD
builder.Property(f => f.CopyInfo)
    .HasColumnName("copy_info")
    .HasColumnType("nvarchar(max)");

// WorkspaceItemConfiguration.cs - REMOVE + ADD
// REMOVE:
// builder.Property(wi => wi.IsOriginal)...

// ADD:
builder.Property(wi => wi.CopyInfo)
    .HasColumnName("copy_info")
    .HasColumnType("nvarchar(max)");

// WorkspaceConfiguration.cs - ADD
builder.Property(w => w.CopyInfo)
    .HasColumnName("copy_info")
    .HasColumnType("nvarchar(max)");
```

---

## 5️⃣ API ENDPOINTS - FINAL

### New Endpoints

```csharp
// =============================================
// WorkspaceController.cs
// =============================================

/// <summary>
/// Get workspace bin (deleted workspace_items in this workspace)
/// </summary>
[HttpGet("api/workspaces/{id}/bin")]
[Authorize]
public async Task<IActionResult> GetWorkspaceBin(int id)
{
    var items = await _context.WorkspaceItems
        .Where(wi => wi.WorkspaceId == id && wi.DeletedAt != null)
        .ToListAsync();

    return Ok(items);
}

/// <summary>
/// Restore workspace_item from workspace bin
/// Permission: Editor or Owner
/// </summary>
[HttpPost("api/workspaces/{workspaceId}/bin/{itemId}/restore")]
[Authorize]
public async Task<IActionResult> RestoreWorkspaceItem(int workspaceId, int itemId)
{
    // Check permission (editor or owner)
    var hasPermission = await _workspaceService.HasPermission(
        GetUserId(), workspaceId, "edit"
    );

    if (!hasPermission)
        return Forbid();

    var item = await _context.WorkspaceItems.FindAsync(itemId);
    if (item == null || item.WorkspaceId != workspaceId)
        return NotFound();

    item.DeletedAt = null;
    item.UpdatedAt = DateTime.UtcNow;

    await _context.SaveChangesAsync();

    return Ok(new { message = "Item restored to workspace" });
}

/// <summary>
/// Permanent delete workspace_item from workspace bin
/// Permission: Owner only
/// </summary>
[HttpDelete("api/workspaces/{workspaceId}/bin/{itemId}/permanent")]
[Authorize]
public async Task<IActionResult> PermanentDeleteWorkspaceItem(int workspaceId, int itemId)
{
    // Check permission (owner only)
    var isOwner = await _workspaceService.IsOwner(GetUserId(), workspaceId);
    if (!isOwner)
        return Forbid();

    var item = await _context.WorkspaceItems.FindAsync(itemId);
    if (item == null || item.WorkspaceId != workspaceId)
        return NotFound();

    _context.WorkspaceItems.Remove(item);
    await _context.SaveChangesAsync();

    return Ok(new { message = "Workspace item permanently deleted" });
}

/// <summary>
/// Move item to another workspace (same owner)
/// </summary>
[HttpPost("api/workspaces/{id}/items/move")]
[Authorize]
public async Task<IActionResult> MoveItem(
    int id,
    [FromBody] MoveItemRequest request)
{
    var result = await _workspaceItemService.MoveItemToWorkspace(
        request.ItemId,
        request.FromWorkspaceId,
        id,  // toWorkspaceId
        request.NewParentId
    );

    return StatusCode(result.Status, result);
}

/// <summary>
/// Copy item to workspace (same owner = reuse, different owner = clone)
/// </summary>
[HttpPost("api/workspaces/{id}/items/copy")]
[Authorize]
public async Task<IActionResult> CopyItem(
    int id,
    [FromBody] CopyItemRequest request)
{
    var result = await _workspaceItemService.CopyItemToWorkspace(
        id,  // targetWorkspaceId
        request.ItemType,
        request.ItemId,
        request.TargetParentId,
        request.CopyChildren  // for folders
    );

    return StatusCode(result.Status, result);
}

/// <summary>
/// Copy entire workspace structure
/// </summary>
[HttpPost("api/workspaces/{id}/copy")]
[Authorize]
public async Task<IActionResult> CopyWorkspace(
    int id,
    [FromBody] CopyWorkspaceRequest request)
{
    var result = await _workspaceService.CopyWorkspace(
        id,  // sourceWorkspaceId
        request.NewWorkspaceName,
        GetUserId()
    );

    return StatusCode(result.Status, result);
}


// =============================================
// NotesController.cs
// =============================================

/// <summary>
/// Get note bin for current user (global)
/// </summary>
[HttpGet("api/notes/bin")]
[Authorize]
public async Task<IActionResult> GetNoteBin()
{
    var notes = await _context.Notes
        .Where(n => n.UserId == GetUserId() && n.DeletedAt != null)
        .ToListAsync();

    return Ok(notes);
}

/// <summary>
/// Restore note from note bin (global)
/// Permission: Owner only
/// </summary>
[HttpPost("api/notes/bin/{id}/restore")]
[Authorize]
public async Task<IActionResult> RestoreNote(int id)
{
    var note = await _context.Notes.FindAsync(id);
    if (note == null || note.UserId != GetUserId())
        return NotFound();

    note.DeletedAt = null;
    note.UpdatedAt = DateTime.UtcNow;

    await _context.SaveChangesAsync();

    return Ok(new { message = "Note restored" });
}

/// <summary>
/// Permanent delete note + cascade workspace_items
/// Permission: Owner only
/// </summary>
[HttpDelete("api/notes/bin/{id}/permanent")]
[Authorize]
public async Task<IActionResult> PermanentDeleteNote(int id)
{
    var note = await _context.Notes.FindAsync(id);
    if (note == null || note.UserId != GetUserId())
        return NotFound();

    // Delete workspace_items pointing to this note
    await _context.WorkspaceItems
        .Where(wi => wi.ItemType == 3 && wi.ItemId == id)
        .ExecuteDeleteAsync();

    // Delete note
    _context.Notes.Remove(note);
    await _context.SaveChangesAsync();

    return Ok(new { message = "Note permanently deleted" });
}


// =============================================
// FilesController.cs - Similar to NotesController
// =============================================

// Same pattern: /api/files/bin, /api/files/bin/{id}/restore, etc.


// =============================================
// FoldersController.cs
// =============================================

/// <summary>
/// Permanent delete folder + cascade children
/// Permission: Owner only
/// </summary>
[HttpDelete("api/folders/bin/{id}/permanent")]
[Authorize]
public async Task<IActionResult> PermanentDeleteFolder(
    int id,
    [FromQuery] bool deleteChildren = false)
{
    var folder = await _context.Folders.FindAsync(id);
    if (folder == null || folder.UserId != GetUserId())
        return NotFound();

    var result = await _folderService.PermanentDeleteFolderAsync(id, deleteChildren);

    return StatusCode(result.Status, result);
}
```

### Request DTOs

```csharp
public class MoveItemRequest
{
    public int ItemId { get; set; }
    public int FromWorkspaceId { get; set; }
    public int? NewParentId { get; set; }
}

public class CopyItemRequest
{
    public byte ItemType { get; set; }
    public int ItemId { get; set; }
    public int? TargetParentId { get; set; }
    public bool CopyChildren { get; set; } = false;  // For folders
}

public class CopyWorkspaceRequest
{
    public string NewWorkspaceName { get; set; } = string.Empty;
}
```

---

## 6️⃣ VALIDATION RULES - FINAL

### Core Validations

```csharp
// =============================================
// WorkspaceItemService.cs
// =============================================

public class WorkspaceItemService : IWorkspaceItemService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<WorkspaceItemService> _logger;

    /// <summary>
    /// Validation 1: Owner matching
    /// </summary>
    private async Task<bool> ValidateOwnerMatch(int workspaceId, int itemOwnerId)
    {
        var workspace = await _context.Workspaces.FindAsync(workspaceId);
        if (workspace == null) return false;

        return workspace.UserId == itemOwnerId;
    }

    /// <summary>
    /// Validation 2: Parent exists in workspace
    /// </summary>
    private async Task<bool> ValidateParentExists(int workspaceId, int? parentId)
    {
        if (!parentId.HasValue) return true;  // Root level OK

        return await _context.WorkspaceItems
            .AnyAsync(wi => wi.WorkspaceId == workspaceId
                         && wi.ItemType == 2  // folder
                         && wi.ItemId == parentId.Value
                         && wi.DeletedAt == null);
    }

    /// <summary>
    /// Validation 3: No duplicate
    /// </summary>
    private async Task<bool> ValidateNoDuplicate(
        int workspaceId, byte itemType, int itemId)
    {
        return !await _context.WorkspaceItems
            .AnyAsync(wi => wi.WorkspaceId == workspaceId
                         && wi.ItemType == itemType
                         && wi.ItemId == itemId
                         && wi.DeletedAt == null);
    }

    /// <summary>
    /// Validation 4: No circular reference
    /// </summary>
    private async Task<bool> ValidateNoCircularReference(
        int workspaceId, int itemId, int? newParentId)
    {
        if (!newParentId.HasValue) return true;

        var current = newParentId.Value;
        var visited = new HashSet<int>();

        while (current != 0)
        {
            if (current == itemId) return false;  // Circular!
            if (visited.Contains(current)) break;
            visited.Add(current);

            var parent = await _context.WorkspaceItems
                .Where(wi => wi.WorkspaceId == workspaceId
                          && wi.ItemType == 2
                          && wi.ItemId == current
                          && wi.DeletedAt == null)
                .Select(wi => wi.ParentId)
                .FirstOrDefaultAsync();

            current = parent ?? 0;
        }

        return true;
    }

    /// <summary>
    /// Add item to workspace with full validation
    /// </summary>
    public async Task<ResultOptions> AddItemToWorkspaceAsync(
        int workspaceId,
        byte itemType,
        int itemId,
        int? parentId = null)
    {
        try
        {
            // Get item and owner
            var item = await GetItemByTypeAndId(itemType, itemId);
            if (!item.Success)
                return item;

            var itemOwnerId = GetItemOwnerId(item.Object, itemType);

            // ✅ Validation 1: Owner matching
            if (!await ValidateOwnerMatch(workspaceId, itemOwnerId))
            {
                return new ResultOptions
                {
                    Success = false,
                    Message = "Cannot add item from different owner. Use Copy API instead.",
                    Status = 403
                };
            }

            // ✅ Validation 2: Parent exists
            if (!await ValidateParentExists(workspaceId, parentId))
            {
                return new ResultOptions
                {
                    Success = false,
                    Message = "Parent folder not found in this workspace",
                    Status = 400
                };
            }

            // ✅ Validation 3: No duplicate
            if (!await ValidateNoDuplicate(workspaceId, itemType, itemId))
            {
                return new ResultOptions
                {
                    Success = false,
                    Message = "Item already exists in this workspace",
                    Status = 409
                };
            }

            // ✅ Validation 4: No circular reference (for folders)
            if (itemType == 2 && !await ValidateNoCircularReference(workspaceId, itemId, parentId))
            {
                return new ResultOptions
                {
                    Success = false,
                    Message = "Operation would create circular reference",
                    Status = 400
                };
            }

            // All validations passed - create workspace_item
            var workspaceItem = new WorkspaceItemEntity
            {
                WorkspaceId = workspaceId,
                ParentId = parentId,
                ItemType = itemType,
                ItemId = itemId,
                CopyInfo = null,  // Not a copy, just link
                CreatedAt = DateTime.UtcNow
            };

            _context.WorkspaceItems.Add(workspaceItem);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Added item to workspace: workspace={WorkspaceId}, type={ItemType}, id={ItemId}",
                workspaceId, itemType, itemId
            );

            return new ResultOptions
            {
                Success = true,
                Message = "Item added to workspace successfully",
                Object = workspaceItem,
                Status = 201
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding item to workspace");
            return new ResultOptions
            {
                Success = false,
                Message = ex.Message,
                Status = 500
            };
        }
    }

    /// <summary>
    /// Helper: Get item owner ID
    /// </summary>
    private int GetItemOwnerId(object item, byte itemType)
    {
        return itemType switch
        {
            2 => ((Folder)item).UserId,
            3 => ((Note)item).UserId,
            4 => ((File)item).UserId,
            _ => throw new ArgumentException("Invalid item type")
        };
    }
}
```

---

## 7️⃣ IMPLEMENTATION CHECKLIST - UPDATED

### Phase 1: Database Migration (1 day)
- [ ] Run migration script (section 3)
- [ ] Verify schema changes on dev database
- [ ] Test rollback script
- [ ] Update EF Core models
- [ ] Update EF configurations
- [ ] Generate and test migrations

### Phase 2: Core Services (3 days)
- [ ] Update `WorkspaceItemService` with validations
- [ ] Implement `MoveItemToWorkspace`
- [ ] Implement `CopyItemToWorkspace` (same owner)
- [ ] Implement `CloneAndCopyItem` (cross owner)
- [ ] Implement `CopyFolderStructure` (BFS algorithm)
- [ ] Implement `PermanentDeleteFolderAsync` with cascade
- [ ] Unit tests for all methods

### Phase 3: API Endpoints (2 days)
- [ ] Workspace bin endpoints (GET, restore, permanent delete)
- [ ] Note/File bin endpoints (GET, restore, permanent delete)
- [ ] Move item endpoint
- [ ] Copy item endpoint
- [ ] Copy workspace endpoint
- [ ] Add validation to existing AddItem endpoint
- [ ] API integration tests

### Phase 4: Permission Layer (1 day)
- [ ] Implement `WorkspaceMemberService.HasPermission`
- [ ] Implement `WorkspaceMemberService.IsOwner`
- [ ] Add permission checks to all endpoints
- [ ] Test with different roles (owner/editor/viewer)

### Phase 5: Frontend Updates (4 days)
- [ ] Update workspace tree display (filter on client)
- [ ] Implement workspace bin UI
- [ ] Implement note/file bin UI (separate from workspace bin)
- [ ] Add "Move to workspace" dialog
- [ ] Add "Copy to workspace" dialog (show same/cross owner)
- [ ] Add "Copy structure" UI (for folders)
- [ ] Update delete confirmation dialogs (workspace vs global)
- [ ] Add restore buttons (workspace bin vs entity bin)
- [ ] E2E tests

### Phase 6: Documentation (1 day)
- [ ] Update API documentation
- [ ] Document bin types and restore behavior
- [ ] Document copy vs move
- [ ] Document permission matrix
- [ ] Create user guide with screenshots

**Total estimated effort:** 12 days

---

## 8️⃣ DECISIONS MADE

### ✅ Resolved Decisions

1. **Restore behavior** ✅
   - Workspace Bin restore → workspace_item only
   - Entity Bin restore → entity only
   - Two independent operations

2. **Permission matrix** ✅
   - Editor CAN restore workspace_item
   - Editor CANNOT delete entity
   - Owner only for permanent delete

3. **Frontend filtering** ✅
   - Load ALL workspace_items
   - Filter on client side
   - No stored procedure needed

4. **Move logic** ✅
   - Update workspace_id + parent_id
   - Validate same owner

5. **Copy logic** ✅
   - Same owner → Reuse entity (create workspace_item only)
   - Cross owner → Clone entity + create workspace_item

6. **CopyInfo design** ✅
   - Single JSON column in all tables
   - NULL for same owner
   - Populated for cross owner
   - Track structure copy relationships

### 🟡 Pending Decisions

7. **Duplicate items in workspace**
   - Option A: NO duplicate (current constraint) ← **RECOMMEND**
   - Option B: Allow duplicate (like shortcuts)
   - **Action:** Need confirmation

8. **Permanent delete folder default**
   - Should `deleteChildren` default to `true` or `false`?
   - **Recommend:** `false` (safer)
   - **Action:** Need confirmation

---

## 9️⃣ RISKS & MITIGATIONS - UPDATED

### Risk 1: Frontend performance with large datasets
**Scenario:** Workspace with 10,000+ items
**Impact:** Client-side filtering may be slow
**Mitigation:**
- Use virtual scrolling (react-window, virtuoso)
- Add pagination option
- Cache filtered results
- Monitor performance metrics

### Risk 2: Copy structure transaction failure
**Scenario:** Copying folder with 100+ children, fails at item 50
**Impact:** Partial copy, inconsistent state
**Mitigation:**
- Use database transaction (already in code)
- Add retry logic with exponential backoff
- Add cleanup for failed copies
- Monitor transaction timeout

### Risk 3: CopyInfo JSON deserialization errors
**Scenario:** Invalid JSON in copy_info column
**Impact:** API errors, display issues
**Mitigation:**
- Validate JSON before saving
- Add try-catch around deserialization
- Add fallback for invalid JSON
- Add database check constraint (optional)

### Risk 4: Permission check performance
**Scenario:** Check permission on every API call
**Impact:** Extra database queries
**Mitigation:**
- Cache workspace membership in memory
- Use Redis for distributed cache
- Add composite index on workspace_members
- Optimize query with joins

---

## 🎯 NEXT STEPS - UPDATED

### Immediate (This week)
1. ✅ Finalize this plan
2. 🟡 Get confirmation on pending decisions (#7, #8)
3. 🔜 Create detailed tickets in project board
4. 🔜 Set up test data for development

### Short term (Next 2 weeks)
1. **Week 1:** Phase 1-3 (Database, Services, APIs)
2. **Week 2:** Phase 4-5 (Permissions, Frontend)

### Medium term (Week 3)
1. **Phase 6:** Documentation
2. **QA Testing:** Full regression test
3. **Deployment:** Staging → Production

---

## 📊 SUMMARY OF CHANGES FROM PREVIOUS PLAN

### Removed/Simplified
- ❌ `is_original` column → Removed
- ❌ `sp_GetWorkspaceTree` → Not needed (frontend filtering)
- ❌ `source_id`, `source_user_id`, `copied_at` columns → Replaced with `copy_info`

### Added/Enhanced
- ✅ `copy_info` JSON column (5 tables)
- ✅ Multiple bin types (workspace bin vs entity bin)
- ✅ Editor can restore workspace_item
- ✅ Copy structure algorithm (BFS with transaction)
- ✅ Move vs Copy logic clarification
- ✅ CopyMetadata DTO with full schema

### Clarified
- ✅ Restore behavior (workspace vs entity)
- ✅ Permission matrix (editor rights)
- ✅ Frontend filtering approach
- ✅ Same owner vs cross owner copy

---

## ✅ FINAL READINESS CHECKLIST

- [x] All clarifications received from user
- [x] Database schema finalized
- [x] EF Core models updated
- [x] API endpoints designed
- [x] Validation rules defined
- [x] Permission matrix confirmed
- [x] Frontend approach decided
- [x] Copy/Move logic clarified
- [x] Edge cases identified and solved
- [x] Implementation checklist created
- [ ] Pending decisions confirmed (#7, #8)
- [ ] Test data prepared
- [ ] Development environment ready

**Status:** 95% READY - Cần confirm 2 decisions cuối

---

**End of Final Plan**

Generated by Claude Code Analysis
Date: 2025-12-25 (Final Version)
Ready for implementation after pending decisions confirmed.
