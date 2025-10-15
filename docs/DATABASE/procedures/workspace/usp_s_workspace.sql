CREATE OR ALTER PROCEDURE usp_s_workspace
    @workspace_id INT,
    @user_id INT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Check if user has access
    IF NOT EXISTS (
        SELECT 1 
        FROM workspace_members
        WHERE workspace_id = @workspace_id
        AND user_id = @user_id
        AND deleted_at IS NULL
        AND invitation_status = 'active'
    )
    BEGIN
        RAISERROR('Access denied. User is not a member of this workspace.', 16, 1);
        RETURN;
    END;
    
    -- Return workspace details
    SELECT 
        w.*,
        u.username AS owner_username,
        u.display_name AS owner_display_name
    FROM workspaces w
    INNER JOIN users u ON w.user_id = u.id
    WHERE w.id = @workspace_id
    AND w.deleted_at IS NULL;
    
    -- Return members
    SELECT 
        wm.id AS member_id,
        wm.user_id,
        u.username,
        u.display_name,
        u.avatar_url,
        wm.role,
        wm.invitation_status,
        wm.invited_at,
        wm.joined_at
    FROM workspace_members wm
    INNER JOIN users u ON wm.user_id = u.id
    WHERE wm.workspace_id = @workspace_id
    AND wm.deleted_at IS NULL
    ORDER BY 
        CASE wm.role 
            WHEN 'owner' THEN 1 
            WHEN 'editor' THEN 2 
            ELSE 3 
        END,
        wm.joined_at;
    
    -- Return relationship types
    SELECT *
    FROM workspace_relationship_types
    WHERE workspace_id = @workspace_id
    AND deleted_at IS NULL
    ORDER BY sort_order;
END;
GO
GO

PRINT 'âœ… usp_s_workspace created successfully';
GO
