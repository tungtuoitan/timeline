CREATE OR ALTER PROCEDURE usp_get_cache_statistics
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        w.id AS workspace_id,
        w.name AS workspace_name,
        COUNT(*) AS cached_items,
        MIN(wtc.cached_at) AS oldest_cache,
        MAX(wtc.cached_at) AS newest_cache,
        DATEDIFF(MINUTE, MAX(wtc.cached_at), GETUTCDATE()) AS minutes_since_refresh,
        MAX(wtc.depth) AS max_depth,
        SUM(wtc.child_count) AS total_relationships
    FROM workspace_tree_cache wtc
    INNER JOIN workspaces w ON wtc.workspace_id = w.id
    GROUP BY w.id, w.name
    ORDER BY cached_items DESC;
END;
GO

PRINT '   ✅ usp_get_cache_statistics created';

-- ============================================
-- SECTION 5: AUTOMATIC CACHE INVALIDATION
-- ============================================

PRINT '';
PRINT '📊 Section 5: Automatic cache invalidation triggers';

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

-- ============================================
-- SECTION 6: PERFORMANCE COMPARISON
-- ============================================

PRINT '';
PRINT '📊 Section 6: Performance comparison procedure';

-- Procedure: Compare cached vs non-cached query performance
GO

PRINT 'âœ… usp_get_cache_statistics created successfully';
GO

