-- ============================================
-- FILE: workspace_members.sql
-- PURPOSE: Workspace sharing with role-based permissions
-- CATEGORY: Workspace Tables
-- SCALE: ~5 members per workspace, 100,000+ total
-- DEPENDENCIES: workspace/workspaces.sql, core/users.sql
-- ============================================

PRINT '📦 Creating table: workspace_members...';
GO

-- ============================================
-- TABLE: workspace_members
-- ============================================

CREATE TABLE workspace_members (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    
    -- Relationship
    workspace_id INT NOT NULL,
    user_id INT NOT NULL,
    
    -- Permission level
    role NVARCHAR(20) NOT NULL DEFAULT 'viewer',
    -- 'owner': Full control (delete workspace, manage members)
    -- 'editor': Edit tags, relationships, content
    -- 'viewer': Read-only access
    
    -- Invitation tracking
    invited_by INT NULL, -- User who added this member
    invitation_status NVARCHAR(20) DEFAULT 'active',
    -- 'pending': Invitation sent, not accepted
    -- 'active': Member is active
    -- 'declined': Invitation declined
    -- 'removed': Member was removed
    
    -- Metadata
    custom_permissions NVARCHAR(MAX), -- JSON: Future custom permissions
    
    -- Timestamps
    invited_at DATETIME2 DEFAULT GETUTCDATE(),
    joined_at DATETIME2 NULL, -- When invitation accepted
    last_accessed_at DATETIME2 NULL, -- For activity tracking
    deleted_at DATETIME2 NULL, -- Soft delete (removed from workspace)
    
    -- Constraints
    CONSTRAINT fk_wsmember_workspace FOREIGN KEY (workspace_id) 
        REFERENCES workspaces(id) ON DELETE CASCADE,
    
    CONSTRAINT fk_wsmember_user FOREIGN KEY (user_id) 
        REFERENCES users(id) ON DELETE NO ACTION,
    
    CONSTRAINT fk_wsmember_inviter FOREIGN KEY (invited_by) 
        REFERENCES users(id) ON DELETE NO ACTION,
    
    CONSTRAINT ck_wsmember_role CHECK (
        role IN ('owner', 'editor', 'viewer')
    ),
    
    CONSTRAINT ck_wsmember_status CHECK (
        invitation_status IN ('pending', 'active', 'declined', 'removed')
    ),
    
    -- Unique: user can only be member once per workspace
    CONSTRAINT uq_wsmember_workspace_user UNIQUE (workspace_id, user_id, deleted_at)
);
GO

-- ============================================
-- INDEXES
-- ============================================

-- Get all members of a workspace
CREATE INDEX ix_wsmember_workspace ON workspace_members(workspace_id, deleted_at) 
    INCLUDE (user_id, role, invitation_status)
    WHERE deleted_at IS NULL;

-- Get all workspaces for a user
CREATE INDEX ix_wsmember_user ON workspace_members(user_id, deleted_at) 
    INCLUDE (workspace_id, role, invitation_status)
    WHERE deleted_at IS NULL;

-- Find owners (for permission checks)
CREATE INDEX ix_wsmember_owner ON workspace_members(workspace_id, role, deleted_at) 
    WHERE role = 'owner' AND deleted_at IS NULL;

-- Find pending invitations
CREATE INDEX ix_wsmember_pending ON workspace_members(user_id, invitation_status, deleted_at) 
    WHERE invitation_status = 'pending' AND deleted_at IS NULL;

GO

-- ============================================
-- TRIGGERS
-- ============================================

-- Trigger: Update workspace member_count when members change
CREATE OR ALTER TRIGGER tr_wsmember_update_count
ON workspace_members
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Update count for affected workspaces
    UPDATE w
    SET member_count = (
        SELECT COUNT(*)
        FROM workspace_members wm
        WHERE wm.workspace_id = w.id
        AND wm.deleted_at IS NULL
        AND wm.invitation_status = 'active'
    )
    FROM workspaces w
    WHERE w.id IN (
        SELECT DISTINCT workspace_id FROM inserted
        UNION
        SELECT DISTINCT workspace_id FROM deleted
    );
END;
GO

-- Trigger: Prevent deleting last owner (validation only)
CREATE OR ALTER TRIGGER tr_wsmember_prevent_delete_last_owner
ON workspace_members
AFTER DELETE
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Check if we just deleted the last owner of any workspace
    DECLARE @workspace_id INT;
    
    SELECT TOP 1 @workspace_id = workspace_id
    FROM deleted d
    WHERE d.role = 'owner'
    AND NOT EXISTS (
        SELECT 1 
        FROM workspace_members wm
        WHERE wm.workspace_id = d.workspace_id 
        AND wm.role = 'owner' 
        AND wm.deleted_at IS NULL
    );
    
    IF @workspace_id IS NOT NULL
    BEGIN
        RAISERROR('Cannot remove last owner from workspace. Transfer ownership first.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END;
END;
GO

-- ============================================
-- VIEWS
-- ============================================

-- View: User's accessible workspaces (owned + shared)
CREATE OR ALTER VIEW vw_user_workspaces AS
SELECT DISTINCT
    w.id AS workspace_id,
    w.name AS workspace_name,
    w.type AS workspace_type,
    w.color,
    w.icon,
    wm.user_id,
    wm.role,
    w.user_id AS owner_id,
    CASE WHEN w.user_id = wm.user_id THEN 1 ELSE 0 END AS is_owner,
    w.tag_count,
    w.member_count,
    w.is_archived,
    w.last_accessed_at,
    w.created_at
FROM workspaces w
INNER JOIN workspace_members wm ON w.id = wm.workspace_id
WHERE w.deleted_at IS NULL
AND wm.deleted_at IS NULL
AND wm.invitation_status = 'active';
GO

-- ============================================
-- EXTENDED PROPERTIES (Comments)
-- ============================================

EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description',
    @value = N'Workspace sharing and permissions. Supports 3 roles: owner, editor, viewer.',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE', @level1name = N'workspace_members';

EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description',
    @value = N'owner: full control, editor: edit content, viewer: read-only',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE', @level1name = N'workspace_members',
    @level2type = N'COLUMN', @level2name = N'role';

GO

PRINT '✅ Table created: workspace_members';
PRINT '   - 4 indexes created';
PRINT '   - 2 triggers created';
PRINT '   - 1 view created';
GO
