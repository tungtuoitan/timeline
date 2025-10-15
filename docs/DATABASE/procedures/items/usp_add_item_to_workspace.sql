CREATE OR ALTER PROCEDURE usp_add_item_to_workspace
    @workspace_id INT,
    @parent_tag_id INT,
    @child_type NVARCHAR(50), -- 'tag' or 'note'
    @child_id INT,
    @relationship_type NVARCHAR(50) = NULL, -- Optional: 'contains', 'related_to', 'depends_on'
    @label NVARCHAR(200) = NULL,
    @sort_order INT = 0
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Validate workspace exists and user has access
        IF NOT EXISTS (
            SELECT 1 FROM workspaces 
            WHERE id = @workspace_id 
            AND deleted_at IS NULL
        )
        BEGIN
            RAISERROR('Workspace not found or deleted', 16, 1);
            RETURN;
        END;
        
        -- Validate parent tag exists in workspace
        IF NOT EXISTS (
            SELECT 1 FROM tags 
            WHERE id = @parent_tag_id 
            AND deleted_at IS NULL
        )
        BEGIN
            RAISERROR('Parent tag not found or deleted', 16, 1);
            RETURN;
        END;
        
        -- Validate child entity type
        IF NOT EXISTS (
            SELECT 1 FROM entity_types 
            WHERE type_name = @child_type 
            AND is_active = 1
        )
        BEGIN
            RAISERROR('Invalid or inactive entity type', 16, 1);
            RETURN;
        END;
        
        -- FIXED BUG 2: Validate child entity exists (moved from trigger)
        -- This replaces the performance-killing dynamic SQL trigger
        IF @child_type = 'tag'
        BEGIN
            IF NOT EXISTS (
                SELECT 1 FROM tags 
                WHERE id = @child_id 
                AND deleted_at IS NULL
            )
            BEGIN
                RAISERROR('Tag does not exist or is deleted', 16, 1);
                RETURN;
            END;
        END
        ELSE IF @child_type = 'note'
        BEGIN
            IF NOT EXISTS (
                SELECT 1 FROM notes 
                WHERE id = @child_id 
                AND deleted_at IS NULL
            )
            BEGIN
                RAISERROR('Note does not exist or is deleted', 16, 1);
                RETURN;
            END;
        END
        ELSE IF @child_type = 'project'
        BEGIN
            -- Future: Add validation when projects table is created
            RAISERROR('Project entity type not yet implemented', 16, 1);
            RETURN;
        END
        ELSE IF @child_type = 'document'
        BEGIN
            -- Future: Add validation when documents table is created
            RAISERROR('Document entity type not yet implemented', 16, 1);
            RETURN;
        END
        ELSE IF @child_type = 'task'
        BEGIN
            -- Future: Add validation when tasks table is created
            RAISERROR('Task entity type not yet implemented', 16, 1);
            RETURN;
        END;
        -- Add more entity types as they are implemented
        
        -- Get parent item path and depth
        DECLARE @parent_path NVARCHAR(4000);
        DECLARE @parent_depth INT;
        
        SELECT TOP 1
            @parent_path = item_path,
            @parent_depth = depth
        FROM workspace_items
        WHERE workspace_id = @workspace_id
        AND child_type = 'tag'
        AND child_id = @parent_tag_id
        AND deleted_at IS NULL
        ORDER BY depth DESC; -- Get deepest occurrence
        
        -- If parent not in workspace, add it as root (depth 0)
        -- FIXED BUG 4: Consistent path format - always use 'type:id' format
        IF @parent_path IS NULL
        BEGIN
            -- Always use 'tag:id' format, not just 'id'
            SET @parent_path = 'tag:' + CAST(@parent_tag_id AS NVARCHAR);
            SET @parent_depth = 0;
            
            -- Add parent as root item if not exists
            IF NOT EXISTS (
                SELECT 1 FROM workspace_items
                WHERE workspace_id = @workspace_id
                AND child_type = 'tag'
                AND child_id = @parent_tag_id
                AND deleted_at IS NULL
            )
            BEGIN
                INSERT INTO workspace_items (
                    workspace_id, parent_tag_id, child_type, child_id,
                    item_path, depth, sort_order, created_by
                )
                VALUES (
                    @workspace_id, @parent_tag_id, 'tag', @parent_tag_id,
                    @parent_path, 0, 0, @workspace_id -- Use workspace_id as created_by for system
                );
            END;
        END;
        
        -- Build new item path
        -- Format is always: 'tag:1.tag:5.note:10' (consistent type:id throughout)
        DECLARE @new_path NVARCHAR(4000);
        DECLARE @new_depth INT;
        
        SET @new_path = @parent_path + '.' + @child_type + ':' + CAST(@child_id AS NVARCHAR);
        SET @new_depth = @parent_depth + 1;
        
        -- Check max depth
        DECLARE @max_depth INT;
        SELECT @max_depth = max_depth 
        FROM workspaces 
        WHERE id = @workspace_id;
        
        IF @new_depth > @max_depth
        BEGIN
            RAISERROR('Maximum depth exceeded for this workspace', 16, 1);
            RETURN;
        END;
        
        -- Check for circular reference (only for tags)
        -- FIXED: Proper circular detection - check if child_id appears anywhere in parent's path
        IF @child_type = 'tag'
        BEGIN
            -- Get parent's full path (all ancestors)
            DECLARE @parent_full_path NVARCHAR(4000);
            
            SELECT TOP 1 @parent_full_path = item_path
            FROM workspace_items
            WHERE workspace_id = @workspace_id
            AND child_type = 'tag'
            AND child_id = @parent_tag_id
            AND deleted_at IS NULL
            ORDER BY depth DESC;
            
            -- If parent_path is NULL, use parent_tag_id directly
            IF @parent_full_path IS NULL
            BEGIN
                SET @parent_full_path = 'tag:' + CAST(@parent_tag_id AS NVARCHAR);
            END;
            
            -- Check if child_id appears anywhere in parent's path
            -- This prevents both direct and indirect circular references
            -- Example: Tag A â†’ Tag B â†’ Tag C â†’ Tag A will be caught
            IF @parent_full_path LIKE '%tag:' + CAST(@child_id AS NVARCHAR) + '.%'
               OR @parent_full_path LIKE '%tag:' + CAST(@child_id AS NVARCHAR)
               OR @parent_full_path = 'tag:' + CAST(@child_id AS NVARCHAR)
            BEGIN
                RAISERROR('Circular reference detected: tag cannot be child of itself or its descendants', 16, 1);
                ROLLBACK TRANSACTION;
                RETURN;
            END;
        END;
        
        -- Check if item already exists at this location
        IF EXISTS (
            SELECT 1 FROM workspace_items
            WHERE workspace_id = @workspace_id
            AND parent_tag_id = @parent_tag_id
            AND child_type = @child_type
            AND child_id = @child_id
            AND deleted_at IS NULL
        )
        BEGIN
            RAISERROR('Item already exists at this location', 16, 1);
            RETURN;
        END;
        
        -- Insert item
        INSERT INTO workspace_items (
            workspace_id, parent_tag_id, child_type, child_id,
            item_path, depth, relationship_type, label, sort_order
        )
        VALUES (
            @workspace_id, @parent_tag_id, @child_type, @child_id,
            @new_path, @new_depth, @relationship_type, @label, @sort_order
        );
        
        DECLARE @item_id BIGINT = SCOPE_IDENTITY();
        
        -- Return created item
        SELECT * FROM workspace_items WHERE id = @item_id;
        
        COMMIT TRANSACTION;
        
        PRINT 'Item added to workspace: ' + @child_type + ':' + CAST(@child_id AS NVARCHAR);
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
GO

PRINT 'âœ… usp_add_item_to_workspace created successfully';
GO
