CREATE OR ALTER PROCEDURE usp_get_subtree_cached
    @workspace_id INT,
    @root_path NVARCHAR(4000),
    @max_depth INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
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
        descendant_count,
        -- Calculate relative depth from root
        depth - (
            SELECT depth FROM workspace_tree_cache 
            WHERE workspace_id = @workspace_id 
              AND item_path = @root_path
        ) AS relative_depth
    FROM workspace_tree_cache
    WHERE workspace_id = @workspace_id
      AND (item_path = @root_path OR item_path LIKE @root_path + '.%')
      AND (@max_depth IS NULL OR 
           depth <= (
               SELECT depth + @max_depth 
               FROM workspace_tree_cache 
               WHERE workspace_id = @workspace_id 
                 AND item_path = @root_path
           ))
    ORDER BY depth, sort_order, item_name;
END;
GO

PRINT '   ✅ usp_get_subtree_cached created';

-- Procedure: Get direct children from cache
GO

PRINT 'âœ… usp_get_subtree_cached created successfully';
GO

