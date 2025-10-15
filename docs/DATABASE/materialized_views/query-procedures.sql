-- ============================================
-- FILE: materialized_views/query-procedures.sql
-- PURPOSE: Procedures for querying cached workspace tree
-- DEPENDENCIES: 02-tables-core.sql, 03-tables-workspace.sql, 07-tables-entities.sql, materialized_views/workspace-tree-cache-table.sql
-- ============================================

USE SuperApp;
GO

SET NOCOUNT ON;

PRINT '🔄 Creating cached tree query procedures...';
GO

-- Procedure: Get full cached workspace tree
CREATE OR ALTER PROCEDURE usp_get_workspace_tree_cached
    @workspace_id INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        id,
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
    FROM workspace_tree_cache
    WHERE workspace_id = @workspace_id
    ORDER BY item_path, sort_order;
END;
GO

PRINT '   ✅ usp_get_workspace_tree_cached created';

-- Procedure: Get subtree from cached data
CREATE OR ALTER PROCEDURE usp_get_subtree_cached
    @workspace_id INT,
    @root_path NVARCHAR(4000)
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        id,
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
    FROM workspace_tree_cache
    WHERE workspace_id = @workspace_id
      AND item_path LIKE @root_path + '.%'
    ORDER BY item_path, sort_order;
END;
GO

PRINT '   ✅ usp_get_subtree_cached created';

-- Procedure: Get direct children from cached data
CREATE OR ALTER PROCEDURE usp_get_children_cached
    @workspace_id INT,
    @parent_path NVARCHAR(4000)
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        id,
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
    FROM workspace_tree_cache
    WHERE workspace_id = @workspace_id
      AND parent_path = @parent_path
    ORDER BY sort_order;
END;
GO

PRINT '   ✅ usp_get_children_cached created';

-- Procedure: Get breadcrumb path from cached data
CREATE OR ALTER PROCEDURE usp_get_breadcrumb_cached
    @workspace_id INT,
    @item_path NVARCHAR(4000)
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @segments TABLE (
        segment_path NVARCHAR(4000),
        level INT
    );
    
    DECLARE @current_path NVARCHAR(4000) = '';
    DECLARE @pos INT = 1;
    DECLARE @next_pos INT;
    DECLARE @segment NVARCHAR(100);
    DECLARE @level INT = 0;
    
    -- Split path by dots
    WHILE @pos <= LEN(@item_path)
    BEGIN
        SET @next_pos = CHARINDEX('.', @item_path, @pos);
        
        IF @next_pos = 0
            SET @next_pos = LEN(@item_path) + 1;
        
        SET @segment = SUBSTRING(@item_path, @pos, @next_pos - @pos);
        
        IF LEN(@current_path) = 0
            SET @current_path = @segment;
        ELSE
            SET @current_path = @current_path + '.' + @segment;
        
        INSERT INTO @segments (segment_path, level)
        VALUES (@current_path, @level);
        
        SET @level = @level + 1;
        SET @pos = @next_pos + 1;
    END;
    
    -- Return breadcrumb items
    SELECT 
        wtc.item_id,
        wtc.child_type,
        wtc.child_id,
        wtc.item_path,
        wtc.depth,
        wtc.item_name,
        wtc.item_color,
        s.level AS breadcrumb_level
    FROM @segments s
    INNER JOIN workspace_tree_cache wtc ON wtc.item_path = s.segment_path
    WHERE wtc.workspace_id = @workspace_id
    ORDER BY s.level;
END;
GO

PRINT '   ✅ usp_get_breadcrumb_cached created';
PRINT '📊 Next step: Run materialized_views/maintenance-procedures.sql';
GO