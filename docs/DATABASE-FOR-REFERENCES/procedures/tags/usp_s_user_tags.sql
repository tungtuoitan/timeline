-- ============================================
-- FILE: procedures/tags/usp_s_user_tags.sql
-- PURPOSE: List all tags for a user
-- DEPENDENCIES: tags table
-- ============================================

PRINT '';
PRINT '📝 Creating procedure: usp_s_user_tags';
PRINT '   Purpose: List all tags for user with hierarchy';
PRINT '';

GO
CREATE OR ALTER PROCEDURE usp_s_user_tags
    @user_id INT,
    @include_deleted BIT = 0
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
        
        -- Return tags (flat list - hierarchy managed in workspace_items)
        SELECT 
            t.id,
            t.user_id,
            t.name,
            t.slug,
            t.color,
            t.icon,
            t.description,
            t.metadata,
            t.usage_count,
            t.created_at,
            t.updated_at,
            t.deleted_at,
            
            -- Usage count across all workspaces
            (SELECT COUNT(DISTINCT workspace_id) 
             FROM workspace_items wi
             WHERE wi.child_type = 'tag'
             AND wi.child_id = t.id
             AND wi.deleted_at IS NULL
            ) as workspace_count
            
        FROM tags t
        WHERE t.user_id = @user_id
        AND (@include_deleted = 1 OR t.deleted_at IS NULL)
        ORDER BY t.name;
        
    END TRY
    BEGIN CATCH
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END;
GO

PRINT '   ✅ usp_s_user_tags created';
GO
