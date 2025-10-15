-- ============================================
-- FILE: procedures/workspace/usp_s_user_workspaces.sql
-- PURPOSE: List workspaces user owns or has access to
-- DEPENDENCIES: workspaces, workspace_members
-- ============================================

PRINT '';
PRINT '📝 Creating procedure: usp_s_user_workspaces';
PRINT '   Purpose: List all workspaces user can access';
PRINT '';

GO
CREATE OR ALTER PROCEDURE usp_s_user_workspaces
    @user_id INT
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        -- Validate user exists
        IF NOT EXISTS (SELECT 1 FROM users WHERE id = @user_id AND deleted_at IS NULL)
        BEGIN
            RAISERROR('User not found', 16, 1);
            RETURN;
        END
        
        -- Return workspaces (owned or shared)
        SELECT 
            w.id,
            w.user_id,
            w.name,
            w.type,
            w.description,
            w.icon,
            w.color,
            w.settings,
            w.is_default,
            w.created_at,
            w.updated_at,
            
            -- Owner info
            u.email as owner_email,
            u.username as owner_username,
            
            -- User's role in workspace
            CASE 
                WHEN w.user_id = @user_id THEN 'owner'
                ELSE ISNULL(wm.role, 'none')
            END as user_role,
            
            -- Stats
            w.total_items,
            w.total_tags,
            w.total_members,
            
            -- Member info (if shared)
            wm.joined_at,
            wm.last_accessed_at
            
        FROM workspaces w
        INNER JOIN users u ON w.user_id = u.id
        LEFT JOIN workspace_members wm 
            ON w.id = wm.workspace_id 
            AND wm.user_id = @user_id
            AND wm.deleted_at IS NULL
        
        WHERE w.deleted_at IS NULL
        AND (
            w.user_id = @user_id  -- User owns workspace
            OR wm.user_id = @user_id  -- User is member
        )
        
        ORDER BY 
            CASE WHEN w.user_id = @user_id THEN 0 ELSE 1 END,  -- Own workspaces first
            w.is_default DESC,  -- Default workspace first
            w.created_at DESC;
        
    END TRY
    BEGIN CATCH
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END;
GO

PRINT '   ✅ usp_s_user_workspaces created';
GO
