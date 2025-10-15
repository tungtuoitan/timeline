-- ============================================
-- FILE: materialized_views/maintenance-procedures.sql
-- PURPOSE: Cache maintenance procedures
-- DEPENDENCIES: 02-tables-core.sql, 03-tables-workspace.sql, 07-tables-entities.sql, materialized_views/workspace-tree-cache-table.sql
-- ============================================

USE SuperApp;
GO

SET NOCOUNT ON;

PRINT '🔄 Creating cache maintenance procedures...';
GO

-- Procedure: Invalidate cache for a workspace
CREATE OR ALTER PROCEDURE usp_invalidate_workspace_cache
    @workspace_id INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DELETE FROM workspace_tree_cache
    WHERE workspace_id = @workspace_id;
    
    PRINT '✅ Cache invalidated for workspace: ' + CAST(@workspace_id AS VARCHAR);
END;
GO

PRINT '   ✅ usp_invalidate_workspace_cache created';

-- Procedure: Cleanup old caches
CREATE OR ALTER PROCEDURE usp_cleanup_old_caches
    @hours_old INT = 24
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @cutoff DATETIME2 = DATEADD(HOUR, -@hours_old, GETUTCDATE());
    DECLARE @deleted_count INT;
    
    DELETE FROM workspace_tree_cache
    WHERE cached_at < @cutoff;
    
    SET @deleted_count = @@ROWCOUNT;
    
    PRINT '✅ Cleaned up old caches';
    PRINT '   Deleted entries: ' + CAST(@deleted_count AS VARCHAR);
    PRINT '   Older than: ' + CAST(@hours_old AS VARCHAR) + ' hours';
END;
GO

PRINT '   ✅ usp_cleanup_old_caches created';

-- Procedure: Get cache statistics
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
PRINT '📊 Next step: Run materialized_views/invalidation-trigger.sql';
GO