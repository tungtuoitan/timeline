CREATE OR ALTER PROCEDURE usp_update_workspace_access
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
        RETURN;
    END;
    
    -- Update workspace last_accessed_at
    UPDATE workspaces
    SET last_accessed_at = GETUTCDATE()
    WHERE id = @workspace_id;
    
    -- Update member last_accessed_at
    UPDATE workspace_members
    SET last_accessed_at = GETUTCDATE()
    WHERE workspace_id = @workspace_id
    AND user_id = @user_id;
END;
GO
GO

PRINT 'âœ… usp_update_workspace_access created successfully';
GO
