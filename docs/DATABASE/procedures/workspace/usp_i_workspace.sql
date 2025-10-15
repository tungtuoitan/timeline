CREATE OR ALTER PROCEDURE usp_i_workspace
    @user_id INT,
    @name NVARCHAR(200),
    @type NVARCHAR(20) = 'hierarchy',
    @description NVARCHAR(1000) = NULL,
    @color NVARCHAR(7) = NULL,
    @icon NVARCHAR(50) = NULL,
    @max_depth INT = 10,
    @is_default BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Validate type
        IF @type NOT IN ('hierarchy', 'graph', 'network', 'timeline', 'custom')
        BEGIN
            RAISERROR('Invalid workspace type. Must be: hierarchy, graph, network, timeline, or custom', 16, 1);
            RETURN;
        END;
        
        -- If setting as default, unset other defaults
        IF @is_default = 1
        BEGIN
            UPDATE workspaces
            SET is_default = 0
            WHERE user_id = @user_id
            AND is_default = 1
            AND deleted_at IS NULL;
        END;
        
        -- Create workspace
        INSERT INTO workspaces (
            user_id, name, type, description, color, icon,
            max_depth, is_default
        )
        VALUES (
            @user_id, @name, @type, @description, @color, @icon,
            @max_depth, @is_default
        );
        
        DECLARE @workspace_id INT = SCOPE_IDENTITY();
        
        -- Owner is automatically added by trigger tr_workspace_add_owner
        
        -- Return created workspace
        SELECT * FROM workspaces WHERE id = @workspace_id;
        
        COMMIT TRANSACTION;
        
        PRINT 'Workspace created: ' + @name;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
GO

PRINT 'âœ… usp_i_workspace created successfully';
GO
