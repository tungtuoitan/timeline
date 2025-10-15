-- =============================================
-- TABLE: workspace_relationship_types
-- Description: Custom relationship types for workspace-specific workflows
-- Status: ✅ DEPLOYED (MVP)
-- =============================================

CREATE TABLE workspace_relationship_types (
    -- Primary Key
    relationship_type_id INT IDENTITY(1,1) PRIMARY KEY,

    -- Workspace
    workspace_id INT NOT NULL,

    -- Type definition
    type_name NVARCHAR(50) NOT NULL,
    display_name NVARCHAR(100) NOT NULL,
    description NVARCHAR(500),

    -- Visual properties
    icon NVARCHAR(50),
    color NVARCHAR(7),
    line_style NVARCHAR(20) DEFAULT 'solid', -- 'solid', 'dashed', 'dotted'
    line_width INT DEFAULT 2,

    -- Relationship behavior
    is_bidirectional BIT DEFAULT 0,
    allows_cycles BIT DEFAULT 0,
    max_depth INT NULL,

    -- Validation rules
    validation_rules NVARCHAR(MAX), -- JSON

    -- Display order
    sort_order INT DEFAULT 0,

    -- Timestamps
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    updated_at DATETIME2 DEFAULT GETUTCDATE(),
    deleted_at DATETIME2 NULL,

    -- Foreign Keys
    CONSTRAINT FK_workspace_relationship_types_workspace FOREIGN KEY (workspace_id)
        REFERENCES workspaces(workspace_id) ON DELETE CASCADE,

    -- Constraints
    CONSTRAINT CK_workspace_relationship_types_line_style CHECK (
        line_style IN ('solid', 'dashed', 'dotted')
    ),
    CONSTRAINT UQ_workspace_relationship_types_name UNIQUE (workspace_id, type_name)
);

-- =============================================
-- INDEXES
-- =============================================

CREATE INDEX IX_workspace_relationship_types_workspace
    ON workspace_relationship_types(workspace_id, sort_order, deleted_at)
    WHERE deleted_at IS NULL;

-- =============================================
-- NOTES
-- =============================================
-- - Default types auto-created by TR_workspaces_init_relationship_types:
--   * 'hierarchy' workspace: 'parent_child'
--   * 'graph' workspace: 'connected_to', 'related_to'
