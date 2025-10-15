-- ============================================
-- FILE: procedures/workspace/usp_s_item_path.sql
-- PURPOSE: Get breadcrumb path for workspace item
-- DEPENDENCIES: workspace_items, tags
-- ============================================

PRINT '';
PRINT '📝 Creating procedure: usp_s_item_path';
PRINT '   Purpose: Get full breadcrumb path for item';
PRINT '';

GO
CREATE OR ALTER PROCEDURE usp_s_item_path
    @workspace_id INT,
    @item_id INT
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
        
        -- Validate item exists
        IF NOT EXISTS (
            SELECT 1 FROM workspace_items 
            WHERE id = @item_id 
            AND workspace_id = @workspace_id 
            AND deleted_at IS NULL
        )
        BEGIN
            RAISERROR('Item not found in workspace', 16, 1);
            RETURN;
        END
        
        -- Get item info
        DECLARE @parent_tag_id INT;
        DECLARE @child_type NVARCHAR(50);
        DECLARE @child_id INT;
        
        SELECT 
            @parent_tag_id = parent_tag_id,
            @child_type = child_type,
            @child_id = child_id
        FROM workspace_items
        WHERE id = @item_id;
        
        -- Build breadcrumb path from workspace_items hierarchy
        -- (tags table is FLAT, hierarchy stored in workspace_items)
        -- Strategy: Use CTE without joins, then join to tags in final SELECT
        WITH ItemPath AS (
            -- Start from current item (no joins in CTE)
            SELECT 
                wi.id,
                wi.parent_tag_id,
                wi.child_type,
                wi.child_id,
                wi.depth,
                wi.item_path,
                wi.label,
                wi.color,
                wi.icon,
                0 as level
            FROM workspace_items wi
            WHERE wi.id = @item_id
            AND wi.deleted_at IS NULL
            
            UNION ALL
            
            -- Recursively get parent items (no joins allowed!)
            SELECT 
                wi.id,
                wi.parent_tag_id,
                wi.child_type,
                wi.child_id,
                wi.depth,
                wi.item_path,
                wi.label,
                wi.color,
                wi.icon,
                ip.level + 1
            FROM workspace_items wi
            INNER JOIN ItemPath ip ON wi.child_id = ip.parent_tag_id 
                                   AND wi.child_type = 'tag'
            WHERE wi.workspace_id = @workspace_id
            AND wi.deleted_at IS NULL
        )
        -- Join to tags AFTER CTE completes
        SELECT 
            ip.id as item_id,
            ip.child_type,
            ip.child_id,
            CASE 
                WHEN ip.child_type = 'tag' THEN t.name
                ELSE ip.label
            END as item_name,
            ip.depth,
            ip.item_path,
            CASE 
                WHEN ip.child_type = 'tag' THEN t.color
                ELSE ip.color
            END as item_color,
            CASE 
                WHEN ip.child_type = 'tag' THEN t.icon
                ELSE ip.icon
            END as item_icon,
            ip.level
        FROM ItemPath ip
        LEFT JOIN tags t ON ip.child_type = 'tag' AND ip.child_id = t.id
        ORDER BY ip.level DESC;  -- Root to leaf
        
    END TRY
    BEGIN CATCH
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END;
GO

PRINT '   ✅ usp_s_item_path created';
GO
