CREATE OR ALTER PROCEDURE usp_s_workspace_stats
    @workspace_id INT,
    @user_id INT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Check access
    IF NOT EXISTS (
        SELECT 1 
        FROM workspace_members
        WHERE workspace_id = @workspace_id
        AND user_id = @user_id
        AND deleted_at IS NULL
    )
    BEGIN
        RAISERROR('Access denied.', 16, 1);
        RETURN;
    END;
    
    -- Basic stats
    SELECT 
        w.id,
        w.name,
        w.type,
        w.tag_count,
        w.relationship_count,
        w.member_count,
        w.created_at,
        w.last_accessed_at,
        (
            SELECT COUNT(DISTINCT child_id)
            FROM workspace_items
            WHERE workspace_id = w.id
            AND child_type = 'note'
            AND deleted_at IS NULL
        ) AS note_count,
        (
            SELECT COUNT(*)
            FROM workspace_items
            WHERE workspace_id = w.id
            AND depth = 0
            AND deleted_at IS NULL
        ) AS root_count,
        (
            SELECT MAX(depth)
            FROM workspace_items
            WHERE workspace_id = w.id
            AND deleted_at IS NULL
        ) AS max_depth
    FROM workspaces w
    WHERE w.id = @workspace_id;
    
    -- Item breakdown by type
    SELECT 
        child_type,
        COUNT(*) AS count
    FROM workspace_items
    WHERE workspace_id = @workspace_id
    AND deleted_at IS NULL
    GROUP BY child_type
    ORDER BY count DESC;
    
    -- Recent activity (last 10 items added)
    SELECT TOP 10
        wi.id,
        wi.child_type,
        wi.child_id,
        CASE wi.child_type
            WHEN 'tag' THEN t.name
            WHEN 'note' THEN n.name
            ELSE NULL
        END AS item_name,
        wi.created_at,
        u.username AS created_by_username
    FROM workspace_items wi
    LEFT JOIN tags t ON wi.child_type = 'tag' AND wi.child_id = t.id
    LEFT JOIN notes n ON wi.child_type = 'note' AND wi.child_id = n.id
    INNER JOIN users u ON wi.created_by = u.id
    WHERE wi.workspace_id = @workspace_id
    AND wi.deleted_at IS NULL
    ORDER BY wi.created_at DESC;
END;
GO
GO

PRINT 'âœ… usp_s_workspace_stats created successfully';
GO
