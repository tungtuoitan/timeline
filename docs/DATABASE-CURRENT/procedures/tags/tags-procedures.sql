-- =============================================
-- TAG PROCEDURES (4 procedures)
-- Status: ✅ DEPLOYED (MVP)
-- =============================================

-- =============================================
-- 1. usp_i_tag - Create tag
-- =============================================
CREATE PROCEDURE usp_i_tag
    @user_id INT,
    @name NVARCHAR(255),
    @color NVARCHAR(7) = '#3B82F6',
    @icon NVARCHAR(50) = NULL,
    @description NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- Insert tag
        INSERT INTO tags (user_id, name, color, icon, description)
        VALUES (@user_id, @name, @color, @icon, @description);

        DECLARE @tag_id INT = SCOPE_IDENTITY();

        -- Return created tag
        SELECT * FROM tags WHERE tag_id = @tag_id;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH;
END;
GO

-- =============================================
-- 2. usp_s_tag - Get tag by ID
-- =============================================
CREATE PROCEDURE usp_s_tag
    @tag_id INT,
    @user_id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT * FROM tags
    WHERE tag_id = @tag_id
    AND user_id = @user_id
    AND deleted_at IS NULL;
END;
GO

-- =============================================
-- 3. usp_u_tag - Update tag
-- =============================================
CREATE PROCEDURE usp_u_tag
    @tag_id INT,
    @user_id INT,
    @name NVARCHAR(255) = NULL,
    @color NVARCHAR(7) = NULL,
    @icon NVARCHAR(50) = NULL,
    @description NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Check ownership
    IF NOT EXISTS (
        SELECT 1 FROM tags
        WHERE tag_id = @tag_id
        AND user_id = @user_id
        AND deleted_at IS NULL
    )
    BEGIN
        RAISERROR('Tag not found or access denied', 16, 1);
        RETURN;
    END;

    -- Update tag
    UPDATE tags
    SET
        name = COALESCE(@name, name),
        color = COALESCE(@color, color),
        icon = COALESCE(@icon, icon),
        description = COALESCE(@description, description),
        updated_at = GETUTCDATE()
    WHERE tag_id = @tag_id;

    -- Return updated tag
    SELECT * FROM tags WHERE tag_id = @tag_id;
END;
GO

-- =============================================
-- 4. usp_s_user_tags - List user's tags
-- =============================================
CREATE PROCEDURE usp_s_user_tags
    @user_id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        tag_id,
        name,
        slug,
        color,
        icon,
        description,
        usage_count,
        created_at,
        updated_at
    FROM tags
    WHERE user_id = @user_id
    AND deleted_at IS NULL
    ORDER BY usage_count DESC, name ASC;
END;
GO

-- =============================================
-- 5. usp_s_tag_tree - Get tag tree for workspace
-- =============================================
CREATE PROCEDURE usp_s_tag_tree
    @workspace_id INT,
    @user_id INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Check access to workspace
    IF NOT EXISTS (
        SELECT 1 FROM workspace_members
        WHERE workspace_id = @workspace_id
        AND user_id = @user_id
        AND deleted_at IS NULL
        AND invitation_status = 'active'
    )
    BEGIN
        RAISERROR('Access denied or workspace not found', 16, 1);
        RETURN;
    END;

    -- Create a temp table with all items (tags and notes) for this workspace
    CREATE TABLE #WorkspaceItems (
        item_id INT,
        name NVARCHAR(255),
        slug NVARCHAR(255),
        color NVARCHAR(7),
        icon NVARCHAR(50),
        description NVARCHAR(MAX),
        usage_count INT,
        item_type NVARCHAR(10)
    );

    -- Insert tags
    INSERT INTO #WorkspaceItems
    SELECT 
        t.tag_id, t.name, t.slug, t.color, t.icon, t.description, t.usage_count, 'tag'
    FROM tags t
    WHERE t.user_id = @user_id AND t.deleted_at IS NULL;

    -- Insert notes
    INSERT INTO #WorkspaceItems
    SELECT 
        n.note_id, n.name, n.slug, n.color, n.icon, n.description, 0, 'note'
    FROM notes n
    WHERE n.user_id = @user_id AND n.deleted_at IS NULL;

    -- Build the hierarchy using CTE
    WITH TagHierarchy AS (
        -- Root level tags (tags that are parents but not children in this workspace)
        SELECT 
            wi.item_id,
            wi.name,
            wi.slug,
            wi.color,
            wi.icon,
            wi.description,
            wi.usage_count,
            wi.item_type,
            0 AS depth,
            CAST('/' + CAST(wi.item_id AS VARCHAR(10)) + '/' AS NVARCHAR(4000)) AS path,
            CAST(wi.name AS NVARCHAR(4000)) AS breadcrumb,
            0 AS sort_order,
            CAST(NULL AS INT) AS parent_tag_id
        FROM #WorkspaceItems wi
        WHERE wi.item_type = 'tag'
        AND EXISTS (
            -- Tag appears as parent in workspace_items
            SELECT 1 FROM workspace_items wsi
            WHERE wsi.parent_tag_id = wi.item_id
            AND wsi.workspace_id = @workspace_id
            AND wsi.deleted_at IS NULL
        )
        AND NOT EXISTS (
            -- But tag is NOT a child of another tag in this workspace
            SELECT 1 FROM workspace_items wsi2
            WHERE wsi2.child_type = 'tag'
            AND wsi2.child_id = wi.item_id
            AND wsi2.workspace_id = @workspace_id
            AND wsi2.deleted_at IS NULL
        )

        UNION ALL

        -- Recursive part: child items
        SELECT
            wi_child.item_id,
            wi_child.name,
            wi_child.slug,
            wi_child.color,
            wi_child.icon,
            wi_child.description,
            wi_child.usage_count,
            wi_child.item_type,
            th.depth + 1 AS depth,
            CAST(th.path + CAST(wi_child.item_id AS VARCHAR(10)) + '/' AS NVARCHAR(4000)) AS path,
            CAST(th.breadcrumb + ' > ' + wi_child.name AS NVARCHAR(4000)) AS breadcrumb,
            wsi.sort_order,
            wsi.parent_tag_id
        FROM TagHierarchy th
        INNER JOIN workspace_items wsi ON th.item_id = wsi.parent_tag_id
        INNER JOIN #WorkspaceItems wi_child ON wsi.child_id = wi_child.item_id AND wsi.child_type = wi_child.item_type
        WHERE wsi.workspace_id = @workspace_id
        AND wsi.deleted_at IS NULL
        AND th.depth < 10 -- Prevent infinite recursion
    )
    SELECT
        item_id AS id,
        @user_id AS user_id,
        name,
        slug,
        color,
        icon,
        description,
        usage_count,
        item_type,
        depth AS level,
        path,
        breadcrumb,
        sort_order,
        -- Additional computed fields
        CASE 
            WHEN item_type = 'tag' THEN 
                (SELECT COUNT(*) FROM workspace_items wi_child 
                 WHERE wi_child.parent_tag_id = item_id 
                 AND wi_child.workspace_id = @workspace_id 
                 AND wi_child.deleted_at IS NULL)
            ELSE 0 
        END AS children_count,
        parent_tag_id AS parent_id,
        'owner' AS access_type
    FROM TagHierarchy
    ORDER BY depth, sort_order, name;

    -- Clean up
    DROP TABLE #WorkspaceItems;
END;
GO
