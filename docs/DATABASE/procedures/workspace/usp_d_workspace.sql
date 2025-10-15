CREATE OR ALTER PROCEDURE usp_d_workspace
    @workspace_id INT,
    @user_id INT
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Check if user is owner
        DECLARE @user_role NVARCHAR(20);
        
        SELECT @user_role = role
        FROM workspace_members
        WHERE workspace_id = @workspace_id
        AND user_id = @user_id
        AND deleted_at IS NULL;
        
        IF @user_role != 'owner'
        BEGIN
            RAISERROR('Only workspace owner can delete workspace.', 16, 1);
            RETURN;
        END;
        
        -- Soft delete workspace
        UPDATE workspaces
        SET deleted_at = GETUTCDATE()
        WHERE id = @workspace_id;
        
        -- Soft delete all items in workspace
        UPDATE workspace_items
        SET deleted_at = GETUTCDATE()
        WHERE workspace_id = @workspace_id;
        
        -- Soft delete all members
        UPDATE workspace_members
        SET deleted_at = GETUTCDATE()
        WHERE workspace_id = @workspace_id;
        
        COMMIT TRANSACTION;
        
        PRINT 'Workspace deleted successfully';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
GO

PRINT 'âœ… usp_d_workspace created successfully';
GO
