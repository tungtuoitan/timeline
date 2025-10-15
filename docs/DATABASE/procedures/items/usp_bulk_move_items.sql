CREATE OR ALTER PROCEDURE usp_bulk_move_items
    @workspace_id INT,
    @items MoveItemsTableType READONLY
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
        
        -- Validate all items exist in workspace
        IF EXISTS (
            SELECT 1 FROM @items i
            WHERE NOT EXISTS (
                SELECT 1 FROM workspace_items
                WHERE id = i.item_id
                AND workspace_id = @workspace_id
                AND deleted_at IS NULL
            )
        )
        BEGIN
            RAISERROR('One or more items not found in workspace', 16, 1);
            RETURN;
        END;
        
        -- Validate all new parent tags exist
        IF EXISTS (
            SELECT 1 FROM @items i
            WHERE NOT EXISTS (
                SELECT 1 FROM tags
                WHERE id = i.new_parent_tag_id
                AND deleted_at IS NULL
            )
        )
        BEGIN
            RAISERROR('One or more parent tags not found', 16, 1);
            RETURN;
        END;
        
        -- Check for circular references
        -- Cannot move item to be a child of itself or its descendants
        IF EXISTS (
            SELECT 1 FROM @items move
            INNER JOIN workspace_items wi ON wi.id = move.item_id
            INNER JOIN workspace_items parent ON 
                parent.workspace_id = @workspace_id
                AND parent.child_type = 'tag'
                AND parent.child_id = move.new_parent_tag_id
                AND parent.deleted_at IS NULL
            WHERE parent.item_path LIKE wi.item_path + '%'
        )
        BEGIN
            RAISERROR('Circular reference detected: Cannot move item to its own descendant', 16, 1);
            RETURN;
        END;
        
        -- Perform all moves in batch
        -- Step 1: Update parent, depth, sort_order
        UPDATE wi
        SET 
            parent_tag_id = move.new_parent_tag_id,
            depth = new_parent.depth + 1,
            sort_order = COALESCE(move.new_sort_order, wi.sort_order),
            updated_at = GETUTCDATE()
        FROM workspace_items wi
        INNER JOIN @items move ON wi.id = move.item_id
        CROSS APPLY (
            -- Get new parent's depth
            SELECT COALESCE(MAX(depth), 0) AS depth
            FROM workspace_items
            WHERE workspace_id = @workspace_id
            AND child_type = 'tag'
            AND child_id = move.new_parent_tag_id
            AND deleted_at IS NULL
        ) new_parent;
        
        -- Step 2: Rebuild paths for moved items and their descendants
        -- This is done in a set-based operation (much faster than cursor)
        WITH moved_items AS (
            SELECT 
                wi.id AS item_id,
                wi.child_type,
                wi.child_id,
                -- Build new path
                COALESCE(
                    (SELECT TOP 1 item_path 
                     FROM workspace_items
                     WHERE workspace_id = @workspace_id
                     AND child_type = 'tag'
                     AND child_id = wi.parent_tag_id
                     AND deleted_at IS NULL
                     ORDER BY depth DESC),
                    'tag:' + CAST(wi.parent_tag_id AS NVARCHAR)
                ) + '.' + wi.child_type + ':' + CAST(wi.child_id AS NVARCHAR) AS new_path,
                wi.item_path AS old_path
            FROM workspace_items wi
            INNER JOIN @items move ON wi.id = move.item_id
            WHERE wi.workspace_id = @workspace_id
            AND wi.deleted_at IS NULL
        )
        UPDATE wi
        SET 
            item_path = mi.new_path + 
                CASE 
                    WHEN wi.id = mi.item_id THEN ''
                    ELSE SUBSTRING(wi.item_path, LEN(mi.old_path) + 1, LEN(wi.item_path))
                END,
            depth = mi_parent.depth + 1 + 
                CASE 
                    WHEN wi.id = mi.item_id THEN 0
                    ELSE (LEN(wi.item_path) - LEN(mi.old_path) - 1) / 10 -- Approximate depth difference
                END
        FROM workspace_items wi
        CROSS APPLY (
            -- Find which moved item this descendant belongs to
            SELECT TOP 1 *
            FROM moved_items
            WHERE wi.item_path LIKE old_path + '%'
            ORDER BY LEN(old_path) DESC
        ) mi
        CROSS APPLY (
            -- Get moved item's new parent depth
            SELECT COALESCE(MAX(depth), 0) AS depth
            FROM workspace_items
            WHERE workspace_id = @workspace_id
            AND id = (SELECT id FROM workspace_items WHERE workspace_id = @workspace_id AND child_type = mi.child_type AND child_id = mi.child_id AND deleted_at IS NULL)
            AND deleted_at IS NULL
        ) mi_parent
        WHERE wi.workspace_id = @workspace_id
        AND wi.deleted_at IS NULL;
        
        DECLARE @rows_moved INT = (SELECT COUNT(*) FROM @items);
        
        COMMIT TRANSACTION;
        
        PRINT 'Bulk items moved: ' + CAST(@rows_moved AS NVARCHAR);
        
        SELECT @rows_moved AS items_moved;
        
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH;
END;
GO
GO

PRINT 'âœ… usp_bulk_move_items created successfully';
GO
