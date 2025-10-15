-- ============================================
-- FILE: materialized_views/refresh-procedures.sql
-- PURPOSE: Procedures for refreshing workspace tree cache
-- DEPENDENCIES: 02-tables-core.sql, 03-tables-workspace.sql, 07-tables-entities.sql, materialized_views/workspace-tree-cache-table.sql
-- ============================================

USE SuperApp;
GO

SET NOCOUNT ON;

PRINT '🔄 Creating cache refresh procedures...';
GO

-- Procedure: Refresh workspace tree cache for a specific workspace
CREATE OR ALTER PROCEDURE usp_refresh_workspace_tree_cache
    @workspace_id INT,
    @force_refresh BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @start_time DATETIME2 = GETUTCDATE();
    DECLARE @item_count INT;
    DECLARE @last_refresh DATETIME2;
    
    -- Check last refresh time
    SELECT @last_refresh = MAX(cached_at)
    FROM workspace_tree_cache
    WHERE workspace_id = @workspace_id;
    
    -- Skip if refreshed recently (within 5 minutes) and not forced
    IF @force_refresh = 0 AND @last_refresh IS NOT NULL 
       AND DATEDIFF(MINUTE, @last_refresh, GETUTCDATE()) < 5
    BEGIN
        PRINT 'Cache refreshed recently. Skipping refresh.';
        PRINT 'Last refresh: ' + CONVERT(VARCHAR(30), @last_refresh, 120);
        PRINT 'Use @force_refresh = 1 to force refresh.';
        RETURN;
    END;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Delete old cache for this workspace
        DELETE FROM workspace_tree_cache
        WHERE workspace_id = @workspace_id;
        
        -- Build tree cache using recursive CTE
        ;WITH WorkspaceTree AS (
            -- Base case: Root items (depth = 0)
            SELECT 
                wi.id AS item_id,
                wi.workspace_id,
                wi.child_type,
                wi.child_id,
                wi.item_path,
                wi.depth,
                CAST(NULL AS NVARCHAR(4000)) AS parent_path,
                wi.relationship_type,
                wi.label,
                wi.sort_order,
                CASE wi.child_type
                    WHEN 'tag' THEN t.name
                    WHEN 'note' THEN n.name
                    ELSE 'Unknown'
                END AS item_name,
                CASE wi.child_type
                    WHEN 'tag' THEN t.description
                    WHEN 'note' THEN n.description
                    ELSE NULL
                END AS item_description,
                CASE wi.child_type
                    WHEN 'tag' THEN t.color
                    WHEN 'note' THEN n.color
                    ELSE NULL
                END AS item_color,
                CAST(1 AS BIT) AS is_root,
                CAST(0 AS BIT) AS is_leaf_temp
            FROM workspace_items wi
            LEFT JOIN tags t ON wi.child_type = 'tag' AND wi.child_id = t.id AND t.deleted_at IS NULL
            LEFT JOIN notes n ON wi.child_type = 'note' AND wi.child_id = n.id AND n.deleted_at IS NULL
            WHERE wi.workspace_id = @workspace_id
              AND wi.deleted_at IS NULL
              AND wi.depth = 0
            
            UNION ALL
            
            -- Recursive case: Child items
            SELECT 
                wi.id AS item_id,
                wi.workspace_id,
                wi.child_type,
                wi.child_id,
                wi.item_path,
                wi.depth,
                wt.item_path AS parent_path,
                wi.relationship_type,
                wi.label,
                wi.sort_order,
                CASE wi.child_type
                    WHEN 'tag' THEN t.name
                    WHEN 'note' THEN n.name
                    ELSE 'Unknown'
                END AS item_name,
                CASE wi.child_type
                    WHEN 'tag' THEN t.description
                    WHEN 'note' THEN n.description
                    ELSE NULL
                END AS item_description,
                CASE wi.child_type
                    WHEN 'tag' THEN t.color
                    WHEN 'note' THEN n.color
                    ELSE NULL
                END AS item_color,
                CAST(0 AS BIT) AS is_root,
                CAST(0 AS BIT) AS is_leaf_temp
            FROM workspace_items wi
            INNER JOIN WorkspaceTree wt ON wi.item_path LIKE wt.item_path + '.%'
            LEFT JOIN tags t ON wi.child_type = 'tag' AND wi.child_id = t.id AND t.deleted_at IS NULL
            LEFT JOIN notes n ON wi.child_type = 'note' AND wi.child_id = n.id AND n.deleted_at IS NULL
            WHERE wi.workspace_id = @workspace_id
              AND wi.deleted_at IS NULL
              AND wi.depth = wt.depth + 1
        )
        -- Insert into cache with child counts
        INSERT INTO workspace_tree_cache (
            workspace_id,
            item_id,
            child_type,
            child_id,
            item_path,
            depth,
            parent_path,
            item_name,
            item_description,
            item_color,
            relationship_type,
            label,
            sort_order,
            is_root,
            is_leaf,
            child_count,
            descendant_count,
            cached_at
        )
        SELECT 
            wt.workspace_id,
            wt.item_id,
            wt.child_type,
            wt.child_id,
            wt.item_path,
            wt.depth,
            wt.parent_path,
            wt.item_name,
            wt.item_description,
            wt.item_color,
            wt.relationship_type,
            wt.label,
            wt.sort_order,
            wt.is_root,
            -- Is leaf: no children
            CASE 
                WHEN NOT EXISTS (
                    SELECT 1 FROM workspace_items wi2
                    WHERE wi2.workspace_id = wt.workspace_id
                      AND wi2.item_path LIKE wt.item_path + '.%'
                      AND wi2.depth = wt.depth + 1
                      AND wi2.deleted_at IS NULL
                ) THEN 1
                ELSE 0
            END AS is_leaf,
            -- Direct child count
            (
                SELECT COUNT(*)
                FROM workspace_items wi2
                WHERE wi2.workspace_id = wt.workspace_id
                  AND wi2.item_path LIKE wt.item_path + '.%'
                  AND wi2.depth = wt.depth + 1
                  AND wi2.deleted_at IS NULL
            ) AS child_count,
            -- Total descendant count
            (
                SELECT COUNT(*)
                FROM workspace_items wi2
                WHERE wi2.workspace_id = wt.workspace_id
                  AND wi2.item_path LIKE wt.item_path + '.%'
                  AND wi2.deleted_at IS NULL
            ) AS descendant_count,
            GETUTCDATE() AS cached_at
        FROM WorkspaceTree wt
        OPTION (MAXRECURSION 100); -- Limit recursion depth
        
        SET @item_count = @@ROWCOUNT;
        
        COMMIT TRANSACTION;
        
        PRINT '✅ Cache refreshed for workspace: ' + CAST(@workspace_id AS VARCHAR);
        PRINT '   Items cached: ' + CAST(@item_count AS VARCHAR);
        PRINT '   Duration: ' + CAST(DATEDIFF(MILLISECOND, @start_time, GETUTCDATE()) AS VARCHAR) + ' ms';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        
        PRINT '❌ Error refreshing cache:';
        PRINT ERROR_MESSAGE();
        THROW;
    END CATCH;
END;
GO

PRINT '   ✅ usp_refresh_workspace_tree_cache created';

-- Procedure: Refresh caches for all large workspaces (500+ items)
CREATE OR ALTER PROCEDURE usp_refresh_all_workspace_caches
    @min_item_count INT = 500
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @workspace_id INT;
    DECLARE workspace_cursor CURSOR FOR
    SELECT w.id
    FROM workspaces w
    WHERE w.tag_count >= @min_item_count
      AND w.deleted_at IS NULL;
    
    OPEN workspace_cursor;
    FETCH NEXT FROM workspace_cursor INTO @workspace_id;
    
    WHILE @@FETCH_STATUS = 0
    BEGIN
        EXEC usp_refresh_workspace_tree_cache @workspace_id, @force_refresh = 1;
        FETCH NEXT FROM workspace_cursor INTO @workspace_id;
    END;
    
    CLOSE workspace_cursor;
    DEALLOCATE workspace_cursor;
    
    PRINT '✅ All large workspace caches refreshed';
END;
GO

PRINT '   ✅ usp_refresh_all_workspace_caches created';
PRINT '📊 Next step: Run materialized_views/query-procedures.sql';
GO