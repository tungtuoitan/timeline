CREATE OR ALTER PROCEDURE usp_get_children_cached
    @workspace_id INT,
    @parent_path NVARCHAR(4000)
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
        descendant_count
    FROM workspace_tree_cache
    WHERE workspace_id = @workspace_id
      AND parent_path = @parent_path
    ORDER BY sort_order, item_name;
END;
GO

PRINT '   ✅ usp_get_children_cached created';

-- Procedure: Get breadcrumb path from cache
GO

PRINT 'âœ… usp_get_children_cached created successfully';
GO

