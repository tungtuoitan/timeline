-- =============================================
-- Stored Procedure: usp_update_workspace_item
-- Purpose: Update workspace item metadata (label, notes, color, icon, sort_order)
-- Created: 2025-10-22
-- =============================================

CREATE OR ALTER PROCEDURE usp_update_workspace_item
    @item_id BIGINT,
    @user_id INT,
    @label NVARCHAR(200) = NULL,
    @notes NVARCHAR(2000) = NULL,
    @color NVARCHAR(7) = NULL,
    @icon NVARCHAR(50) = NULL,
    @sort_order INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @workspace_id INT;

    -- Get workspace_id and check if item exists
    SELECT @workspace_id = workspace_id
    FROM workspace_items
    WHERE item_id = @item_id
    AND deleted_at IS NULL;

    IF @workspace_id IS NULL
    BEGIN
        RAISERROR('Workspace item not found', 16, 1);
        RETURN;
    END;

    -- Check if user has access to modify (owner or editor)
    IF NOT EXISTS (
        SELECT 1 FROM workspace_members
        WHERE workspace_id = @workspace_id
        AND user_id = @user_id
        AND role IN ('owner', 'editor')
        AND deleted_at IS NULL
        AND invitation_status = 'active'
    )
    BEGIN
        RAISERROR('Access denied - you do not have permission to modify this workspace', 16, 1);
        RETURN;
    END;

    -- Validate color format if provided
    IF @color IS NOT NULL AND @color NOT LIKE '#[0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f]'
    BEGIN
        RAISERROR('Invalid color format. Must be #RRGGBB', 16, 1);
        RETURN;
    END;

    -- Update the item (only update fields that are provided)
    UPDATE workspace_items
    SET 
        label = COALESCE(@label, label),
        notes = COALESCE(@notes, notes),
        color = COALESCE(@color, color),
        icon = COALESCE(@icon, icon),
        sort_order = COALESCE(@sort_order, sort_order),
        updated_at = GETUTCDATE()
    WHERE item_id = @item_id;

    -- Return the updated item
    SELECT
        wi.item_id,
        wi.workspace_id,
        wi.parent_tag_id,
        wi.child_type,
        wi.child_id,
        wi.label,
        wi.notes,
        wi.color,
        wi.icon,
        wi.sort_order,
        wi.created_at,
        wi.updated_at,
        wi.added_by
    FROM workspace_items wi
    WHERE wi.item_id = @item_id;

    -- Log success
    PRINT 'Workspace item updated successfully';
END;
GO
