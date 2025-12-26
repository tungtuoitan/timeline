# Workspace System - Implementation Plan Review

**Ngày:** 2025-12-25
**Tác giả:** Claude Analysis
**Mục đích:** Review plan hiện tại và đưa ra các vấn đề cần giải quyết trước khi implement

---

## 📋 EXECUTIVE SUMMARY

### Đánh giá tổng quan
- ✅ **Plan tổng thể:** RẤT TỐT - Logic rõ ràng, đơn giản hóa complexity
- ⚠️ **Thiếu sót:** 6 vấn đề quan trọng cần bổ sung
- 🔥 **Khả thi:** CÓ - Nhưng cần implement validation và bổ sung schema

### Strengths
1. Simple ownership model (workspace.owner = file.owner)
2. Clear separation: workspace_item (structure) vs file/note (data)
3. Two-level delete (workspace-level vs global-level)
4. Copy model for cross-owner collaboration

### Critical Gaps
1. Missing validation: parent_id cross-workspace check
2. Missing schema: copy metadata tracking (source_id, copied_at)
3. Unclear logic: restore behavior, permission model
4. Performance: recursive parent filtering

---

## 1️⃣ CURRENT PLAN REVIEW

### Hiện trạng từ `update-workspace-feature-plan.md`

#### ✅ Đã rõ ràng
- Workspace không sở hữu dữ liệu (chỉ link qua workspace_item)
- workspace_item + item_id = IMMUTABLE pair
- Owner constraint: file.owner = workspace.owner
- Delete 2 cấp:
  - Workspace-level: `workspace_item.deletedAt = X` (ẩn khỏi workspace)
  - Global-level: `file.deletedAt = X` (soft delete vào Bin)

#### ⚠️ Chưa rõ ràng
- Restore behavior: workspace_item có tự động restore không?
- WorkspaceMember permissions: editor có quyền gì?
- Permanent delete: cascade logic cho children
- Copy metadata: tracking source file như thế nào?

---

## 2️⃣ EDGE CASES CẦN XỬ LÝ

### 🔴 CRITICAL - Must fix trước khi code

#### **EDGE CASE #1: Parent validation - Cross-workspace parent**

**Vấn đề:**
```csharp
// Database schema hiện tại
builder.HasOne(wi => wi.Folder)
    .WithMany(f => f.WorkspaceItems)
    .HasForeignKey(wi => wi.ParentId);
```
- `parent_id` là FK đến `folders.id` (global scope)
- KHÔNG đảm bảo parent folder thuộc cùng workspace!

**Scenario lỗi:**
```
Workspace W1: Folder A (folder_id = 10)
Workspace W2: Folder B (folder_id = 20)

User tạo: workspace_item {
  workspace_id: 1,
  parent_id: 20,     // ❌ Folder từ workspace khác!
  item_id: 30
}
```

**Giải pháp:**
```csharp
// Validation trong AddItemToWorkspace
if (parentId.HasValue)
{
    var parentInWorkspace = await _context.WorkspaceItems
        .AnyAsync(wi => wi.WorkspaceId == workspaceId
                     && wi.ItemType == 2  // folder
                     && wi.ItemId == parentId.Value
                     && wi.DeletedAt == null);

    if (!parentInWorkspace)
        return Error("Parent folder not found in this workspace");
}
```

**Priority:** 🔥 CRITICAL
**Effort:** Low (validation logic)

---

#### **EDGE CASE #2: Circular parent reference**

**Vấn đề:**
```
Folder A → parent = Folder B
Folder B → parent = Folder C
Folder C → parent = Folder A  // ❌ Circular!
```

**Giải pháp:**
```csharp
public async Task<bool> WouldCreateCycle(int itemId, int? newParentId)
{
    if (!newParentId.HasValue) return false;

    var current = newParentId.Value;
    var visited = new HashSet<int>();

    while (current != 0)
    {
        if (current == itemId) return true;  // Cycle!
        if (visited.Contains(current)) break;
        visited.Add(current);

        var parent = await GetParentId(current);
        current = parent ?? 0;
    }

    return false;
}

// Usage trong UpdateParent API
if (await WouldCreateCycle(itemId, newParentId))
    return Error("Circular reference detected");
```

**Priority:** 🔥 HIGH
**Effort:** Medium

---

#### **EDGE CASE #3: Owner matching validation**

**Vấn đề:**
Database schema không enforce `file.owner = workspace.owner`

**Giải pháp:**
```csharp
public async Task<ResultOptions> AddItemToWorkspace(
    int workspaceId, byte itemType, int itemId)
{
    var workspace = await GetWorkspaceById(workspaceId);
    var item = await GetItemByTypeAndId(itemType, itemId);

    // ✅ CRITICAL VALIDATION
    if (item.UserId != workspace.UserId)
    {
        return new ResultOptions
        {
            Success = false,
            Message = "Cannot add item from different owner. Use Copy API instead.",
            Status = 403
        };
    }

    // Proceed...
}
```

**Priority:** 🔥 CRITICAL
**Effort:** Low

---

### 🟡 IMPORTANT - Nên fix ngay

#### **EDGE CASE #4: Restore from Bin - Workspace relationship**

**Scenario:**
```
1. User delete File F1 trong workspace W1
   → workspace_item.deletedAt = X
2. User delete File F1 từ NoteGrid
   → file.deletedAt = X
3. User restore F1 từ Bin
   → file.deletedAt = NULL
```

**Vấn đề:**
- File đã restore nhưng workspace_item.deletedAt vẫn = X
- File KHÔNG tự động xuất hiện lại trong workspace

**Giải pháp đề xuất:**

**Option 1: Restore chỉ restore file gốc** (Recommend)
```
Behavior: User phải manually add lại file vào workspace
Ưu điểm: Simple, explicit, user có control
Nhược điểm: Thêm 1 step cho user
```

**Option 2: Restore file + auto-restore workspace_items**
```csharp
public async Task<ResultOptions> RestoreNoteAsync(int noteId)
{
    // 1. Restore note
    await _context.Notes
        .Where(n => n.Id == noteId)
        .ExecuteUpdateAsync(s => s
            .SetProperty(n => n.DeletedAt, (DateTime?)null)
            .SetProperty(n => n.UpdatedAt, DateTime.UtcNow));

    // 2. Auto-restore ALL workspace_items
    await _context.WorkspaceItems
        .Where(wi => wi.ItemType == 3 && wi.ItemId == noteId)
        .ExecuteUpdateAsync(s => s
            .SetProperty(wi => wi.DeletedAt, (DateTime?)null)
            .SetProperty(wi => wi.UpdatedAt, DateTime.UtcNow));
}
```

**Decision needed:** Chọn Option 1 hay 2?

**Priority:** 🟡 IMPORTANT
**Effort:** Medium

---

#### **EDGE CASE #5: Query performance - Recursive parent check**

**Vấn đề:**
Plan yêu cầu: "workspace_item có cha, ông nội bị deletedAt = x thì invalid"

Frontend phải check ancestor chain → N+1 query problem

**Giải pháp:**
Backend API trả flat list đã filter sẵn bằng Stored Procedure:

```sql
-- sp_GetWorkspaceTree.sql
CREATE OR ALTER PROCEDURE [ws].[sp_GetWorkspaceTree]
    @workspace_id INT
AS
BEGIN
    SET NOCOUNT ON;

    WITH RecursiveTree AS (
        -- Level 0: Root items (parent_id IS NULL or parent valid)
        SELECT
            wi.id,
            wi.workspace_id,
            wi.parent_id,
            wi.item_type,
            wi.item_id,
            wi.deleted_at,
            0 as level,
            CAST(wi.id AS NVARCHAR(MAX)) as path
        FROM ws.workspace_items wi
        WHERE wi.workspace_id = @workspace_id
          AND wi.deleted_at IS NULL
          AND (
              wi.parent_id IS NULL
              OR wi.parent_id IN (
                  SELECT item_id
                  FROM ws.workspace_items
                  WHERE workspace_id = @workspace_id
                    AND item_type = 2  -- folder
                    AND deleted_at IS NULL
              )
          )

        UNION ALL

        -- Recursive: Children
        SELECT
            wi.id,
            wi.workspace_id,
            wi.parent_id,
            wi.item_type,
            wi.item_id,
            wi.deleted_at,
            rt.level + 1,
            rt.path + '/' + CAST(wi.id AS NVARCHAR(MAX))
        FROM ws.workspace_items wi
        INNER JOIN RecursiveTree rt
            ON wi.parent_id = rt.item_id
        WHERE wi.deleted_at IS NULL
    )
    SELECT
        rt.*,
        -- Join with actual entities
        CASE rt.item_type
            WHEN 2 THEN f.name
            WHEN 3 THEN n.name
            WHEN 4 THEN fi.name
        END as name,
        CASE rt.item_type
            WHEN 2 THEN f.deleted_at
            WHEN 3 THEN n.deleted_at
            WHEN 4 THEN fi.deleted_at
        END as entity_deleted_at
    FROM RecursiveTree rt
    LEFT JOIN ws.folders f ON rt.item_type = 2 AND rt.item_id = f.id
    LEFT JOIN dbo.notes n ON rt.item_type = 3 AND rt.item_id = n.id
    LEFT JOIN ws.files fi ON rt.item_type = 4 AND rt.item_id = fi.id
    -- Filter out items with deleted entities
    WHERE (
        (rt.item_type = 2 AND f.deleted_at IS NULL) OR
        (rt.item_type = 3 AND n.deleted_at IS NULL) OR
        (rt.item_type = 4 AND fi.deleted_at IS NULL)
    )
    ORDER BY rt.level, rt.id;
END
```

**Priority:** 🟡 HIGH (for performance)
**Effort:** Medium

---

#### **EDGE CASE #6: WorkspaceMember Permissions**

**Vấn đề:**
Plan có WorkspaceMember (owner/editor/viewer) nhưng chưa rõ permission matrix

**Scenario:**
```
User A: workspace owner
User B: editor
User B delete File F1 trong workspace → ?
```

**Permission Matrix đề xuất:**

| Action | Owner | Editor | Viewer |
|--------|-------|--------|--------|
| View workspace items | ✅ | ✅ | ✅ |
| Add item to workspace | ✅ | ✅ | ❌ |
| Delete workspace_item | ✅ | ✅ | ❌ |
| Move item (reorder/reparent) | ✅ | ✅ | ❌ |
| Edit file/note content | ✅ | ✅ | ❌ |
| Soft delete file/note (to Bin) | ✅ | ❌ | ❌ |
| Restore from Bin | ✅ | ❌ | ❌ |
| Permanent delete | ✅ | ❌ | ❌ |
| Manage members | ✅ | ❌ | ❌ |

**Note:**
- Editor có thể "remove from workspace" (delete workspace_item)
- Editor KHÔNG được soft delete file gốc (chỉ owner)
- Bin operations chỉ owner

**Decision needed:** Confirm permission matrix

**Priority:** 🟡 IMPORTANT
**Effort:** Low (documentation + validation)

---

### 🔵 NICE TO HAVE

#### **EDGE CASE #7: Move item between workspaces (same owner)**

**Scenario:**
```
User A có workspace W1, W2
User A muốn move File F1 từ W1 sang W2
```

**Current constraint:** `UNIQUE (workspace_id, item_type, item_id)`
→ 1 file có thể xuất hiện ở nhiều workspace

**Behavior options:**

**Option A: Allow duplicate (recommend)**
```
File F1 có thể tồn tại trong cả W1 và W2
Move = Soft delete workspace_item ở W1, create mới ở W2
```

**Option B: Enforce single workspace**
```
File chỉ xuất hiện ở 1 workspace tại 1 thời điểm
Add unique constraint global
```

**Decision needed:** Option A or B?

**Priority:** 🔵 NICE TO HAVE
**Effort:** Low

---

#### **EDGE CASE #8: Copy metadata tracking**

**Vấn đề:**
Plan nói "Lưu metadata trỏ về file gốc" nhưng schema chưa có columns

**Giải pháp:**
```sql
-- Add to dbo.notes
ALTER TABLE dbo.notes ADD source_id INT NULL;
ALTER TABLE dbo.notes ADD source_user_id INT NULL;
ALTER TABLE dbo.notes ADD copied_at DATETIME2 NULL;
ALTER TABLE dbo.notes ADD copy_metadata NVARCHAR(MAX) NULL;  -- JSON

-- Add to ws.files
ALTER TABLE ws.files ADD source_id INT NULL;
ALTER TABLE ws.files ADD source_user_id INT NULL;
ALTER TABLE ws.files ADD copied_at DATETIME2 NULL;
ALTER TABLE ws.files ADD copy_metadata NVARCHAR(MAX) NULL;

-- Add to ws.folders
ALTER TABLE ws.folders ADD source_id INT NULL;
ALTER TABLE ws.folders ADD source_user_id INT NULL;
ALTER TABLE ws.folders ADD copied_at DATETIME2 NULL;
```

**Metadata JSON example:**
```json
{
  "sourceId": 123,
  "sourceUserId": 456,
  "sourceVersion": "v1.2.3",
  "copiedAt": "2025-12-25T10:30:00Z",
  "checksum": "abc123...",
  "lastSyncAt": null
}
```

**Priority:** 🟡 IMPORTANT (for future sync feature)
**Effort:** Low (schema change)

---

#### **EDGE CASE #9: Permanent delete folder - Cascade children**

**Vấn đề:**
Plan nói "permanent delete folder, các folder con cháu và workspace_item bị xóa"

Nhưng hierarchy được định nghĩa bởi workspace_item.parent_id, không phải folder.parent_id

**Scenario:**
```
Folder A (id=10)
  ├─ Folder B (id=20)
  │   └─ Note N1 (id=30)
  └─ Note N2 (id=40)
```

**Khi permanent delete Folder A, cần xóa:**
1. ✅ Folder A (folders table)
2. ✅ Workspace_item của Folder A
3. ❓ Folder B (con của A trong workspace tree)
4. ❓ Notes N1, N2
5. ❓ Workspace_items của children

**Giải pháp:**

```csharp
public async Task<ResultOptions> PermanentDeleteFolderAsync(int folderId, bool deleteChildren = false)
{
    // 1. Find all workspace_items pointing to this folder
    var folderWorkspaceItems = await _context.WorkspaceItems
        .Where(wi => wi.ItemType == 2 && wi.ItemId == folderId)
        .ToListAsync();

    if (deleteChildren)
    {
        // 2. For each workspace, recursively find descendants
        foreach (var folderWi in folderWorkspaceItems)
        {
            var descendants = await GetAllDescendantsRecursive(
                folderWi.WorkspaceId,
                folderWi.ItemId
            );

            // 3. Delete children workspace_items
            await _context.WorkspaceItems
                .Where(wi => descendants.Select(d => d.Id).Contains(wi.Id))
                .ExecuteDeleteAsync();

            // 4. Delete children folders (optional)
            var childFolderIds = descendants
                .Where(d => d.ItemType == 2)
                .Select(d => d.ItemId)
                .ToList();

            if (childFolderIds.Any())
            {
                await _context.Folders
                    .Where(f => childFolderIds.Contains(f.Id))
                    .ExecuteDeleteAsync();
            }
        }
    }

    // 5. Delete folder workspace_items
    await _context.WorkspaceItems
        .Where(wi => wi.ItemType == 2 && wi.ItemId == folderId)
        .ExecuteDeleteAsync();

    // 6. Delete folder
    await _context.Folders
        .Where(f => f.Id == folderId)
        .ExecuteDeleteAsync();

    return Success("Folder permanently deleted");
}

// Recursive helper
private async Task<List<WorkspaceItemEntity>> GetAllDescendantsRecursive(
    int workspaceId, int parentItemId)
{
    var result = new List<WorkspaceItemEntity>();
    var queue = new Queue<int>();
    queue.Enqueue(parentItemId);

    while (queue.Count > 0)
    {
        var currentParent = queue.Dequeue();

        var children = await _context.WorkspaceItems
            .Where(wi => wi.WorkspaceId == workspaceId
                      && wi.ParentId == currentParent)
            .ToListAsync();

        result.AddRange(children);

        // Add folder children to queue
        foreach (var child in children.Where(c => c.ItemType == 2))
        {
            queue.Enqueue(child.ItemId);
        }
    }

    return result;
}
```

**Decision needed:**
- Default behavior: delete children or not?
- API có parameter `deleteChildren: boolean`?

**Priority:** 🟡 IMPORTANT
**Effort:** High

---

## 3️⃣ DATABASE SCHEMA CHANGES

### Required Changes

```sql
-- ========================================
-- 1. REMOVE is_original column
-- ========================================
ALTER TABLE ws.workspace_items
DROP COLUMN is_original;

-- Update EF Core configuration
-- File: WorkspaceItemConfiguration.cs
-- Remove: builder.Property(wi => wi.IsOriginal)...


-- ========================================
-- 2. ADD copy metadata tracking
-- ========================================

-- Notes
ALTER TABLE dbo.notes ADD source_id INT NULL;
ALTER TABLE dbo.notes ADD source_user_id INT NULL;
ALTER TABLE dbo.notes ADD copied_at DATETIME2 NULL;
ALTER TABLE dbo.notes ADD copy_metadata NVARCHAR(MAX) NULL;

CREATE INDEX IX_notes_source ON dbo.notes(source_id) WHERE source_id IS NOT NULL;

-- Files
ALTER TABLE ws.files ADD source_id INT NULL;
ALTER TABLE ws.files ADD source_user_id INT NULL;
ALTER TABLE ws.files ADD copied_at DATETIME2 NULL;
ALTER TABLE ws.files ADD copy_metadata NVARCHAR(MAX) NULL;

CREATE INDEX IX_files_source ON ws.files(source_id) WHERE source_id IS NOT NULL;

-- Folders
ALTER TABLE ws.folders ADD source_id INT NULL;
ALTER TABLE ws.folders ADD source_user_id INT NULL;
ALTER TABLE ws.folders ADD copied_at DATETIME2 NULL;

CREATE INDEX IX_folders_source ON ws.folders(source_id) WHERE source_id IS NOT NULL;


-- ========================================
-- 3. ADD performance indexes
-- ========================================

-- Improve parent lookup performance
CREATE INDEX IX_workspace_items_parent_lookup
ON ws.workspace_items(workspace_id, parent_id, deleted_at)
WHERE deleted_at IS NULL;

-- Improve item filtering
CREATE INDEX IX_workspace_items_item_lookup
ON ws.workspace_items(item_type, item_id, deleted_at)
WHERE deleted_at IS NULL;
```

### EF Core Model Updates

```csharp
// Note.cs
public class Note : ITimestampEntity
{
    // Existing...

    // Copy tracking
    public int? SourceId { get; set; }
    public int? SourceUserId { get; set; }
    public DateTime? CopiedAt { get; set; }
    public string? CopyMetadata { get; set; }
}

// File.cs
public class File : ITimestampEntity
{
    // Existing...

    // Copy tracking
    public int? SourceId { get; set; }
    public int? SourceUserId { get; set; }
    public DateTime? CopiedAt { get; set; }
    public string? CopyMetadata { get; set; }
}

// Folder.cs
public class Folder : ITimestampEntity
{
    // Existing...

    // Copy tracking
    public int? SourceId { get; set; }
    public int? SourceUserId { get; set; }
    public DateTime? CopiedAt { get; set; }
}

// WorkspaceItemEntity.cs
public class WorkspaceItemEntity : ITimestampEntity
{
    // REMOVE:
    // public bool IsOriginal { get; set; } = true;

    // Existing properties remain...
}
```

---

## 4️⃣ VALIDATION RULES

### Application Layer Validation

```csharp
// File: WorkspaceItemService.cs

public async Task<ResultOptions> AddItemToWorkspaceAsync(
    int workspaceId,
    byte itemType,
    int itemId,
    int? parentId = null)
{
    // ========================================
    // Validation 1: Owner matching
    // ========================================
    var workspace = await _workspaceRepo.GetWorkspaceById(workspaceId);
    if (!workspace.Success)
        return workspace;

    var item = await GetItemByTypeAndId(itemType, itemId);
    if (!item.Success)
        return item;

    var workspaceEntity = (Workspace)workspace.Object;

    // Extract owner from polymorphic item
    var itemOwnerId = itemType switch
    {
        2 => ((Folder)item.Object).UserId,
        3 => ((Note)item.Object).UserId,
        4 => ((File)item.Object).UserId,
        _ => throw new ArgumentException("Invalid item type")
    };

    if (itemOwnerId != workspaceEntity.UserId)
    {
        return new ResultOptions
        {
            Success = false,
            Message = "Cannot add item from different owner. Use Copy API instead.",
            Status = 403
        };
    }


    // ========================================
    // Validation 2: Parent exists in workspace
    // ========================================
    if (parentId.HasValue)
    {
        var parentExists = await _context.WorkspaceItems
            .AnyAsync(wi => wi.WorkspaceId == workspaceId
                         && wi.ItemType == 2  // folder
                         && wi.ItemId == parentId.Value
                         && wi.DeletedAt == null);

        if (!parentExists)
        {
            return new ResultOptions
            {
                Success = false,
                Message = "Parent folder not found in this workspace",
                Status = 400
            };
        }
    }


    // ========================================
    // Validation 3: No duplicate
    // ========================================
    var duplicate = await _context.WorkspaceItems
        .AnyAsync(wi => wi.WorkspaceId == workspaceId
                     && wi.ItemType == itemType
                     && wi.ItemId == itemId
                     && wi.DeletedAt == null);

    if (duplicate)
    {
        return new ResultOptions
        {
            Success = false,
            Message = "Item already exists in this workspace",
            Status = 409
        };
    }


    // ========================================
    // Validation 4: Circular reference (if moving)
    // ========================================
    if (parentId.HasValue && itemType == 2)  // Moving folder
    {
        if (await WouldCreateCycle(workspaceId, itemId, parentId.Value))
        {
            return new ResultOptions
            {
                Success = false,
                Message = "Operation would create circular reference",
                Status = 400
            };
        }
    }


    // All validations passed - create workspace_item
    var workspaceItem = new WorkspaceItemEntity
    {
        WorkspaceId = workspaceId,
        ParentId = parentId,
        ItemType = itemType,
        ItemId = itemId,
        CreatedAt = DateTime.UtcNow
    };

    _context.WorkspaceItems.Add(workspaceItem);
    await _context.SaveChangesAsync();

    return new ResultOptions
    {
        Success = true,
        Message = "Item added to workspace successfully",
        Object = workspaceItem,
        Status = 201
    };
}


// ========================================
// Helper: Check circular reference
// ========================================
private async Task<bool> WouldCreateCycle(int workspaceId, int itemId, int newParentId)
{
    var current = newParentId;
    var visited = new HashSet<int>();

    while (current != 0)
    {
        if (current == itemId)
            return true;  // Cycle detected!

        if (visited.Contains(current))
            break;  // Already checked

        visited.Add(current);

        // Get parent of current
        var parent = await _context.WorkspaceItems
            .Where(wi => wi.WorkspaceId == workspaceId
                      && wi.ItemType == 2  // folder
                      && wi.ItemId == current
                      && wi.DeletedAt == null)
            .Select(wi => wi.ParentId)
            .FirstOrDefaultAsync();

        current = parent ?? 0;
    }

    return false;
}


// ========================================
// Helper: Get item by type
// ========================================
private async Task<ResultOptions> GetItemByTypeAndId(byte itemType, int itemId)
{
    return itemType switch
    {
        2 => await GetFolderById(itemId),
        3 => await GetNoteById(itemId),
        4 => await GetFileById(itemId),
        _ => new ResultOptions
        {
            Success = false,
            Message = "Invalid item type",
            Status = 400
        }
    };
}
```

---

## 5️⃣ API CHANGES

### New APIs Required

```csharp
// ========================================
// 1. Copy file/note/folder cross-owner
// ========================================
[HttpPost("api/files/{id}/copy")]
public async Task<IActionResult> CopyFile(
    int id,
    [FromBody] CopyItemRequest request)
{
    /*
    Request body:
    {
      "targetWorkspaceId": 123,
      "targetParentId": 456,  // optional
      "copyChildren": true    // for folders
    }

    Creates new file with:
    - owner = target workspace owner
    - source_id = original file id
    - source_user_id = original owner
    - copied_at = now
    */
}

[HttpPost("api/notes/{id}/copy")]
public async Task<IActionResult> CopyNote(
    int id,
    [FromBody] CopyItemRequest request)
{
    // Same as CopyFile
}

[HttpPost("api/folders/{id}/copy")]
public async Task<IActionResult> CopyFolder(
    int id,
    [FromBody] CopyItemRequest request)
{
    // Recursive copy with children if copyChildren=true
}


// ========================================
// 2. Get workspace tree (pre-filtered)
// ========================================
[HttpGet("api/workspaces/{id}/tree")]
public async Task<IActionResult> GetWorkspaceTree(int id)
{
    /*
    Returns flat list with:
    - All workspace_items filtered
    - Invalid items removed (deleted parent, deleted entity)
    - Joined with entity data (name, etc)

    Response:
    [
      {
        "id": 1,
        "workspaceId": 1,
        "parentId": null,
        "itemType": 2,
        "itemId": 10,
        "level": 0,
        "path": "1",
        "name": "Folder A",
        "entityDeletedAt": null
      },
      ...
    ]
    */
}


// ========================================
// 3. Move item between workspaces
// ========================================
[HttpPost("api/workspaces/{id}/items/move")]
public async Task<IActionResult> MoveItem(
    int id,
    [FromBody] MoveItemRequest request)
{
    /*
    Request body:
    {
      "itemType": 3,
      "itemId": 789,
      "fromWorkspaceId": 1,
      "toWorkspaceId": 2,
      "newParentId": 10  // optional
    }

    Validates:
    - Both workspaces have same owner
    - Item owner matches workspace owner

    Operations:
    - Soft delete workspace_item in fromWorkspace
    - Create workspace_item in toWorkspace
    */
}


// ========================================
// 4. Restore from bin with options
// ========================================
[HttpPost("api/bin/notes/{id}/restore")]
public async Task<IActionResult> RestoreNote(
    int id,
    [FromBody] RestoreItemRequest request)
{
    /*
    Request body:
    {
      "restoreToWorkspaces": [1, 2]  // optional
    }

    Operations:
    - Set note.deletedAt = null
    - Option 1: Also restore workspace_items
    - Option 2: User manually adds to workspaces

    DECISION NEEDED!
    */
}


// ========================================
// 5. Permanent delete with cascade
// ========================================
[HttpDelete("api/bin/folders/{id}/permanent")]
public async Task<IActionResult> PermanentDeleteFolder(
    int id,
    [FromQuery] bool deleteChildren = false)
{
    /*
    Operations:
    - Delete folder from folders table
    - Delete all workspace_items pointing to folder
    - If deleteChildren=true:
      - Recursively delete children folders
      - Delete children workspace_items
      - Optionally delete children notes/files

    DECISION NEEDED: Default deleteChildren?
    */
}


// ========================================
// 6. Update parent (move within workspace)
// ========================================
[HttpPatch("api/workspaces/{workspaceId}/items/{itemId}/parent")]
public async Task<IActionResult> UpdateParent(
    int workspaceId,
    int itemId,
    [FromBody] UpdateParentRequest request)
{
    /*
    Request body:
    {
      "newParentId": 20  // or null for root
    }

    Validates:
    - Parent exists in workspace
    - No circular reference (for folders)
    */
}
```

### Modified APIs

```csharp
// WorkspaceListController.cs - Add validation

[HttpPost("api/workspaces/{id}/items")]
public async Task<IActionResult> AddItemToWorkspace(
    int id,
    [FromBody] AddItemRequest request)
{
    // ADD validation from section 4
    var result = await _workspaceItemService.AddItemToWorkspaceAsync(
        id,
        request.ItemType,
        request.ItemId,
        request.ParentId
    );

    return StatusCode(result.Status, result);
}
```

---

## 6️⃣ STORED PROCEDURES

### New Stored Procedures

```sql
-- ========================================
-- sp_GetWorkspaceTree
-- ========================================
CREATE OR ALTER PROCEDURE [ws].[sp_GetWorkspaceTree]
    @workspace_id INT
AS
BEGIN
    SET NOCOUNT ON;

    WITH RecursiveTree AS (
        -- Level 0: Root items
        SELECT
            wi.id,
            wi.workspace_id,
            wi.parent_id,
            wi.item_type,
            wi.item_id,
            wi.deleted_at,
            0 as level,
            CAST(wi.id AS NVARCHAR(MAX)) as path
        FROM ws.workspace_items wi
        WHERE wi.workspace_id = @workspace_id
          AND wi.deleted_at IS NULL
          AND (
              wi.parent_id IS NULL
              OR wi.parent_id IN (
                  SELECT item_id
                  FROM ws.workspace_items
                  WHERE workspace_id = @workspace_id
                    AND item_type = 2
                    AND deleted_at IS NULL
              )
          )

        UNION ALL

        -- Recursive: Children
        SELECT
            wi.id,
            wi.workspace_id,
            wi.parent_id,
            wi.item_type,
            wi.item_id,
            wi.deleted_at,
            rt.level + 1,
            rt.path + '/' + CAST(wi.id AS NVARCHAR(MAX))
        FROM ws.workspace_items wi
        INNER JOIN RecursiveTree rt
            ON wi.parent_id = rt.item_id
        WHERE wi.deleted_at IS NULL
    )
    SELECT
        rt.*,
        -- Join with actual entities
        CASE rt.item_type
            WHEN 2 THEN f.name
            WHEN 3 THEN n.name
            WHEN 4 THEN fi.name
        END as name,
        CASE rt.item_type
            WHEN 2 THEN f.deleted_at
            WHEN 3 THEN n.deleted_at
            WHEN 4 THEN fi.deleted_at
        END as entity_deleted_at,
        CASE rt.item_type
            WHEN 2 THEN f.user_id
            WHEN 3 THEN n.user_id
            WHEN 4 THEN fi.user_id
        END as entity_owner_id
    FROM RecursiveTree rt
    LEFT JOIN ws.folders f ON rt.item_type = 2 AND rt.item_id = f.id
    LEFT JOIN dbo.notes n ON rt.item_type = 3 AND rt.item_id = n.id
    LEFT JOIN ws.files fi ON rt.item_type = 4 AND rt.item_id = fi.id
    -- Filter out items with deleted entities
    WHERE (
        (rt.item_type = 2 AND f.deleted_at IS NULL) OR
        (rt.item_type = 3 AND n.deleted_at IS NULL) OR
        (rt.item_type = 4 AND fi.deleted_at IS NULL)
    )
    ORDER BY rt.level, rt.path;
END
GO


-- ========================================
-- sp_PermanentDeleteFolder (update)
-- ========================================
CREATE OR ALTER PROCEDURE [ws].[sp_PermanentDeleteFolder]
    @folder_id INT,
    @delete_children BIT = 0,
    @deleted_count INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        SET @deleted_count = 0;

        -- Find all workspace_items pointing to this folder
        CREATE TABLE #FolderWorkspaceItems (
            id INT,
            workspace_id INT,
            item_id INT
        );

        INSERT INTO #FolderWorkspaceItems
        SELECT id, workspace_id, item_id
        FROM ws.workspace_items
        WHERE item_type = 2 AND item_id = @folder_id;

        IF @delete_children = 1
        BEGIN
            -- Recursive delete children
            CREATE TABLE #DescendantsToDelete (
                id INT,
                workspace_id INT,
                item_type TINYINT,
                item_id INT,
                level INT
            );

            -- For each workspace this folder appears in
            DECLARE @ws_id INT, @wi_item_id INT;
            DECLARE ws_cursor CURSOR FOR
                SELECT workspace_id, item_id FROM #FolderWorkspaceItems;

            OPEN ws_cursor;
            FETCH NEXT FROM ws_cursor INTO @ws_id, @wi_item_id;

            WHILE @@FETCH_STATUS = 0
            BEGIN
                -- Recursive CTE to find all descendants
                WITH Descendants AS (
                    SELECT id, workspace_id, item_type, item_id, 0 as level
                    FROM ws.workspace_items
                    WHERE workspace_id = @ws_id
                      AND parent_id = @wi_item_id

                    UNION ALL

                    SELECT wi.id, wi.workspace_id, wi.item_type, wi.item_id, d.level + 1
                    FROM ws.workspace_items wi
                    INNER JOIN Descendants d ON wi.parent_id = d.item_id
                    WHERE wi.workspace_id = @ws_id
                )
                INSERT INTO #DescendantsToDelete
                SELECT * FROM Descendants;

                FETCH NEXT FROM ws_cursor INTO @ws_id, @wi_item_id;
            END

            CLOSE ws_cursor;
            DEALLOCATE ws_cursor;

            -- Delete children workspace_items
            DELETE wi
            FROM ws.workspace_items wi
            INNER JOIN #DescendantsToDelete d ON wi.id = d.id;

            -- Delete children folders (item_type = 2)
            DELETE f
            FROM ws.folders f
            INNER JOIN #DescendantsToDelete d
                ON d.item_type = 2 AND d.item_id = f.id;

            DROP TABLE #DescendantsToDelete;
        END

        -- Delete folder workspace_items
        DELETE FROM ws.workspace_items
        WHERE item_type = 2 AND item_id = @folder_id;

        -- Delete folder itself
        DELETE FROM ws.folders WHERE id = @folder_id;

        SET @deleted_count = @@ROWCOUNT;

        DROP TABLE #FolderWorkspaceItems;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;

        DECLARE @msg NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@msg, 16, 1);
    END CATCH
END
GO
```

---

## 7️⃣ IMPLEMENTATION CHECKLIST

### Phase 1: Database Changes (1 day)
- [ ] Remove `is_original` column from `ws.workspace_items`
- [ ] Add copy tracking columns to `notes`, `files`, `folders`
- [ ] Add performance indexes
- [ ] Update EF Core models and configurations
- [ ] Create migration script
- [ ] Test migration on dev database

### Phase 2: Core Validation (2 days)
- [ ] Implement owner matching validation
- [ ] Implement parent existence validation
- [ ] Implement circular reference check
- [ ] Implement duplicate check
- [ ] Unit tests for all validations

### Phase 3: Stored Procedures (1 day)
- [ ] Create `sp_GetWorkspaceTree`
- [ ] Update `sp_PermanentDeleteFolder` with children cascade
- [ ] Test with sample data
- [ ] Benchmark performance

### Phase 4: Repository Layer (2 days)
- [ ] Update `WorkspaceListRepository` with validations
- [ ] Implement `GetWorkspaceTreeAsync` using SP
- [ ] Implement `CopyNoteAsync`, `CopyFileAsync`, `CopyFolderAsync`
- [ ] Implement `PermanentDeleteWithCascadeAsync`
- [ ] Unit tests

### Phase 5: Service Layer (2 days)
- [ ] Update `WorkspaceListService`
- [ ] Update `NoteService`
- [ ] Implement permission checking with `WorkspaceMember`
- [ ] Integration tests

### Phase 6: API Endpoints (2 days)
- [ ] Add validation to existing `AddItem` API
- [ ] Create `POST /api/files/{id}/copy`
- [ ] Create `POST /api/notes/{id}/copy`
- [ ] Create `POST /api/folders/{id}/copy`
- [ ] Create `GET /api/workspaces/{id}/tree`
- [ ] Create `POST /api/workspaces/{id}/items/move`
- [ ] Create `PATCH /api/workspaces/{id}/items/{id}/parent`
- [ ] Update `POST /api/bin/{type}/{id}/restore`
- [ ] Update `DELETE /api/bin/folders/{id}/permanent`
- [ ] API tests

### Phase 7: Frontend Updates (3 days)
- [ ] Update workspace tree display to use new API
- [ ] Add "Copy to workspace" UI
- [ ] Add "Move to workspace" UI
- [ ] Add "Delete children" confirmation dialog
- [ ] Update restore UX (auto-add to workspace or manual?)
- [ ] E2E tests

### Phase 8: Documentation (1 day)
- [ ] Update API documentation
- [ ] Document permission matrix
- [ ] Document delete behavior (workspace vs global)
- [ ] Document copy metadata format
- [ ] Create user guide

**Total estimated effort:** 14 days

---

## 8️⃣ DECISIONS NEEDED

### 🔴 Critical Decisions (Must decide before implementation)

1. **Restore behavior**
   - Option A: Restore file only, user manually re-add to workspace
   - Option B: Auto-restore all workspace_items when restore file
   - **Recommendation:** Option A (simpler, clearer UX)

2. **Permission matrix for WorkspaceMember**
   - Confirm the table in EDGE CASE #6
   - Specifically: Can editor delete file gốc or only workspace_item?
   - **Recommendation:** Editor can only delete workspace_item

3. **Permanent delete folder default**
   - Should `deleteChildren` default to `true` or `false`?
   - **Recommendation:** `false` (safer), with UI warning

4. **Move between workspaces**
   - Option A: Allow file in multiple workspaces (duplicate workspace_items)
   - Option B: Enforce single workspace (add global unique constraint)
   - **Recommendation:** Option A (more flexible, like Google Drive)

### 🟡 Nice-to-have Decisions (Can decide during implementation)

5. **Copy metadata format**
   - What fields to include in `copy_metadata` JSON?
   - Suggest: `{sourceId, sourceUserId, sourceVersion, copiedAt, checksum, lastSyncAt}`

6. **Query optimization**
   - Use stored procedure or EF Core for workspace tree?
   - **Recommendation:** Stored procedure (better performance)

---

## 9️⃣ RISKS & MITIGATIONS

### Risk 1: Performance degradation with deep hierarchies
**Mitigation:**
- Use stored procedure with CTE for recursive queries
- Add composite indexes on `(workspace_id, parent_id, deleted_at)`
- Benchmark with 10,000+ items

### Risk 2: Data inconsistency (orphaned workspace_items)
**Mitigation:**
- Implement cascade delete properly
- Add background job to clean orphaned workspace_items
- Add database constraint where possible

### Risk 3: Migration breaks existing data
**Mitigation:**
- Test migration on copy of production database
- Create rollback script
- Deploy during low-traffic window

### Risk 4: Complex validation slows down API
**Mitigation:**
- Cache workspace ownership checks
- Use database indexes effectively
- Consider async validation for non-critical checks

---

## 🎯 NEXT STEPS

### Immediate (This week)
1. **Review and approve this plan** with team
2. **Make decisions** on critical items (section 8)
3. **Create detailed tickets** for each phase
4. **Set up dev environment** with test data

### Short term (Next week)
1. **Phase 1:** Database changes
2. **Phase 2:** Core validation
3. **Phase 3:** Stored procedures

### Medium term (Week 3-4)
1. **Phase 4-6:** Repository, Service, API
2. **Phase 7:** Frontend updates
3. **Phase 8:** Documentation

---

## 📚 REFERENCES

- Original plan: `update-workspace-feature-plan.md`
- Database schema: `migrations/REBUILD_SIMPLIFIED_SCHEMA.sql`
- Current delete SP: `migrations/sp_DeleteWorkspace.sql`
- Models: `SuperAppModels/Models/Workspace*.cs`

---

**End of Review**

Generated by Claude Code Analysis
Date: 2025-12-25
