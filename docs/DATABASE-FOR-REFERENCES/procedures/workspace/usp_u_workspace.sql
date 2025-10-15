CREATE OR ALTER PROCEDURE usp_u_workspace
    @workspace_id INT,
    @user_id INT,
    @name NVARCHAR(200) = NULL,
    @description NVARCHAR(1000) = NULL,
    @color NVARCHAR(7) = NULL,
    @icon NVARCHAR(50) = NULL,
    @is_archived BIT = NULL,
    @is_default BIT = NULL
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
        
        IF @user_role IS NULL
        BEGIN
            RAISERROR('Access denied. User is not a member of this workspace.', 16, 1);
            RETURN;
        END;
        
        -- Only owner can archive or set as default
        IF (@is_archived IS NOT NULL OR @is_default IS NOT NULL) 
           AND @user_role != 'owner'
        BEGIN
            RAISERROR('Only workspace owner can archive or set as default.', 16, 1);
            RETURN;
        END;
        
        -- If setting as default, unset other defaults
        IF @is_default = 1
        BEGIN
            UPDATE workspaces
            SET is_default = 0
            WHERE user_id = (SELECT user_id FROM workspaces WHERE id = @workspace_id)
            AND is_default = 1
            AND id != @workspace_id
            AND deleted_at IS NULL;
        END;
        
        -- Update workspace
        UPDATE workspaces
        SET 
            name = ISNULL(@name, name),
            description = ISNULL(@description, description),
            color = ISNULL(@color, color),
            icon = ISNULL(@icon, icon),
            is_archived = ISNULL(@is_archived, is_archived),
            is_default = ISNULL(@is_default, is_default),
            updated_at = GETUTCDATE()
        WHERE id = @workspace_id
        AND deleted_at IS NULL;
        
        -- Return updated workspace
        SELECT * FROM workspaces WHERE id = @workspace_id;
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
GO

PRINT 'âœ… usp_u_workspace created successfully';
GO
