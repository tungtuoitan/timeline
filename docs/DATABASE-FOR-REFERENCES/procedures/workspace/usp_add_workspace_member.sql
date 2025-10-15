CREATE OR ALTER PROCEDURE usp_add_workspace_member
    @workspace_id INT,
    @user_id INT, -- User to add
    @role NVARCHAR(20) = 'viewer',
    @added_by INT -- Current user (must be owner)
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Check if adding user is owner
        DECLARE @adder_role NVARCHAR(20);
        
        SELECT @adder_role = role
        FROM workspace_members
        WHERE workspace_id = @workspace_id
        AND user_id = @added_by
        AND deleted_at IS NULL;
        
        IF @adder_role != 'owner'
        BEGIN
            RAISERROR('Only workspace owner can add members.', 16, 1);
            RETURN;
        END;
        
        -- Check if user already member
        IF EXISTS (
            SELECT 1
            FROM workspace_members
            WHERE workspace_id = @workspace_id
            AND user_id = @user_id
            AND deleted_at IS NULL
        )
        BEGIN
            RAISERROR('User is already a member of this workspace.', 16, 1);
            RETURN;
        END;
        
        -- Validate role
        IF @role NOT IN ('owner', 'editor', 'viewer')
        BEGIN
            RAISERROR('Invalid role. Must be: owner, editor, or viewer', 16, 1);
            RETURN;
        END;
        
        -- Add member
        INSERT INTO workspace_members (
            workspace_id, user_id, role, 
            invited_by, invitation_status, joined_at
        )
        VALUES (
            @workspace_id, @user_id, @role,
            @added_by, 'active', GETUTCDATE()
        );
        
        COMMIT TRANSACTION;
        
        PRINT 'Member added successfully';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
GO

PRINT 'âœ… usp_add_workspace_member created successfully';
GO
