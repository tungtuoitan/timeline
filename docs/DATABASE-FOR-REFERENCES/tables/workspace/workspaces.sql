-- ============================================
-- FILE: workspaces.sql
-- PURPOSE: Workspace containers for tag contexts/organizations
-- CATEGORY: Workspace Tables
-- SCALE: 10-20 per user, 20,000+ total
-- DEPENDENCIES: core/users.sql
-- ============================================

PRINT '📦 Creating table: workspaces...';
GO

-- ============================================
-- TABLE: workspaces
-- ============================================

CREATE TABLE workspaces (
    id INT IDENTITY(1,1) PRIMARY KEY,
    
    -- Owner
    user_id INT NOT NULL,
    
    -- Workspace info
    name NVARCHAR(200) NOT NULL,
    description NVARCHAR(1000),
    
    -- Visual properties
    color NVARCHAR(7), -- Hex color: #FF5733
    icon NVARCHAR(50), -- Icon name/emoji: "briefcase", "💼"
    
    -- Workspace type & behavior
    type NVARCHAR(20) NOT NULL DEFAULT 'hierarchy',
    -- 'hierarchy': Tree structure (no cycles, single parent)
    -- 'graph': Free-form network (cycles allowed)
    -- 'network': Business relationships (typed connections)
    -- 'timeline': Temporal relationships (before/after)
    -- 'custom': User-defined relationship types
    
    -- Settings
    max_depth INT DEFAULT 10, -- Limit hierarchy depth for performance
    
    -- Flags
    is_default BIT DEFAULT 0, -- User's default workspace
    is_public BIT DEFAULT 0, -- Public workspace (future: discovery)
    is_template BIT DEFAULT 0, -- Template for cloning
    is_archived BIT DEFAULT 0, -- Archived workspace (read-only)
    
    -- Statistics (denormalized for performance)
    tag_count INT DEFAULT 0, -- Number of tags in workspace
    relationship_count INT DEFAULT 0, -- Number of relationships
    member_count INT DEFAULT 1, -- Number of members (including owner)
    
    -- Metadata
    settings NVARCHAR(MAX), -- JSON: { view_mode, sort_order, filters, ... }
    
    -- Timestamps
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    updated_at DATETIME2 DEFAULT GETUTCDATE(),
    last_accessed_at DATETIME2 NULL, -- For sorting by recent use
    deleted_at DATETIME2 NULL, -- Soft delete
    
    -- Constraints
    CONSTRAINT fk_workspace_user FOREIGN KEY (user_id) 
        REFERENCES users(id) ON DELETE CASCADE,
    
    CONSTRAINT ck_workspace_type CHECK (
        type IN ('hierarchy', 'graph', 'network', 'timeline', 'custom')
    ),
    
    CONSTRAINT ck_workspace_max_depth CHECK (max_depth > 0 AND max_depth <= 50),
    
    -- Unique: user can't have duplicate workspace names (unless deleted)
    CONSTRAINT uq_workspace_user_name UNIQUE (user_id, name, deleted_at)
);
GO

-- ============================================
-- INDEXES
-- ============================================

-- Primary lookup: Get user's workspaces
CREATE INDEX ix_workspace_user ON workspaces(user_id, deleted_at) 
    INCLUDE (name, type, color, icon, is_archived)
    WHERE deleted_at IS NULL;

-- Filter by type
CREATE INDEX ix_workspace_type ON workspaces(user_id, type, deleted_at) 
    WHERE deleted_at IS NULL;

-- Sort by recent access
CREATE INDEX ix_workspace_recent ON workspaces(user_id, last_accessed_at DESC, deleted_at) 
    WHERE deleted_at IS NULL;

-- Find templates
CREATE INDEX ix_workspace_template ON workspaces(is_template, is_public, deleted_at) 
    WHERE is_template = 1 AND deleted_at IS NULL;

-- Find default workspace
CREATE INDEX ix_workspace_default ON workspaces(user_id, is_default, deleted_at) 
    WHERE is_default = 1 AND deleted_at IS NULL;

GO

-- ============================================
-- TRIGGERS
-- ============================================

-- Trigger: Auto-add owner as member when workspace created
CREATE OR ALTER TRIGGER tr_workspace_add_owner
ON workspaces
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Add workspace creator as owner
    INSERT INTO workspace_members (
        workspace_id, 
        user_id, 
        role, 
        invited_by,
        invitation_status,
        joined_at
    )
    SELECT 
        i.id,
        i.user_id,
        'owner',
        i.user_id,
        'active',
        GETUTCDATE()
    FROM inserted i;
    
    -- Update member_count
    UPDATE w
    SET member_count = 1
    FROM workspaces w
    INNER JOIN inserted i ON w.id = i.id;
END;
GO

-- ============================================
-- Trigger: Initialize default relationship types when workspace created
-- ============================================

USE SuperApp;
GO

-- Drop existing trigger
DROP TRIGGER IF EXISTS tr_workspace_init_relationship_types;
GO

-- Recreate with explicit column aliases
CREATE TRIGGER tr_workspace_init_relationship_types
ON workspaces
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Add default relationship types based on workspace type
    
    -- For 'hierarchy' workspaces
    INSERT INTO workspace_relationship_types (
        workspace_id, type_name, display_name, description,
        is_bidirectional, allows_cycles, sort_order
    )
    SELECT 
        i.id,
        'parent_child',
        'Parent-Child',
        'Hierarchical parent-child relationship',
        0, -- Not bidirectional
        0, -- No cycles
        1
    FROM inserted i
    WHERE i.type = 'hierarchy';
    
    -- For 'graph' workspaces
    INSERT INTO workspace_relationship_types (
        workspace_id, type_name, display_name, description,
        is_bidirectional, allows_cycles, sort_order
    )
    SELECT 
        i.id,
        types.type_name,
        types.display_name,
        types.description,  -- Now it's clear this comes from types alias
        types.is_bidirectional,
        types.allows_cycles,
        types.sort_order
    FROM inserted i
    CROSS APPLY (VALUES
        ('connected_to', 'Connected To', 'General connection', 1, 1, 1),
        ('related_to', 'Related To', 'Related concept', 1, 1, 2)
    ) AS types(type_name, display_name, description, is_bidirectional, allows_cycles, sort_order)
    WHERE i.type = 'graph';
    
    -- For 'network' workspaces
    INSERT INTO workspace_relationship_types (
        workspace_id, type_name, display_name, description,
        is_bidirectional, allows_cycles, sort_order
    )
    SELECT 
        i.id,
        types.type_name,
        types.display_name,
        types.description,  -- Explicitly use types.description
        types.is_bidirectional,
        types.allows_cycles,
        types.sort_order
    FROM inserted i
    CROSS APPLY (VALUES
        ('reports_to', 'Reports To', 'Organizational hierarchy', 0, 0, 1),
        ('collaborates_with', 'Collaborates With', 'Collaboration relationship', 1, 1, 2),
        ('depends_on', 'Depends On', 'Dependency relationship', 0, 1, 3)
    ) AS types(type_name, display_name, description, is_bidirectional, allows_cycles, sort_order)
    WHERE i.type = 'network';
    
    -- For 'timeline' workspaces
    INSERT INTO workspace_relationship_types (
        workspace_id, type_name, display_name, description,
        is_bidirectional, allows_cycles, sort_order
    )
    SELECT 
        i.id,
        types.type_name,
        types.display_name,
        types.description,  -- Explicitly use types.description
        types.is_bidirectional,
        types.allows_cycles,
        types.sort_order
    FROM inserted i
    CROSS APPLY (VALUES
        ('before', 'Before', 'Happens before', 0, 0, 1),
        ('after', 'After', 'Happens after', 0, 0, 2),
        ('concurrent', 'Concurrent', 'Happens at same time', 1, 0, 3)
    ) AS types(type_name, display_name, description, is_bidirectional, allows_cycles, sort_order)
    WHERE i.type = 'timeline';
    
    -- For 'custom' workspaces - just add a default type
    INSERT INTO workspace_relationship_types (
        workspace_id, type_name, display_name, description,
        is_bidirectional, allows_cycles, sort_order
    )
    SELECT 
        i.id,
        'linked_to',
        'Linked To',
        'Generic link',
        1,
        1,
        1
    FROM inserted i
    WHERE i.type = 'custom';
END;
GO

PRINT '✅ Trigger tr_workspace_init_relationship_types fixed!';
PRINT '   - Added explicit alias references (types.description)';
PRINT '   - Resolved ambiguous column name errors';
GO

-- Trigger: Update workspaces.updated_at on change
CREATE OR ALTER TRIGGER tr_workspace_updated_at
ON workspaces
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE workspaces
    SET updated_at = GETUTCDATE()
    WHERE id IN (SELECT id FROM inserted);
END;
GO

-- ============================================
-- VIEWS
-- ============================================

-- View: Active workspaces with owner info
CREATE OR ALTER VIEW vw_workspaces_with_owner AS
SELECT 
    w.id,
    w.user_id AS owner_id,
    u.username AS owner_username,
    u.display_name AS owner_display_name,
    w.name,
    w.description,
    w.type,
    w.color,
    w.icon,
    w.tag_count,
    w.relationship_count,
    w.member_count,
    w.is_default,
    w.is_public,
    w.is_template,
    w.is_archived,
    w.created_at,
    w.updated_at,
    w.last_accessed_at
FROM workspaces w
INNER JOIN users u ON w.user_id = u.id
WHERE w.deleted_at IS NULL
AND u.deleted_at IS NULL;
GO

-- ============================================
-- EXTENDED PROPERTIES (Comments)
-- ============================================

EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description',
    @value = N'Workspace containers. Each workspace provides a different context/organization for tags.',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE', @level1name = N'workspaces';

EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description',
    @value = N'Workspace type determines allowed relationship patterns. hierarchy=tree, graph=free-form, network=typed connections',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE', @level1name = N'workspaces',
    @level2type = N'COLUMN', @level2name = N'type';

EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description',
    @value = N'JSON format: {"view_mode": "tree", "sort_by": "name", "show_icons": true, ...}',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE', @level1name = N'workspaces',
    @level2type = N'COLUMN', @level2name = N'settings';

GO

PRINT '✅ Table created: workspaces';
PRINT '   - 5 indexes created';
PRINT '   - 3 triggers created';
PRINT '   - 1 view created';
GO
