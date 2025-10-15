CREATE OR ALTER PROCEDURE usp_change_workspace_member_role
    @workspace_id INT,
    @user_id INT, -- User whose role to change
    @new_role NVARCHAR(20),
    @changed_by INT -- Current user (must be owner)
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Check if changing user is owner
        DECLARE @changer_role NVARCHAR(20);
        
        SELECT @changer_role = role
        FROM workspace_members
        WHERE workspace_id = @workspace_id
        AND user_id = @changed_by
        AND deleted_at IS NULL;
        
        IF @changer_role != 'owner'
        BEGIN
            RAISERROR('Only workspace owner can change member roles.', 16, 1);
            RETURN;
        END;
        
        -- Validate new role
        IF @new_role NOT IN ('owner', 'editor', 'viewer')
        BEGIN
            RAISERROR('Invalid role. Must be: owner, editor, or viewer', 16, 1);
            RETURN;
        END;
        
        -- Check if changing from owner
        DECLARE @current_role NVARCHAR(20);
        
        SELECT @current_role = role
        FROM workspace_members
        WHERE workspace_id = @workspace_id
        AND user_id = @user_id
        AND deleted_at IS NULL;
        
        IF @current_role = 'owner' AND @new_role != 'owner'
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
                RAISERROR('Cannot change last owner role. Add another owner first.', 16, 1);
                RETURN;
            END;
        END;
        
        -- Update role
        UPDATE workspace_members
        SET role = @new_role
        WHERE workspace_id = @workspace_id
        AND user_id = @user_id;
        
        COMMIT TRANSACTION;
        
        PRINT 'Member role changed successfully';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
GO

PRINT 'âœ… usp_change_workspace_member_role created successfully';
GO
