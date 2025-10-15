CREATE OR ALTER PROCEDURE usp_refresh_all_workspace_caches
    @min_items INT = 500,
    @force_refresh BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @workspace_id INT;
    DECLARE @workspace_name NVARCHAR(200);
    DECLARE @item_count INT;
    DECLARE @total_refreshed INT = 0;
    DECLARE @total_skipped INT = 0;
    
    PRINT 'Refreshing workspace tree caches...';
    PRINT 'Minimum items threshold: ' + CAST(@min_items AS VARCHAR);
    PRINT '';
    
    -- Cursor for large workspaces
    DECLARE workspace_cursor CURSOR FOR
    SELECT 
        w.id,
        w.name,
        COUNT(*) AS item_count
    FROM workspaces w
    INNER JOIN workspace_items wi ON w.id = wi.workspace_id
    WHERE w.deleted_at IS NULL
      AND wi.deleted_at IS NULL
    GROUP BY w.id, w.name
    HAVING COUNT(*) >= @min_items
    ORDER BY COUNT(*) DESC;
    
    OPEN workspace_cursor;
    
    FETCH NEXT FROM workspace_cursor INTO @workspace_id, @workspace_name, @item_count;
    
    WHILE @@FETCH_STATUS = 0
    BEGIN
        PRINT 'Processing: ' + @workspace_name + ' (' + CAST(@item_count AS VARCHAR) + ' items)';
        
        BEGIN TRY
            EXEC usp_refresh_workspace_tree_cache 
                @workspace_id = @workspace_id,
                @force_refresh = @force_refresh;
                
            SET @total_refreshed = @total_refreshed + 1;
        END TRY
        BEGIN CATCH
            PRINT '   ⚠️  Failed to refresh workspace: ' + ERROR_MESSAGE();
            SET @total_skipped = @total_skipped + 1;
        END CATCH;
        
        PRINT '';
        
        FETCH NEXT FROM workspace_cursor INTO @workspace_id, @workspace_name, @item_count;
    END;
    
    CLOSE workspace_cursor;
    DEALLOCATE workspace_cursor;
    
    PRINT '═══════════════════════════════════════════════════════════';
    PRINT 'Cache refresh complete';
    PRINT '   Refreshed: ' + CAST(@total_refreshed AS VARCHAR);
    PRINT '   Skipped: ' + CAST(@total_skipped AS VARCHAR);
    PRINT '═══════════════════════════════════════════════════════════';
END;
GO

PRINT '   ✅ usp_refresh_all_workspace_caches created';

-- ============================================
-- SECTION 3: QUERY PROCEDURES USING CACHE
-- ============================================

PRINT '';
PRINT '📊 Section 3: Query procedures using materialized cache';

-- Procedure: Get workspace tree from cache (fast!)
GO

PRINT 'âœ… usp_refresh_all_workspace_caches created successfully';
GO

