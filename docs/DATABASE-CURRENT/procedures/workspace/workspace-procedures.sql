-- =============================================
-- WORKSPACE PROCEDURES (6 procedures)
-- Status: ✅ DEPLOYED (MVP)
-- =============================================

-- =============================================
-- 1. usp_i_workspace - Create workspace
-- =============================================
CREATE PROCEDURE usp_i_workspace
    @user_id INT,
    @name NVARCHAR(200),
    @type NVARCHAR(20) = 'hierarchy',
    @description NVARCHAR(1000) = NULL,
    @color NVARCHAR(7) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Validate type
        IF @type NOT IN ('hierarchy', 'graph', 'network', 'timeline', 'custom')
        BEGIN
            RAISERROR('Invalid workspace type', 16, 1);
            RETURN;
        END;

        -- Create workspace
        INSERT INTO workspaces (user_id, name, type, description, color)
        VALUES (@user_id, @name, @type, @description, @color);

        DECLARE @workspace_id INT = SCOPE_IDENTITY();

        -- Return created workspace
        SELECT * FROM workspaces WHERE workspace_id = @workspace_id;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

-- =============================================
-- 2. usp_s_workspace - Get workspace by ID
-- =============================================
CREATE PROCEDURE usp_s_workspace
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
        RAISERROR('Access denied or workspace not found', 16, 1);
        RETURN;
    END;

    -- Return workspace details
    SELECT * FROM workspaces
    WHERE workspace_id = @workspace_id
    AND deleted_at IS NULL;
END;
GO

-- =============================================
-- 3. usp_s_user_workspaces - List user's workspaces
-- =============================================
CREATE PROCEDURE usp_s_user_workspaces
    @user_id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        w.workspace_id,
        w.name,
        w.type,
        w.description,
        w.color,
        w.icon,
        w.tag_count,
        w.member_count,
        w.is_default,
        w.created_at,
        w.last_accessed_at,
        wm.role
    FROM workspaces w
    INNER JOIN workspace_members wm ON w.workspace_id = wm.workspace_id
    WHERE wm.user_id = @user_id
    AND w.deleted_at IS NULL
    AND wm.deleted_at IS NULL
    AND wm.invitation_status = 'active'
    ORDER BY w.is_default DESC, w.last_accessed_at DESC;
END;
GO

-- =============================================
-- 4. usp_u_workspace - Update workspace
-- =============================================
CREATE PROCEDURE usp_u_workspace
    @workspace_id INT,
    @user_id INT,
    @name NVARCHAR(200) = NULL,
    @description NVARCHAR(1000) = NULL,
    @color NVARCHAR(7) = NULL,
    @icon NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Check owner permission
    IF NOT EXISTS (
        SELECT 1 FROM workspace_members
        WHERE workspace_id = @workspace_id
        AND user_id = @user_id
        AND role = 'owner'
        AND deleted_at IS NULL
    )
    BEGIN
        RAISERROR('Only workspace owner can update workspace', 16, 1);
        RETURN;
    END;

    -- Update workspace
    UPDATE workspaces
    SET
        name = COALESCE(@name, name),
        description = COALESCE(@description, description),
        color = COALESCE(@color, color),
        icon = COALESCE(@icon, icon),
        updated_at = GETUTCDATE()
    WHERE workspace_id = @workspace_id;

    -- Return updated workspace
    SELECT * FROM workspaces WHERE workspace_id = @workspace_id;
END;
GO

-- =============================================
-- 5. usp_d_workspace - Delete workspace (soft)
-- =============================================
CREATE PROCEDURE usp_d_workspace
    @workspace_id INT,
    @user_id INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Check owner permission
    IF NOT EXISTS (
        SELECT 1 FROM workspace_members
        WHERE workspace_id = @workspace_id
        AND user_id = @user_id
        AND role = 'owner'
        AND deleted_at IS NULL
    )
    BEGIN
        RAISERROR('Only workspace owner can delete workspace', 16, 1);
        RETURN;
    END;

    -- Soft delete
    UPDATE workspaces
    SET deleted_at = GETUTCDATE()
    WHERE workspace_id = @workspace_id;

    PRINT 'Workspace deleted successfully';
END;
GO

-- =============================================
-- 6. usp_add_workspace_member - Add member to workspace
-- =============================================
CREATE PROCEDURE usp_add_workspace_member
    @workspace_id INT,
    @user_id INT,
    @invited_by INT,
    @role NVARCHAR(20) = 'viewer'
AS
BEGIN
    SET NOCOUNT ON;

    -- Validate role
    IF @role NOT IN ('owner', 'editor', 'viewer')
    BEGIN
        RAISERROR('Invalid role', 16, 1);
        RETURN;
    END;

    -- Check inviter is owner or editor
    IF NOT EXISTS (
        SELECT 1 FROM workspace_members
        WHERE workspace_id = @workspace_id
        AND user_id = @invited_by
        AND role IN ('owner', 'editor')
        AND deleted_at IS NULL
    )
    BEGIN
        RAISERROR('Only owner or editor can invite members', 16, 1);
        RETURN;
    END;

    -- Add member
    INSERT INTO workspace_members (
        workspace_id, user_id, role, invited_by,
        invitation_status, joined_at
    )
    VALUES (
        @workspace_id, @user_id, @role, @invited_by,
        'active', GETUTCDATE()
    );

    PRINT 'Member added successfully';
END;
GO
