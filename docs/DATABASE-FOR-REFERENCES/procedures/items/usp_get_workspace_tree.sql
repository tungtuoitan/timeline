CREATE OR ALTER PROCEDURE usp_get_workspace_tree
    @workspace_id INT,
    @parent_tag_id INT = NULL, -- If NULL, get all; if specified, get subtree
    @max_depth INT = NULL, -- Limit depth
    @include_deleted BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    
    -- If no specific parent, get all items
    IF @parent_tag_id IS NULL
    BEGIN
        SELECT 
            wi.id,
            wi.workspace_id,
            wi.parent_tag_id,
            t_parent.name AS parent_tag_name,
            wi.child_type,
            wi.child_id,
            CASE wi.child_type
                WHEN 'tag' THEN t_child.name
                WHEN 'note' THEN n.name
                ELSE NULL
            END AS child_name,
            wi.item_path,
            wi.depth,
            wi.sort_order,
            wi.relationship_type,
            wi.label,
            wi.created_at,
            wi.updated_at,
            wi.deleted_at
        FROM workspace_items wi
        INNER JOIN tags t_parent ON wi.parent_tag_id = t_parent.id
        LEFT JOIN tags t_child ON wi.child_type = 'tag' AND wi.child_id = t_child.id
        LEFT JOIN notes n ON wi.child_type = 'note' AND wi.child_id = n.id
        WHERE wi.workspace_id = @workspace_id
        AND (@include_deleted = 1 OR wi.deleted_at IS NULL)
        AND (@max_depth IS NULL OR wi.depth <= @max_depth)
        ORDER BY wi.depth, wi.sort_order, wi.id;
    END
    ELSE
    BEGIN
        -- Get subtree under specific parent
        DECLARE @parent_path NVARCHAR(4000);
        
        SELECT TOP 1 @parent_path = item_path
        FROM workspace_items
        WHERE workspace_id = @workspace_id
        AND child_type = 'tag'
        AND child_id = @parent_tag_id
        AND deleted_at IS NULL
        ORDER BY depth DESC;
        
        IF @parent_path IS NULL
        BEGIN
            SET @parent_path = CAST(@parent_tag_id AS NVARCHAR);
        END;
        
        SELECT 
            wi.id,
            wi.workspace_id,
            wi.parent_tag_id,
            t_parent.name AS parent_tag_name,
            wi.child_type,
            wi.child_id,
            CASE wi.child_type
                WHEN 'tag' THEN t_child.name
                WHEN 'note' THEN n.name
                ELSE NULL
            END AS child_name,
            wi.item_path,
            wi.depth,
            wi.sort_order,
            wi.relationship_type,
            wi.label,
            wi.created_at,
            wi.updated_at,
            wi.deleted_at
        FROM workspace_items wi
        INNER JOIN tags t_parent ON wi.parent_tag_id = t_parent.id
        LEFT JOIN tags t_child ON wi.child_type = 'tag' AND wi.child_id = t_child.id
        LEFT JOIN notes n ON wi.child_type = 'note' AND wi.child_id = n.id
        WHERE wi.workspace_id = @workspace_id
        AND wi.item_path LIKE @parent_path + '%'
        AND (@include_deleted = 1 OR wi.deleted_at IS NULL)
        AND (@max_depth IS NULL OR wi.depth <= @max_depth)
        ORDER BY wi.depth, wi.sort_order, wi.id;
    END;
END;
GO
GO

PRINT 'âœ… usp_get_workspace_tree created successfully';
GO
