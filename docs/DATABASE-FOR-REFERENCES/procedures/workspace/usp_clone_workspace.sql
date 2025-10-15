CREATE OR ALTER PROCEDURE usp_clone_workspace
    @source_workspace_id INT,
    @user_id INT,
    @new_name NVARCHAR(200),
    @clone_items BIT = 1 -- Clone workspace_items?
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Check if user has access to source
        IF NOT EXISTS (
            SELECT 1 
            FROM workspace_members
            WHERE workspace_id = @source_workspace_id
            AND user_id = @user_id
            AND deleted_at IS NULL
        )
        BEGIN
            RAISERROR('Access denied to source workspace.', 16, 1);
            RETURN;
        END;
        
        -- Get source workspace details
        DECLARE @type NVARCHAR(20);
        DECLARE @description NVARCHAR(1000);
        DECLARE @color NVARCHAR(7);
        DECLARE @icon NVARCHAR(50);
        DECLARE @max_depth INT;
        
        SELECT 
            @type = type,
            @description = description,
            @color = color,
            @icon = icon,
            @max_depth = max_depth
        FROM workspaces
        WHERE id = @source_workspace_id;
        
        -- Create new workspace
        DECLARE @new_workspace_id INT;
        
        INSERT INTO workspaces (
            user_id, name, type, description, color, icon, max_depth
        )
        VALUES (
            @user_id, @new_name, @type, @description, @color, @icon, @max_depth
        );
        
        SET @new_workspace_id = SCOPE_IDENTITY();
        
        -- Clone relationship types
        INSERT INTO workspace_relationship_types (
            workspace_id, type_name, display_name, description,
            icon, color, line_style, line_width,
            is_bidirectional, allows_cycles, max_depth, sort_order
        )
        SELECT 
            @new_workspace_id, type_name, display_name, description,
            icon, color, line_style, line_width,
            is_bidirectional, allows_cycles, max_depth, sort_order
        FROM workspace_relationship_types
        WHERE workspace_id = @source_workspace_id
        AND deleted_at IS NULL;
        
        -- Clone items if requested
        IF @clone_items = 1
        BEGIN
            INSERT INTO workspace_items (
                workspace_id, parent_tag_id, child_type, child_id,
                relationship_type, item_path, depth, sort_order,
                label, color, icon, created_by
            )
            SELECT 
                @new_workspace_id, parent_tag_id, child_type, child_id,
                relationship_type, item_path, depth, sort_order,
                label, color, icon, @user_id
            FROM workspace_items
            WHERE workspace_id = @source_workspace_id
            AND deleted_at IS NULL;
        END;
        
        -- Return new workspace
        SELECT * FROM workspaces WHERE id = @new_workspace_id;
        
        COMMIT TRANSACTION;
        
        PRINT 'Workspace cloned successfully';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
GO

PRINT 'âœ… usp_clone_workspace created successfully';
GO
