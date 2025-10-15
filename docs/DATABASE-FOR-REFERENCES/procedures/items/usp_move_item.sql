CREATE OR ALTER PROCEDURE usp_move_item
    @item_id BIGINT,
    @new_parent_tag_id INT,
    @new_sort_order INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Get current item info
        DECLARE @workspace_id INT;
        DECLARE @old_parent_id INT;
        DECLARE @child_type NVARCHAR(50);
        DECLARE @child_id INT;
        DECLARE @old_path NVARCHAR(4000);
        DECLARE @old_depth INT;
        
        SELECT 
            @workspace_id = workspace_id,
            @old_parent_id = parent_tag_id,
            @child_type = child_type,
            @child_id = child_id,
            @old_path = item_path,
            @old_depth = depth
        FROM workspace_items
        WHERE id = @item_id
        AND deleted_at IS NULL;
        
        IF @workspace_id IS NULL
        BEGIN
            RAISERROR('Item not found or deleted', 16, 1);
            RETURN;
        END;
        
        -- ðŸ› FIX: Validate new parent exists AND is in the same workspace
        -- Problem: Could move item to tag that exists but not in workspace â†’ orphaned item!
        -- Solution: Check both tag existence and workspace membership
        IF NOT EXISTS (
            SELECT 1 FROM tags 
            WHERE id = @new_parent_tag_id 
            AND deleted_at IS NULL
        )
        BEGIN
            RAISERROR('New parent tag not found or deleted', 16, 1);
            RETURN;
        END;
        
        -- âœ… Additional validation: Ensure parent tag is in the same workspace
        IF NOT EXISTS (
            SELECT 1 FROM workspace_items
            WHERE workspace_id = @workspace_id
            AND child_type = 'tag'
            AND child_id = @new_parent_tag_id
            AND deleted_at IS NULL
        )
        BEGIN
            RAISERROR('New parent tag not found in this workspace', 16, 1);
            RETURN;
        END;
        
        -- Check for circular reference (only for tags)
        IF @child_type = 'tag'
        BEGIN
            -- Get all descendants of this tag
            IF EXISTS (
                SELECT 1 FROM workspace_items
                WHERE workspace_id = @workspace_id
                AND item_path LIKE @old_path + '%'
                AND child_type = 'tag'
                AND child_id = @new_parent_tag_id
                AND deleted_at IS NULL
            )
            BEGIN
                RAISERROR('Circular reference: cannot move tag to its own descendant', 16, 1);
                RETURN;
            END;
        END;
        
        -- Get new parent path and depth
        DECLARE @new_parent_path NVARCHAR(4000);
        DECLARE @new_parent_depth INT;
        
        SELECT TOP 1
            @new_parent_path = item_path,
            @new_parent_depth = depth
        FROM workspace_items
        WHERE workspace_id = @workspace_id
        AND child_type = 'tag'
        AND child_id = @new_parent_tag_id
        AND deleted_at IS NULL
        ORDER BY depth DESC;
        
        IF @new_parent_path IS NULL
        BEGIN
            -- ðŸ› FIX BUG 4: Use consistent 'tag:id' format for root-level path
            SET @new_parent_path = 'tag:' + CAST(@new_parent_tag_id AS NVARCHAR);
            SET @new_parent_depth = 0;
        END;
        
        -- Calculate new path and depth
        DECLARE @new_path NVARCHAR(4000);
        DECLARE @new_depth INT;
        
        SET @new_path = @new_parent_path + '.' + @child_type + ':' + CAST(@child_id AS NVARCHAR);
        SET @new_depth = @new_parent_depth + 1;
        
        -- Check max depth
        DECLARE @max_depth INT;
        SELECT @max_depth = max_depth FROM workspaces WHERE id = @workspace_id;
        
        IF @new_depth > @max_depth
        BEGIN
            RAISERROR('Maximum depth exceeded for this workspace', 16, 1);
            RETURN;
        END;
        
        -- Update item and all descendants
        -- First, collect all affected items
        DECLARE @old_path_like NVARCHAR(4000) = @old_path + '%';
        
        -- Update descendants' paths
        UPDATE workspace_items
        SET 
            item_path = REPLACE(item_path, @old_path, @new_path),
            depth = depth - @old_depth + @new_depth
        WHERE workspace_id = @workspace_id
        AND item_path LIKE @old_path_like
        AND deleted_at IS NULL;
        
        -- Update main item
        UPDATE workspace_items
        SET 
            parent_tag_id = @new_parent_tag_id,
            item_path = @new_path,
            depth = @new_depth,
            sort_order = ISNULL(@new_sort_order, sort_order),
            updated_at = GETUTCDATE()
        WHERE id = @item_id;
        
        -- Return updated item
        SELECT * FROM workspace_items WHERE id = @item_id;
        
        COMMIT TRANSACTION;
        
        PRINT 'Item moved successfully';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
GO

PRINT 'âœ… usp_move_item created successfully';
GO
