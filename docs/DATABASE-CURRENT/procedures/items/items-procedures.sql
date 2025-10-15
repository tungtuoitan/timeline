-- =============================================
-- ITEMS PROCEDURES (5 procedures)
-- Manages workspace_items (UNIFIED table)
-- Status: ✅ DEPLOYED (MVP)
-- =============================================

-- =============================================
-- 1. usp_add_item_to_workspace - Add item to workspace
-- =============================================
CREATE PROCEDURE usp_add_item_to_workspace
    @workspace_id INT,
    @parent_tag_id INT,
    @child_type NVARCHAR(50),
    @child_id INT,
    @added_by INT,
    @relationship_type NVARCHAR(100) = NULL,
    @sort_order INT = 0
AS
BEGIN
    SET NOCOUNT ON;

    -- Check access
    IF NOT EXISTS (
        SELECT 1 FROM workspace_members
        WHERE workspace_id = @workspace_id
        AND user_id = @added_by
        AND role IN ('owner', 'editor')
        AND deleted_at IS NULL
    )
    BEGIN
        RAISERROR('Access denied', 16, 1);
        RETURN;
    END;

    -- Validate child_type exists
    IF NOT EXISTS (SELECT 1 FROM entity_types WHERE type_name = @child_type)
    BEGIN
        RAISERROR('Invalid entity type', 16, 1);
        RETURN;
    END;

    -- Insert item
    INSERT INTO workspace_items (
        workspace_id, parent_tag_id, child_type, child_id,
        relationship_type, sort_order, added_by
    )
    VALUES (
        @workspace_id, @parent_tag_id, @child_type, @child_id,
        @relationship_type, @sort_order, @added_by
    );

    DECLARE @item_id BIGINT = SCOPE_IDENTITY();

    -- Return created item
    SELECT * FROM workspace_items WHERE item_id = @item_id;
END;
GO

-- =============================================
-- 2. usp_s_workspace_items - Get workspace items
-- =============================================
CREATE PROCEDURE usp_s_workspace_items
    @workspace_id INT,
    @user_id INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Check access
    IF NOT EXISTS (
        SELECT 1 FROM workspace_members
        WHERE workspace_id = @workspace_id
        AND user_id = @user_id
        AND deleted_at IS NULL
        AND invitation_status = 'active'
    )
    BEGIN
        RAISERROR('Access denied', 16, 1);
        RETURN;
    END;

    -- Return items
    SELECT
        wi.item_id,
        wi.workspace_id,
        wi.parent_tag_id,
        wi.child_type,
        wi.child_id,
        wi.relationship_type,
        wi.sort_order,
        wi.created_at,
        -- Get parent tag info
        pt.name AS parent_tag_name,
        pt.color AS parent_tag_color,
        -- Get child tag info (if child is tag)
        ct.name AS child_tag_name,
        ct.color AS child_tag_color
    FROM workspace_items wi
    LEFT JOIN tags pt ON wi.parent_tag_id = pt.tag_id
    LEFT JOIN tags ct ON wi.child_type = 'tag' AND wi.child_id = ct.tag_id
    WHERE wi.workspace_id = @workspace_id
    AND wi.deleted_at IS NULL
    ORDER BY wi.parent_tag_id, wi.sort_order;
END;
GO

-- =============================================
-- 3. usp_move_item - Move item to different parent
-- =============================================
CREATE PROCEDURE usp_move_item
    @item_id BIGINT,
    @new_parent_tag_id INT,
    @user_id INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @workspace_id INT;

    -- Get workspace_id and check access
    SELECT @workspace_id = workspace_id
    FROM workspace_items
    WHERE item_id = @item_id;

    IF NOT EXISTS (
        SELECT 1 FROM workspace_members
        WHERE workspace_id = @workspace_id
        AND user_id = @user_id
        AND role IN ('owner', 'editor')
        AND deleted_at IS NULL
    )
    BEGIN
        RAISERROR('Access denied', 16, 1);
        RETURN;
    END;

    -- Move item
    UPDATE workspace_items
    SET parent_tag_id = @new_parent_tag_id,
        updated_at = GETUTCDATE()
    WHERE item_id = @item_id;

    PRINT 'Item moved successfully';
END;
GO

-- =============================================
-- 4. usp_remove_item - Remove item (soft delete)
-- =============================================
CREATE PROCEDURE usp_remove_item
    @item_id BIGINT,
    @user_id INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @workspace_id INT;

    -- Get workspace_id and check access
    SELECT @workspace_id = workspace_id
    FROM workspace_items
    WHERE item_id = @item_id;

    IF NOT EXISTS (
        SELECT 1 FROM workspace_members
        WHERE workspace_id = @workspace_id
        AND user_id = @user_id
        AND role IN ('owner', 'editor')
        AND deleted_at IS NULL
    )
    BEGIN
        RAISERROR('Access denied', 16, 1);
        RETURN;
    END;

    -- Soft delete
    UPDATE workspace_items
    SET deleted_at = GETUTCDATE()
    WHERE item_id = @item_id;

    PRINT 'Item removed successfully';
END;
GO

-- =============================================
-- 5. usp_s_item_path - Get item path (breadcrumb)
-- =============================================
CREATE PROCEDURE usp_s_item_path
    @item_id BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    -- Return item with parent tag info
    SELECT
        wi.item_id,
        wi.parent_tag_id,
        pt.name AS parent_tag_name,
        wi.child_type,
        wi.child_id,
        CASE
            WHEN wi.child_type = 'tag' THEN ct.name
            ELSE NULL
        END AS child_name
    FROM workspace_items wi
    LEFT JOIN tags pt ON wi.parent_tag_id = pt.tag_id
    LEFT JOIN tags ct ON wi.child_type = 'tag' AND wi.child_id = ct.tag_id
    WHERE wi.item_id = @item_id;
END;
GO
