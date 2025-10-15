-- =============================================
-- TABLE: workspaces
-- Description: User workspaces for organizing hierarchical content
-- Status: ✅ DEPLOYED (MVP)
-- =============================================

CREATE TABLE workspaces (
    -- Primary Key
    workspace_id INT IDENTITY(1,1) PRIMARY KEY,

    -- Owner
    user_id INT NOT NULL,

    -- Workspace info
    name NVARCHAR(200) NOT NULL,
    description NVARCHAR(1000),

    -- Visual properties
    color NVARCHAR(7), -- Hex color: #FF5733
    icon NVARCHAR(50), -- Icon name/emoji

    -- Workspace type & behavior
    type NVARCHAR(20) NOT NULL DEFAULT 'hierarchy',
    -- 'hierarchy': Tree structure (no cycles, single parent)
    -- 'graph': Free-form network (cycles allowed)
    -- 'network': Business relationships (typed connections)
    -- 'timeline': Temporal relationships (before/after)
    -- 'custom': User-defined relationship types

    -- Settings
    max_depth INT DEFAULT 10, -- Maximum tree depth

    -- Flags
    is_default BIT DEFAULT 0,
    is_public BIT DEFAULT 0,
    is_template BIT DEFAULT 0,
    is_archived BIT DEFAULT 0,

    -- Statistics (updated by triggers)
    tag_count INT DEFAULT 0,
    relationship_count INT DEFAULT 0,
    member_count INT DEFAULT 1,

    -- Metadata
    settings NVARCHAR(MAX), -- JSON: custom settings

    -- Timestamps
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    updated_at DATETIME2 DEFAULT GETUTCDATE(),
    last_accessed_at DATETIME2 NULL,
    deleted_at DATETIME2 NULL,

    -- Foreign Keys
    CONSTRAINT FK_workspaces_user FOREIGN KEY (user_id)
        REFERENCES users(user_id) ON DELETE CASCADE,

    -- Constraints
    CONSTRAINT CK_workspaces_type CHECK (
        type IN ('hierarchy', 'graph', 'network', 'timeline', 'custom')
    ),
    CONSTRAINT CK_workspaces_max_depth CHECK (max_depth > 0 AND max_depth <= 50),
    CONSTRAINT UQ_workspaces_user_name UNIQUE (user_id, name)
);

-- =============================================
-- INDEXES
-- =============================================

CREATE INDEX IX_workspaces_user ON workspaces(user_id, deleted_at)
    INCLUDE (name, type, color, icon, is_archived)
    WHERE deleted_at IS NULL;

CREATE INDEX IX_workspaces_type ON workspaces(user_id, type, deleted_at)
    WHERE deleted_at IS NULL;

CREATE INDEX IX_workspaces_recent ON workspaces(user_id, last_accessed_at DESC, deleted_at)
    WHERE deleted_at IS NULL;

-- =============================================
-- NOTES
-- =============================================
-- - Trigger: TR_workspaces_add_owner (auto-adds owner to workspace_members)
-- - Trigger: TR_workspaces_init_relationship_types (creates default relationship types)
-- - Trigger: TR_workspace_items_update_stats (updates tag_count, relationship_count)
