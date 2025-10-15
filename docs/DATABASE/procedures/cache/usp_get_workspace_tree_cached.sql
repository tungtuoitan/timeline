CREATE OR ALTER PROCEDURE usp_get_workspace_tree_cached
    @workspace_id INT,
    @max_depth INT = NULL,
    @auto_refresh BIT = 1
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Check if cache exists
    IF NOT EXISTS (
        SELECT 1 FROM workspace_tree_cache 
        WHERE workspace_id = @workspace_id
    )
    BEGIN
        IF @auto_refresh = 1
        BEGIN
            PRINT 'Cache not found. Building cache...';
            EXEC usp_refresh_workspace_tree_cache @workspace_id = @workspace_id;
        END
        ELSE
        BEGIN
            PRINT 'Cache not found. Run usp_refresh_workspace_tree_cache first.';
            RETURN;
        END;
    END;
    
    -- Return cached tree
    SELECT 
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
        descendant_count
    FROM workspace_tree_cache
    WHERE workspace_id = @workspace_id
      AND (@max_depth IS NULL OR depth <= @max_depth)
    ORDER BY depth, sort_order, item_name;
END;
GO

PRINT '   ✅ usp_get_workspace_tree_cached created';

-- Procedure: Get subtree from cache
GO

PRINT 'âœ… usp_get_workspace_tree_cached created successfully';
GO

