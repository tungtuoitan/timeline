-- =============================================
-- TABLE: workspace_members
-- Description: Workspace collaboration and access control
-- Status: ✅ DEPLOYED (MVP)
-- =============================================

CREATE TABLE workspace_members (
    -- Primary Key
    member_id BIGINT IDENTITY(1,1) PRIMARY KEY,

    -- Relationship
    workspace_id INT NOT NULL,
    user_id INT NOT NULL,

    -- Permission level
    role NVARCHAR(20) NOT NULL DEFAULT 'viewer',
    -- 'owner': Full control
    -- 'editor': Edit content
    -- 'viewer': Read-only

    -- Invitation tracking
    invited_by INT NULL,
    invitation_status NVARCHAR(20) DEFAULT 'active',
    -- 'pending', 'active', 'declined', 'removed'

    -- Metadata
    custom_permissions NVARCHAR(MAX), -- JSON

    -- Timestamps
    invited_at DATETIME2 DEFAULT GETUTCDATE(),
    joined_at DATETIME2 NULL,
    last_accessed_at DATETIME2 NULL,
    deleted_at DATETIME2 NULL,

    -- Foreign Keys
    CONSTRAINT FK_workspace_members_workspace FOREIGN KEY (workspace_id)
        REFERENCES workspaces(workspace_id) ON DELETE CASCADE,
    CONSTRAINT FK_workspace_members_user FOREIGN KEY (user_id)
        REFERENCES users(user_id) ON DELETE NO ACTION,
    CONSTRAINT FK_workspace_members_inviter FOREIGN KEY (invited_by)
        REFERENCES users(user_id) ON DELETE NO ACTION,

    -- Constraints
    CONSTRAINT CK_workspace_members_role CHECK (role IN ('owner', 'editor', 'viewer')),
    CONSTRAINT CK_workspace_members_status CHECK (
        invitation_status IN ('pending', 'active', 'declined', 'removed')
    ),
    CONSTRAINT UQ_workspace_members_workspace_user UNIQUE (workspace_id, user_id)
);

-- =============================================
-- INDEXES
-- =============================================

CREATE INDEX IX_workspace_members_workspace ON workspace_members(workspace_id, deleted_at)
    INCLUDE (user_id, role, invitation_status)
    WHERE deleted_at IS NULL;

CREATE INDEX IX_workspace_members_user ON workspace_members(user_id, deleted_at)
    INCLUDE (workspace_id, role, invitation_status)
    WHERE deleted_at IS NULL;

-- =============================================
-- NOTES
-- =============================================
-- - Trigger: TR_workspace_members_update_count (updates workspaces.member_count)
-- - Owner is auto-added by TR_workspaces_add_owner trigger
