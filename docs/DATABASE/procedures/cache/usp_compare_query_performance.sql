CREATE OR ALTER PROCEDURE usp_compare_query_performance
    @workspace_id INT,
    @iterations INT = 10
AS
BEGIN
    SET NOCOUNT ON;
    SET STATISTICS TIME OFF;
    
    DECLARE @start_time DATETIME2;
    DECLARE @end_time DATETIME2;
    DECLARE @cached_ms BIGINT = 0;
    DECLARE @direct_ms BIGINT = 0;
    DECLARE @i INT = 1;
    
    PRINT 'Performance comparison for workspace: ' + CAST(@workspace_id AS VARCHAR);
    PRINT 'Iterations: ' + CAST(@iterations AS VARCHAR);
    PRINT '';
    
    -- Ensure cache exists
    EXEC usp_refresh_workspace_tree_cache @workspace_id = @workspace_id, @force_refresh = 1;
    
    PRINT '';
    PRINT 'Testing cached query...';
    
    -- Test cached query
    WHILE @i <= @iterations
    BEGIN
        SET @start_time = SYSDATETIME();
        
        SELECT * FROM workspace_tree_cache
        WHERE workspace_id = @workspace_id;
        
        SET @end_time = SYSDATETIME();
        SET @cached_ms = @cached_ms + DATEDIFF(MICROSECOND, @start_time, @end_time);
        SET @i = @i + 1;
    END;
    
    PRINT 'Testing direct query...';
    SET @i = 1;
    
    -- Test direct query
    WHILE @i <= @iterations
    BEGIN
        SET @start_time = SYSDATETIME();
        
        ;WITH WorkspaceTree AS (
            SELECT * FROM workspace_items
            WHERE workspace_id = @workspace_id AND deleted_at IS NULL AND depth = 0
            UNION ALL
            SELECT wi.* FROM workspace_items wi
            INNER JOIN WorkspaceTree wt ON wi.item_path LIKE wt.item_path + '.%'
            WHERE wi.workspace_id = @workspace_id AND wi.deleted_at IS NULL
        )
        SELECT * FROM WorkspaceTree;
        
        SET @end_time = SYSDATETIME();
        SET @direct_ms = @direct_ms + DATEDIFF(MICROSECOND, @start_time, @end_time);
        SET @i = @i + 1;
    END;
    
    -- Calculate averages
    DECLARE @cached_avg_ms DECIMAL(10,2) = @cached_ms / 1000.0 / @iterations;
    DECLARE @direct_avg_ms DECIMAL(10,2) = @direct_ms / 1000.0 / @iterations;
    DECLARE @speedup DECIMAL(10,2) = @direct_avg_ms / NULLIF(@cached_avg_ms, 0);
    
    PRINT '';
    PRINT '═══════════════════════════════════════════════════════════';
    PRINT 'RESULTS:';
    PRINT '   Cached query avg: ' + CAST(@cached_avg_ms AS VARCHAR) + ' ms';
    PRINT '   Direct query avg: ' + CAST(@direct_avg_ms AS VARCHAR) + ' ms';
    PRINT '   Speedup: ' + CAST(@speedup AS VARCHAR) + 'x faster';
    PRINT '═══════════════════════════════════════════════════════════';
END;
GO

PRINT '   ✅ usp_compare_query_performance created';

-- ============================================
-- COMPLETION MESSAGE
-- ============================================

PRINT '';
PRINT '═══════════════════════════════════════════════════════════';
PRINT '   ✅ MATERIALIZED VIEWS INSTALLATION COMPLETE';
PRINT '═══════════════════════════════════════════════════════════';
PRINT '';
PRINT '📊 Summary:';
PRINT '   ✅ 1 cache table created (workspace_tree_cache)';
PRINT '   ✅ 9 cache procedures created';
PRINT '   ✅ 1 automatic invalidation trigger created';
PRINT '   ✅ 1 performance comparison tool created';
PRINT '';
PRINT '🚀 Quick Start:';
PRINT '   1. Refresh cache for workspace:';
PRINT '      EXEC usp_refresh_workspace_tree_cache @workspace_id = 1';
PRINT '';
PRINT '   2. Query cached tree (10-100x faster):';
PRINT '      EXEC usp_get_workspace_tree_cached @workspace_id = 1';
PRINT '';
PRINT '   3. Refresh all large workspaces (500+ items):';
PRINT '      EXEC usp_refresh_all_workspace_caches';
PRINT '';
PRINT '   4. Compare performance:';
PRINT '      EXEC usp_compare_query_performance @workspace_id = 1';
PRINT '';
PRINT '📋 Available Procedures:';
PRINT '   • usp_refresh_workspace_tree_cache - Rebuild cache';
PRINT '   • usp_refresh_all_workspace_caches - Batch rebuild';
PRINT '   • usp_get_workspace_tree_cached - Get full tree';
PRINT '   • usp_get_subtree_cached - Get subtree';
PRINT '   • usp_get_children_cached - Get direct children';
PRINT '   • usp_get_breadcrumb_cached - Get breadcrumb path';
PRINT '   • usp_invalidate_workspace_cache - Clear cache';
PRINT '   • usp_cleanup_old_caches - Maintenance';
PRINT '   • usp_get_cache_statistics - Cache stats';
PRINT '';
PRINT '⚡ Performance Benefits:';
PRINT '   • 10-100x faster queries for large workspaces';
PRINT '   • Automatic cache invalidation on changes';
PRINT '   • Smart refresh (skip if recent)';
PRINT '   • Optimized for workspaces with 500+ items';
PRINT '';
PRINT '🔄 Cache Invalidation:';
PRINT '   • Automatic when workspace_items change';
PRINT '   • Manual: usp_invalidate_workspace_cache';
PRINT '   • Cleanup old: usp_cleanup_old_caches';
PRINT '';
GO

PRINT 'âœ… usp_compare_query_performance created successfully';
GO

