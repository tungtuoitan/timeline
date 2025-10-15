-- ============================================
-- FILE: materialized_views/invalidation-trigger.sql
-- PURPOSE: Automatic cache invalidation trigger
-- DEPENDENCIES: 02-tables-core.sql, 03-tables-workspace.sql, 07-tables-entities.sql, materialized_views/workspace-tree-cache-table.sql
-- ============================================

USE SuperApp;
GO

SET NOCOUNT ON;

PRINT '🔄 Creating cache invalidation trigger...';
GO

-- Trigger: Invalidate cache when items change
CREATE OR ALTER TRIGGER tr_invalidate_cache_on_item_change
ON workspace_items
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Get affected workspaces
    DECLARE @affected_workspaces TABLE (workspace_id INT);
    
    INSERT INTO @affected_workspaces (workspace_id)
    SELECT DISTINCT workspace_id FROM inserted
    UNION
    SELECT DISTINCT workspace_id FROM deleted;
    
    -- Invalidate caches
    DELETE wtc
    FROM workspace_tree_cache wtc
    INNER JOIN @affected_workspaces aw ON wtc.workspace_id = aw.workspace_id;
END;
GO

PRINT '   ✅ tr_invalidate_cache_on_item_change created';
PRINT '📊 Next step: Run materialized_views/performance-comparison.sql';
GO