-- ============================================
-- FILE: constraints/06.12-business-logic-triggers.sql
-- PURPOSE: Business logic triggers for data consistency
-- DEPENDENCIES: 02-tables-core.sql, 03-tables-workspace.sql
-- ============================================

PRINT '🔒 Creating business logic triggers...';
GO

-- Trigger: Ensure only one default workspace per user
CREATE OR ALTER TRIGGER tr_workspaces_one_default
ON workspaces
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @user_id INT, @workspace_id INT;
    
    -- Get the affected user and workspace
    SELECT @user_id = user_id, @workspace_id = id
    FROM inserted
    WHERE is_default = 1;
    
    IF @user_id IS NOT NULL
    BEGIN
        -- Set other workspaces to non-default for the same user
        UPDATE workspaces
        SET is_default = 0,
            updated_at = GETUTCDATE()
        WHERE user_id = @user_id
        AND id != @workspace_id
        AND is_default = 1
        AND deleted_at IS NULL;
    END
END;
GO

PRINT '   ✅ tr_workspaces_one_default created';

-- Trigger: Ensure workspace has at least one owner
CREATE OR ALTER TRIGGER tr_wsmember_ensure_owner
ON workspace_members
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Check for deleted or updated records
    DECLARE @workspace_id INT;
    
    -- Get affected workspace
    SELECT @workspace_id = workspace_id
    FROM deleted
    UNION
    SELECT workspace_id
    FROM inserted;
    
    IF @workspace_id IS NOT NULL
    BEGIN
        IF NOT EXISTS (
            SELECT 1
            FROM workspace_members
            WHERE workspace_id = @workspace_id
            AND role = 'owner'
            AND deleted_at IS NULL
            AND invitation_status = 'active'
        )
        BEGIN
            RAISERROR('Workspace must have at least one active owner', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END
    END
END;
GO

PRINT '   ✅ tr_wsmember_ensure_owner created';

-- Trigger: Ensure note has at least one owner
CREATE OR ALTER TRIGGER tr_note_members_ensure_owner
ON note_members
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Check for deleted or updated records
    DECLARE @note_id INT;
    
    -- Get affected note
    SELECT @note_id = note_id
    FROM deleted
    UNION
    SELECT note_id
    FROM inserted;
    
    IF @note_id IS NOT NULL
    BEGIN
        IF NOT EXISTS (
            SELECT 1
            FROM note_members
            WHERE note_id = @note_id
            AND role = 'owner'
            AND deleted_at IS NULL
            AND invitation_status = 'active'
        )
        BEGIN
            RAISERROR('Note must have at least one active owner', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END
    END
END;
GO

PRINT '   ✅ tr_note_members_ensure_owner created';

GO

PRINT '✅ Business logic triggers created successfully!';
PRINT '📊 Next step: Run constraints/06.13-audit-log.sql';
GO