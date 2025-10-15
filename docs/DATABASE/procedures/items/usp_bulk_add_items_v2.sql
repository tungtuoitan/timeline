CREATE OR ALTER PROCEDURE usp_bulk_add_items_v2
    @workspace_id INT,
    @items WorkspaceItemsTableType READONLY
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Validate workspace exists
        IF NOT EXISTS (
            SELECT 1 FROM workspaces 
            WHERE id = @workspace_id 
            AND deleted_at IS NULL
        )
        BEGIN
            RAISERROR('Workspace not found or deleted', 16, 1);
            RETURN;
        END;
        
        -- Validate all parent tags exist
        IF EXISTS (
            SELECT 1 FROM @items i
            WHERE NOT EXISTS (
                SELECT 1 FROM tags 
                WHERE id = i.parent_tag_id 
                AND deleted_at IS NULL
            )
        )
        BEGIN
            RAISERROR('One or more parent tags not found', 16, 1);
            RETURN;
        END;
        
        -- Validate all tags exist (if child_type = 'tag')
        IF EXISTS (
            SELECT 1 FROM @items i
            WHERE i.child_type = 'tag'
            AND NOT EXISTS (
                SELECT 1 FROM tags 
                WHERE id = i.child_id 
                AND deleted_at IS NULL
            )
        )
        BEGIN
            RAISERROR('One or more child tags not found', 16, 1);
            RETURN;
        END;
        
        -- Validate all notes exist (if child_type = 'note')
        IF EXISTS (
            SELECT 1 FROM @items i
            WHERE i.child_type = 'note'
            AND NOT EXISTS (
                SELECT 1 FROM notes 
                WHERE id = i.child_id 
                AND deleted_at IS NULL
            )
        )
        BEGIN
            RAISERROR('One or more notes not found', 16, 1);
            RETURN;
        END;
        
        -- Build paths for all items in a single operation
        -- This is much faster than calling procedure for each item
        WITH parent_paths AS (
            SELECT DISTINCT
                i.parent_tag_id,
                COALESCE(
                    (SELECT TOP 1 item_path 
                     FROM workspace_items 
                     WHERE workspace_id = @workspace_id
                     AND child_type = 'tag'
                     AND child_id = i.parent_tag_id
                     AND deleted_at IS NULL
                     ORDER BY depth DESC),
                    'tag:' + CAST(i.parent_tag_id AS NVARCHAR)
                ) AS parent_path,
                COALESCE(
                    (SELECT TOP 1 depth 
                     FROM workspace_items 
                     WHERE workspace_id = @workspace_id
                     AND child_type = 'tag'
                     AND child_id = i.parent_tag_id
                     AND deleted_at IS NULL
                     ORDER BY depth DESC),
                    0
                ) AS parent_depth
            FROM @items i
        )
        INSERT INTO workspace_items (
            workspace_id,
            parent_tag_id,
            child_type,
            child_id,
            item_path,
            depth,
            relationship_type,
            label,
            sort_order
        )
        SELECT 
            @workspace_id,
            i.parent_tag_id,
            i.child_type,
            i.child_id,
            pp.parent_path + '.' + i.child_type + ':' + CAST(i.child_id AS NVARCHAR),
            pp.parent_depth + 1,
            i.relationship_type,
            i.label,
            i.sort_order
        FROM @items i
        JOIN parent_paths pp ON pp.parent_tag_id = i.parent_tag_id
        WHERE NOT EXISTS (
            -- Prevent duplicates
            SELECT 1 FROM workspace_items
            WHERE workspace_id = @workspace_id
            AND parent_tag_id = i.parent_tag_id
            AND child_type = i.child_type
            AND child_id = i.child_id
            AND deleted_at IS NULL
        );
        
        DECLARE @rows_inserted INT = @@ROWCOUNT;
        
        COMMIT TRANSACTION;
        
        PRINT 'Bulk items added: ' + CAST(@rows_inserted AS NVARCHAR);
        
        SELECT @rows_inserted AS items_added;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
GO

PRINT 'âœ… usp_bulk_add_items_v2 created successfully';
GO
