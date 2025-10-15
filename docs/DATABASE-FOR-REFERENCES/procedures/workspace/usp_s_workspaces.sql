CREATE OR ALTER PROCEDURE usp_s_workspaces
    @user_id INT,
    @include_archived BIT = 0,
    @type NVARCHAR(20) = NULL,
    @sort_by NVARCHAR(20) = 'recent' -- 'recent', 'name', 'created'
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
        w.relationship_count,
        w.member_count,
        w.is_default,
        w.is_archived,
        w.is_public,
        w.created_at,
        w.updated_at,
        w.last_accessed_at,
        wm.role AS user_role,
        CASE WHEN w.user_id = @user_id THEN 1 ELSE 0 END AS is_owner
    FROM workspaces w
    INNER JOIN workspace_members wm 
        ON w.id = wm.workspace_id
        AND wm.user_id = @user_id
        AND wm.deleted_at IS NULL
        AND wm.invitation_status = 'active'
    WHERE w.deleted_at IS NULL
    AND (@include_archived = 1 OR w.is_archived = 0)
    AND (@type IS NULL OR w.type = @type)
    ORDER BY
        CASE 
            WHEN @sort_by = 'recent' THEN w.last_accessed_at
            WHEN @sort_by = 'created' THEN w.created_at
        END DESC,
        CASE WHEN @sort_by = 'name' THEN w.name END ASC;
END;
GO
GO

PRINT 'âœ… usp_s_workspaces created successfully';
GO
