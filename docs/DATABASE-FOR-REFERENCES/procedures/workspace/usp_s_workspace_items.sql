-- ============================================
-- FILE: procedures/workspace/usp_s_workspace_items.sql
-- PURPOSE: List items in workspace with hierarchy
-- DEPENDENCIES: workspace_items, tags, workspaces
-- ============================================

PRINT '';
PRINT '📝 Creating procedure: usp_s_workspace_items';
PRINT '   Purpose: List all items in workspace with tag tree';
PRINT '';

GO
CREATE OR ALTER PROCEDURE usp_s_workspace_items
    @workspace_id INT,
    @parent_tag_id INT = NULL,
    @entity_type NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        -- Validate workspace exists
        IF NOT EXISTS (SELECT 1 FROM workspaces WHERE id = @workspace_id AND deleted_at IS NULL)
        BEGIN
            RAISERROR('Workspace not found', 16, 1);
            RETURN;
        END
        
        -- Return workspace items
        SELECT 
            wi.id,
            wi.workspace_id,
            wi.parent_tag_id,
            wi.child_type,
            wi.child_id,
            wi.relationship_type,
            wi.item_path,
            wi.depth,
            wi.sort_order,
            wi.color,
            wi.icon,
            wi.added_by,
            wi.created_at,
            wi.updated_at,
            
            -- Parent tag info
            pt.name as parent_tag_name,
            pt.slug as parent_tag_slug,
            pt.color as parent_tag_color,
            pt.icon as parent_tag_icon,
            
            -- Child info (if tag)
            CASE 
                WHEN wi.child_type = 'tag' THEN ct.name
                ELSE NULL
            END as child_name,
            
            CASE 
                WHEN wi.child_type = 'tag' THEN ct.slug
                ELSE NULL
            END as child_slug,
            
            CASE 
                WHEN wi.child_type = 'tag' THEN ct.color
                ELSE NULL
            END as child_color,
            
            CASE 
                WHEN wi.child_type = 'tag' THEN ct.icon
                ELSE NULL
            END as child_icon,
            
            -- Child count (how many items under this tag)
            (SELECT COUNT(*) 
             FROM workspace_items child
             WHERE child.workspace_id = wi.workspace_id
             AND child.parent_tag_id = wi.child_id
             AND child.child_type = 'tag'
             AND child.deleted_at IS NULL
            ) as child_count,
            
            -- Item count (entities under this tag, excluding tags)
            (SELECT COUNT(*)
             FROM workspace_items items
             WHERE items.workspace_id = wi.workspace_id
             AND items.parent_tag_id = wi.child_id
             AND items.child_type != 'tag'
             AND items.deleted_at IS NULL
            ) as item_count
            
        FROM workspace_items wi
        LEFT JOIN tags pt ON wi.parent_tag_id = pt.id
        LEFT JOIN tags ct ON wi.child_type = 'tag' AND wi.child_id = ct.id
        
        WHERE wi.workspace_id = @workspace_id
        AND (@parent_tag_id IS NULL OR wi.parent_tag_id = @parent_tag_id)
        AND (@entity_type IS NULL OR wi.child_type = @entity_type)
        AND wi.deleted_at IS NULL
        
        ORDER BY 
            wi.sort_order,
            wi.child_type,
            CASE WHEN wi.child_type = 'tag' THEN ct.name ELSE '' END;
        
    END TRY
    BEGIN CATCH
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END;
GO

PRINT '   ✅ usp_s_workspace_items created';
GO
