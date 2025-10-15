CREATE OR ALTER PROCEDURE usp_get_item_children
    @workspace_id INT,
    @parent_tag_id INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        wi.id,
        wi.child_type,
        wi.child_id,
        CASE wi.child_type
            WHEN 'tag' THEN t.name
            WHEN 'note' THEN n.name
            ELSE NULL
        END AS child_name,
        wi.depth,
        wi.sort_order,
        wi.relationship_type,
        wi.label,
        wi.created_at
    FROM workspace_items wi
    LEFT JOIN tags t ON wi.child_type = 'tag' AND wi.child_id = t.id
    LEFT JOIN notes n ON wi.child_type = 'note' AND wi.child_id = n.id
    WHERE wi.workspace_id = @workspace_id
    AND wi.parent_tag_id = @parent_tag_id
    AND wi.deleted_at IS NULL
    ORDER BY wi.sort_order, wi.id;
END;
GO
GO

PRINT 'âœ… usp_get_item_children created successfully';
GO
