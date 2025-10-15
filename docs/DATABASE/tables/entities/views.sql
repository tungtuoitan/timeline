-- ============================================
-- FILE: tables/entities/views.sql
-- PURPOSE: Workspace-related views for tree navigation
-- DEPENDENCIES: workspace_items.sql, workspaces.sql, tags.sql, notes.sql
-- ============================================

PRINT '';
PRINT '📦 Creating view: vw_workspace_tree';
PRINT '   Purpose: Workspace tree with entity details';
PRINT '   Features: Full tree navigation with joins';
PRINT '';

-- ============================================
-- VIEW: vw_workspace_tree
-- PURPOSE: Simplified workspace tree queries with entity details
-- ============================================

CREATE OR ALTER VIEW vw_workspace_tree AS
SELECT 
    wi.id AS item_id,
    wi.workspace_id,
    w.name AS workspace_name,
    wi.parent_tag_id,
    t_parent.name AS parent_tag_name,
    wi.child_type,
    wi.child_id,
    CASE wi.child_type
        WHEN 'tag' THEN t_child.name
        WHEN 'note' THEN n.name
        -- Add more cases when projects/documents/tasks are implemented
        ELSE NULL
    END AS child_name,
    wi.item_path,
    wi.depth,
    wi.sort_order,
    wi.relationship_type,
    wi.label,
    wi.created_at,
    wi.updated_at
FROM workspace_items wi
INNER JOIN workspaces w ON wi.workspace_id = w.id
INNER JOIN tags t_parent ON wi.parent_tag_id = t_parent.id
LEFT JOIN tags t_child ON wi.child_type = 'tag' AND wi.child_id = t_child.id
LEFT JOIN notes n ON wi.child_type = 'note' AND wi.child_id = n.id
WHERE wi.deleted_at IS NULL
AND w.deleted_at IS NULL
AND t_parent.deleted_at IS NULL
AND (t_child.deleted_at IS NULL OR t_child.id IS NULL)
AND (n.deleted_at IS NULL OR n.id IS NULL);
GO

PRINT '   ✅ vw_workspace_tree created';

-- ============================================
-- VALIDATION
-- ============================================

PRINT '';
PRINT '📊 Verifying view...';

SELECT 
    name AS view_name,
    type_desc,
    create_date
FROM sys.objects
WHERE name = 'vw_workspace_tree'
AND type = 'V';

GO

-- ============================================
-- SUCCESS MESSAGE
-- ============================================

PRINT '';
PRINT '✅ vw_workspace_tree view created successfully!';
PRINT '';
PRINT '🌳 FEATURES:';
PRINT '   - Complete workspace tree navigation';
PRINT '   - Joins with workspaces, tags, and notes';
PRINT '   - Automatic child name resolution';
PRINT '   - Filters out soft-deleted items';
PRINT '';
PRINT '💡 USAGE EXAMPLES:';
PRINT '   -- Get all items in a workspace:';
PRINT '   SELECT * FROM vw_workspace_tree WHERE workspace_id = ?';
PRINT '';
PRINT '   -- Get children of a specific tag:';
PRINT '   SELECT * FROM vw_workspace_tree WHERE parent_tag_id = ?';
PRINT '';
PRINT '   -- Get tree roots (depth 0):';
PRINT '   SELECT * FROM vw_workspace_tree WHERE depth = 0';
PRINT '';
PRINT '   -- Filter by entity type:';
PRINT '   SELECT * FROM vw_workspace_tree WHERE child_type = ''note''';
PRINT '';
GO
