CREATE OR ALTER PROCEDURE usp_search_workspaces
    @user_id INT,
    @search_term NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        w.id,
        w.name,
        w.description,
        w.type,
        w.color,
        w.icon,
        w.tag_count,
        w.member_count,
        w.is_default,
        w.is_archived,
        wm.role AS user_role,
        CASE WHEN w.user_id = @user_id THEN 1 ELSE 0 END AS is_owner
    FROM workspaces w
    INNER JOIN workspace_members wm 
        ON w.id = wm.workspace_id
        AND wm.user_id = @user_id
        AND wm.deleted_at IS NULL
        AND wm.invitation_status = 'active'
    WHERE w.deleted_at IS NULL
    AND (
        w.name LIKE '%' + @search_term + '%'
        OR w.description LIKE '%' + @search_term + '%'
    )
    ORDER BY 
        CASE WHEN w.name LIKE @search_term + '%' THEN 1 ELSE 2 END,
        w.name;
END;
GO
GO

PRINT 'âœ… usp_search_workspaces created successfully';
GO
