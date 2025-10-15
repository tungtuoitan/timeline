CREATE OR ALTER PROCEDURE usp_remove_workspace_member
    @workspace_id INT,
    @user_id INT, -- User to remove
    @removed_by INT -- Current user (must be owner)
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Check if removing user is owner
        DECLARE @remover_role NVARCHAR(20);
        
        SELECT @remover_role = role
        FROM workspace_members
        WHERE workspace_id = @workspace_id
        AND user_id = @removed_by
        AND deleted_at IS NULL;
        
        IF @remover_role != 'owner'
        BEGIN
            RAISERROR('Only workspace owner can remove members.', 16, 1);
            RETURN;
        END;
        
        -- Check if trying to remove an owner
        DECLARE @target_role NVARCHAR(20);
        
        SELECT @target_role = role
        FROM workspace_members
        WHERE workspace_id = @workspace_id
        AND user_id = @user_id
        AND deleted_at IS NULL;
        
        IF @target_role = 'owner'
        BEGIN
            -- Check if last owner
            DECLARE @owner_count INT;
            SELECT @owner_count = COUNT(*)
            FROM workspace_members
            WHERE workspace_id = @workspace_id
            AND role = 'owner'
            AND deleted_at IS NULL;
            
            IF @owner_count <= 1
            BEGIN
                RAISERROR('Cannot remove last owner. Transfer ownership first.', 16, 1);
                RETURN;
            END;
        END;
        
        -- Soft delete member
        UPDATE workspace_members
        SET deleted_at = GETUTCDATE()
        WHERE workspace_id = @workspace_id
        AND user_id = @user_id;
        
        COMMIT TRANSACTION;
        
        PRINT 'Member removed successfully';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
GO

PRINT 'âœ… usp_remove_workspace_member created successfully';
GO
