-- =============================================
-- Diagnose Duplicate Workspace Items
-- =============================================
-- This script helps identify duplicate ChildId values
-- across different ChildType values in workspace_items
-- which was causing the BuildHierarchy exception
-- =============================================

USE SuperAppDB;
GO

-- 1. Check for duplicate ChildId across different ChildType
-- (This is EXPECTED and NORMAL - Tag 127, Note 127, File 127 can all exist)
SELECT 
    wi.child_id,
    wi.child_type,
    COUNT(*) AS occurrence_count,
    STRING_AGG(CAST(wi.workspace_id AS VARCHAR), ', ') AS workspaces
FROM workspace_items wi
GROUP BY wi.child_id, wi.child_type
HAVING COUNT(*) > 1
ORDER BY wi.child_id, wi.child_type;

-- 2. Check for TRUE duplicates (same WorkspaceId + ChildType + ChildId)
-- (This is PROBLEMATIC - indicates duplicate entries)
SELECT 
    wi.workspace_id,
    wi.child_type,
    wi.child_id,
    COUNT(*) AS duplicate_count,
    STRING_AGG(CAST(wi.item_id AS VARCHAR), ', ') AS item_ids
FROM workspace_items wi
GROUP BY wi.workspace_id, wi.child_type, wi.child_id
HAVING COUNT(*) > 1
ORDER BY wi.workspace_id, wi.child_type, wi.child_id;

-- 3. Show items with overlapping IDs across types (for debugging)
-- Example: Tag 127, Note 127, File 127 all in the same workspace
WITH ItemCounts AS (
    SELECT 
        workspace_id,
        child_id,
        COUNT(DISTINCT child_type) AS type_count,
        STRING_AGG(child_type, ', ') AS types
    FROM workspace_items
    GROUP BY workspace_id, child_id
    HAVING COUNT(DISTINCT child_type) > 1
)
SELECT 
    ic.workspace_id,
    w.name AS workspace_name,
    ic.child_id,
    ic.type_count,
    ic.types,
    'This is NORMAL - different types can have same ID' AS note
FROM ItemCounts ic
LEFT JOIN workspaces w ON ic.workspace_id = w.workspace_id
ORDER BY ic.workspace_id, ic.child_id;

-- 4. Get sample data for a workspace with mixed types
SELECT TOP 20
    wi.item_id,
    wi.workspace_id,
    wi.parent_tag_id,
    wi.child_type,
    wi.child_id,
    wi.depth,
    wi.sort_order,
    CASE 
        WHEN wi.child_type = 'tag' THEN t.name
        WHEN wi.child_type = 'note' THEN n.name
        WHEN wi.child_type = 'file' THEN f.file_name
    END AS item_name
FROM workspace_items wi
LEFT JOIN tags t ON wi.child_type = 'tag' AND wi.child_id = t.tag_id
LEFT JOIN notes n ON wi.child_type = 'note' AND wi.child_id = n.note_id
LEFT JOIN files f ON wi.child_type = 'file' AND wi.child_id = f.file_id
WHERE wi.workspace_id = (SELECT TOP 1 workspace_id FROM workspace_items ORDER BY workspace_id)
ORDER BY wi.sort_order, wi.depth;

-- 5. Summary statistics
SELECT 
    'Total workspace items' AS metric,
    COUNT(*) AS count
FROM workspace_items
UNION ALL
SELECT 
    'Unique workspaces with items',
    COUNT(DISTINCT workspace_id)
FROM workspace_items
UNION ALL
SELECT 
    'Tag items',
    COUNT(*)
FROM workspace_items
WHERE child_type = 'tag'
UNION ALL
SELECT 
    'Note items',
    COUNT(*)
FROM workspace_items
WHERE child_type = 'note'
UNION ALL
SELECT 
    'File items',
    COUNT(*)
FROM workspace_items
WHERE child_type = 'file';

-- 6. Find workspaces with the same child_id used by multiple types
SELECT 
    wi.workspace_id,
    w.name AS workspace_name,
    COUNT(DISTINCT CONCAT(wi.child_type, '_', wi.child_id)) AS unique_items,
    COUNT(*) AS total_items,
    CASE 
        WHEN COUNT(*) > COUNT(DISTINCT CONCAT(wi.child_type, '_', wi.child_id))
        THEN 'TRUE DUPLICATES DETECTED!'
        ELSE 'OK'
    END AS status
FROM workspace_items wi
LEFT JOIN workspaces w ON wi.workspace_id = w.workspace_id
GROUP BY wi.workspace_id, w.name
ORDER BY wi.workspace_id;
