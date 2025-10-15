-- ============================================
-- FILE: materialized_views/performance-comparison.sql
-- PURPOSE: Procedure for comparing cached vs non-cached query performance
-- DEPENDENCIES: 02-tables-core.sql, 03-tables-workspace.sql, 07-tables-entities.sql, materialized_views/workspace-tree-cache-table.sql, materialized_views/refresh-procedures.sql
-- ============================================

USE SuperApp;
GO

SET NOCOUNT ON;

PRINT '🔄 Creating performance comparison procedure...';
GO

-- Procedure: Compare cached vs non-cached query performance
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
PRINT '📊 Next step: Run materialized_views/summary.sql';
GO