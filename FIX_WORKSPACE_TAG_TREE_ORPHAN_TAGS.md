# Fix GetWorkspaceTagTree - Include Orphan Tags

## Problem

`GetWorkspaceTagTree` không lấy ra các tags có `parent_tag_id = NULL` và không có children (orphan tags).

### Missing Data

```
item_id | workspace_id | parent_tag_id | child_type | child_id
--------|--------------|---------------|------------|----------
16      | 1            | NULL          | tag        | 137      ❌ Missing
17      | 1            | NULL          | tag        | 127      ❌ Missing
```

## Root Cause

Stored procedure `usp_s_tag_tree` hiện tại chỉ lấy tags có children (để build hierarchy):

```sql
-- Root level tags
WHERE wi.item_type = 'tag'
AND EXISTS (
    -- ❌ SAI: Tìm tags là PARENT (có children)
    SELECT 1 FROM workspace_items wsi
    WHERE wsi.parent_tag_id = wi.item_id
)
```

**Logic SAI:**
- Workspace giống như một **virtual root container**
- Root level = Tags được add **TRỰC TIẾP** vào workspace (`parent_tag_id = NULL`)
- Logic hiện tại tìm tags **CÓ CHILDREN** thay vì tags **ĐƯỢC ADD vào workspace**

**Kết quả:**
- Item 16, 17 có `parent_tag_id = NULL` (là root) nhưng KHÔNG có children → Bị loại

## Solution Options

### Option 1: Tags explicitly added to workspace (RECOMMENDED)

**Concept:** Workspace = Virtual Root Container

Root level = Tags được add **TRỰC TIẾP** vào workspace (không thông qua parent tag nào)

```sql
WITH TagHierarchy AS (
    -- ✅ ROOT LEVEL: Tags added DIRECTLY to workspace (parent_tag_id IS NULL)
    SELECT 
        wi.item_id,
        wi.name,
        wi.slug,
        ...
        CAST(NULL AS INT) AS parent_tag_id
    FROM #WorkspaceItems wi
    WHERE wi.item_type = 'tag'
    AND EXISTS (
        -- ✅ Tag được add vào workspace
        SELECT 1 FROM workspace_items wsi
        WHERE wsi.child_id = wi.item_id
        AND wsi.child_type = 'tag'
        AND wsi.workspace_id = @workspace_id
        AND wsi.parent_tag_id IS NULL        -- ✅ TRỰC TIẾP (không có parent)
        AND wsi.deleted_at IS NULL
    )
    
    UNION ALL
    
    -- Recursive children (unchanged)
    SELECT ...
)
```

**Kết quả với data của bạn:**
```
Workspace 1 (Virtual Root)
├─ Tag 137 (item 16) ✅ parent_tag_id = NULL → Root level
├─ Tag 127 (item 17) ✅ parent_tag_id = NULL → Root level  
└─ Tag 126 (????)    ✅ Nếu có record với parent_tag_id = NULL
   ├─ Tag 127 (item 1) → Child của tag 126
   └─ Tag 150 (item 6) → Child của tag 126
```

### Option 2: ALL tags that are not children of other tags

Lấy tất cả tags KHÔNG là children (kể cả chưa được add vào workspace):

```sql
AND NOT EXISTS (
    -- Tag is NOT a child of another tag in this workspace
    SELECT 1 FROM workspace_items wsi2
    WHERE wsi2.child_type = 'tag'
    AND wsi2.child_id = wi.item_id
    AND wsi2.workspace_id = @workspace_id
    AND wsi2.deleted_at IS NULL
)
```

**Note:** Option này sẽ lấy cả tags chưa được add vào workspace.

## Recommended Fix

**Use Option 1** - Tags explicitly added to workspace (Workspace as Virtual Root Container)

**Logic:**
1. **Workspace** = Virtual Root Container
2. **Root Level Tags** = Tags với `parent_tag_id = NULL` trong `workspace_items`
3. **Children** = Tags/Notes với `parent_tag_id = tag_id` của parent

**Ví dụ:**
```
Workspace 1
├─ Tag 137 ← item 16 (parent_tag_id = NULL)
├─ Tag 127 ← item 17 (parent_tag_id = NULL)
└─ Tag 126 ← (nếu có record với parent_tag_id = NULL)
   ├─ Tag 127 ← item 1 (parent_tag_id = 126)
   └─ Tag 150 ← item 6 (parent_tag_id = 126)
```

### Updated Stored Procedure

```sql
CREATE OR ALTER PROCEDURE usp_s_tag_tree
    @workspace_id INT,
    @user_id INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Check access to workspace
    IF NOT EXISTS (
        SELECT 1 FROM workspace_members
        WHERE workspace_id = @workspace_id
        AND user_id = @user_id
        AND deleted_at IS NULL
        AND invitation_status = 'active'
    )
    BEGIN
        RAISERROR('Access denied or workspace not found', 16, 1);
        RETURN;
    END;

    -- Create temp table with all items for this workspace
    CREATE TABLE #WorkspaceItems (
        item_id INT,
        name NVARCHAR(255),
        slug NVARCHAR(255),
        color NVARCHAR(7),
        icon NVARCHAR(50),
        description NVARCHAR(MAX),
        usage_count INT,
        item_type NVARCHAR(10)
    );

    -- Insert tags
    INSERT INTO #WorkspaceItems
    SELECT 
        t.tag_id, t.name, t.slug, t.color, t.icon, t.description, t.usage_count, 'tag'
    FROM tags t
    WHERE t.user_id = @user_id AND t.deleted_at IS NULL;

    -- Insert notes
    INSERT INTO #WorkspaceItems
    SELECT 
        n.note_id, n.name, n.slug, n.color, n.icon, n.description, 0, 'note'
    FROM notes n
    WHERE n.user_id = @user_id AND n.deleted_at IS NULL;

    -- Build hierarchy with CTE
    WITH TagHierarchy AS (
        -- ✅ ROOT LEVEL: Tags added DIRECTLY to workspace
        -- (Tags with parent_tag_id = NULL in workspace_items)
        SELECT 
            wi.item_id,
            wi.name,
            wi.slug,
            wi.color,
            wi.icon,
            wi.description,
            wi.usage_count,
            wi.item_type,
            0 AS depth,
            CAST('/' + CAST(wi.item_id AS VARCHAR(10)) + '/' AS NVARCHAR(4000)) AS path,
            CAST(wi.name AS NVARCHAR(4000)) AS breadcrumb,
            COALESCE(wsi_root.sort_order, 0) AS sort_order,
            CAST(NULL AS INT) AS parent_tag_id
        FROM #WorkspaceItems wi
        INNER JOIN workspace_items wsi_root 
            ON wsi_root.child_id = wi.item_id 
            AND wsi_root.child_type = 'tag'
        WHERE wi.item_type = 'tag'
        AND wsi_root.workspace_id = @workspace_id
        AND wsi_root.parent_tag_id IS NULL        -- ✅ Tags added DIRECTLY to workspace
        AND wsi_root.deleted_at IS NULL

        UNION ALL

        -- Recursive part: child items
        SELECT
            wi_child.item_id,
            wi_child.name,
            wi_child.slug,
            wi_child.color,
            wi_child.icon,
            wi_child.description,
            wi_child.usage_count,
            wi_child.item_type,
            th.depth + 1 AS depth,
            CAST(th.path + CAST(wi_child.item_id AS VARCHAR(10)) + '/' AS NVARCHAR(4000)) AS path,
            CAST(th.breadcrumb + ' > ' + wi_child.name AS NVARCHAR(4000)) AS breadcrumb,
            wsi.sort_order,
            wsi.parent_tag_id
        FROM TagHierarchy th
        INNER JOIN workspace_items wsi ON th.item_id = wsi.parent_tag_id
        INNER JOIN #WorkspaceItems wi_child ON wsi.child_id = wi_child.item_id AND wsi.child_type = wi_child.item_type
        WHERE wsi.workspace_id = @workspace_id
        AND wsi.deleted_at IS NULL
        AND th.depth < 10 -- Prevent infinite recursion
    )
    SELECT
        item_id AS id,
        @user_id AS user_id,
        name,
        slug,
        color,
        icon,
        description,
        usage_count,
        item_type,
        depth AS level,
        path,
        breadcrumb,
        sort_order,
        CASE 
            WHEN item_type = 'tag' THEN 
                (SELECT COUNT(*) FROM workspace_items wi_child 
                 WHERE wi_child.parent_tag_id = item_id 
                 AND wi_child.workspace_id = @workspace_id 
                 AND wi_child.deleted_at IS NULL)
            ELSE 0 
        END AS children_count,
        parent_tag_id AS parent_id,
        'owner' AS access_type
    FROM TagHierarchy
    ORDER BY depth, sort_order, name;

    DROP TABLE #WorkspaceItems;
END;
GO
```

## Testing

After applying the fix:

```sql
-- Should now return ALL tags including orphans
EXEC usp_s_tag_tree @workspace_id = 1, @user_id = 1;

-- Expected results should include:
-- - Tag 126 (has children 127, 150)
-- - Tag 137 (orphan, no parent, no children) ✅ NOW INCLUDED
-- - Tag 127 (from item 17, orphan root) ✅ NOW INCLUDED
-- - Tag 127 (from item 1, child of 126)
-- - Tag 150 (child of 126)
-- - Note 1 (child of 127)
```

## Alternative Approach: Filter by workspace_items

If you only want tags **explicitly added** to the workspace:

```sql
-- Add to WHERE clause of root level:
AND EXISTS (
    SELECT 1 FROM workspace_items wsi_check
    WHERE (wsi_check.child_id = wi.item_id AND wsi_check.child_type = 'tag')
    OR wsi_check.parent_tag_id = wi.item_id
    AND wsi_check.workspace_id = @workspace_id
    AND wsi_check.deleted_at IS NULL
)
```

This ensures only tags that appear in `workspace_items` are included.

## Impact Analysis

### Before Fix
- Returns: Tags with children only (hierarchical structure)
- Missing: Orphan/leaf tags without children

### After Fix
- Returns: ALL tags in workspace (hierarchical + orphans)
- Includes: Orphan tags at root level with `depth = 0`, `children_count = 0`

### Frontend Impact
Frontend should handle:
- Tags with `children_count = 0` (leaf nodes)
- Multiple root-level tags (not just one tree)
- Tags appearing multiple times (if added as both root and child)

## Deployment

1. Backup current stored procedure
2. Apply updated `usp_s_tag_tree`
3. Test with workspace_id = 1
4. Verify all 5 items are returned
5. Check frontend renders correctly
