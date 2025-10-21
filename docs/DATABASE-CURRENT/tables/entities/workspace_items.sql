-- =============================================
-- TABLE: workspace_items (UNIFIED SYSTEM)
-- Description: Manages ALL items in workspace (tags AND notes)
-- Status: ✅ DEPLOYED (MVP)
-- =============================================
-- DESIGN DECISION: This is a UNIFIED table that handles BOTH:
--   1. Tag-to-Tag relationships (child_type = 'tag')
--   2. Tag-to-Note relationships (child_type = 'note')
--
-- This differs from the original design (DATA-FOR-REFERENCES/INDEX.md)
-- which proposed separate tables. See IMPLEMENTATION-STATUS.md for details.
-- =============================================

CREATE TABLE workspace_items (
    -- Primary Key
    item_id BIGINT IDENTITY(1,1) PRIMARY KEY,

    -- Workspace context
    workspace_id INT NOT NULL,

    -- Parent (always a tag, NULL for root-level items)
    parent_tag_id INT NULL,

    -- Child (can be tag or any entity)
    child_type NVARCHAR(50) NOT NULL, -- 'tag', 'note', etc.
    child_id INT NOT NULL, -- References tag_id or note_id

    -- Relationship metadata
    relationship_type NVARCHAR(100),
    label NVARCHAR(500), -- Custom label for this relationship
    notes NVARCHAR(MAX), -- Additional notes about relationship

    -- Materialized path for tree queries
    item_path NVARCHAR(4000), -- e.g., '/1/5/12/'
    depth INT DEFAULT 0, -- 0 = root level

    -- Display properties
    sort_order INT DEFAULT 0,
    color NVARCHAR(7),
    icon NVARCHAR(50),

    -- Audit
    added_by INT NOT NULL,

    -- Timestamps
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    updated_at DATETIME2 DEFAULT GETUTCDATE(),
    deleted_at DATETIME2 NULL,

    -- Foreign Keys
    CONSTRAINT FK_workspace_items_workspace FOREIGN KEY (workspace_id)
        REFERENCES workspaces(workspace_id) ON DELETE CASCADE,
    CONSTRAINT FK_workspace_items_parent_tag FOREIGN KEY (parent_tag_id)
        REFERENCES tags(tag_id),
    CONSTRAINT FK_workspace_items_type FOREIGN KEY (child_type)
        REFERENCES entity_types(type_name),
    CONSTRAINT FK_workspace_items_user FOREIGN KEY (added_by)
        REFERENCES users(user_id),

    -- Constraints
    CONSTRAINT CK_workspace_items_color_format CHECK (
        color IS NULL OR (color LIKE '#[0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f]')
    ),
    CONSTRAINT CK_workspace_items_child_id_positive CHECK (child_id > 0),
    CONSTRAINT CK_workspace_items_depth CHECK (depth >= 0),
    -- Unique constraint handles NULL parent_tag_id properly
    CONSTRAINT UQ_workspace_items_unique UNIQUE (workspace_id, parent_tag_id, child_type, child_id)
);

-- =============================================
-- INDEXES
-- =============================================

CREATE INDEX IX_workspace_items_workspace ON workspace_items(workspace_id, deleted_at)
    INCLUDE (parent_tag_id, child_type, child_id, sort_order)
    WHERE deleted_at IS NULL;

CREATE INDEX IX_workspace_items_parent ON workspace_items(parent_tag_id, deleted_at)
    INCLUDE (workspace_id, child_type, child_id, sort_order)
    WHERE deleted_at IS NULL;

CREATE INDEX IX_workspace_items_child ON workspace_items(child_type, child_id, deleted_at)
    INCLUDE (workspace_id, parent_tag_id)
    WHERE deleted_at IS NULL;

CREATE INDEX IX_workspace_items_path ON workspace_items(workspace_id, item_path, deleted_at)
    WHERE deleted_at IS NULL AND item_path IS NOT NULL;

CREATE INDEX IX_workspace_items_roots ON workspace_items(workspace_id, depth, deleted_at)
    WHERE depth = 0 AND deleted_at IS NULL;

-- =============================================
-- NOTES
-- =============================================
-- UNIFIED DESIGN:
-- - Parent can be NULL for root-level items (depth = 0)
-- - Parent is a tag (parent_tag_id references tags.tag_id) for nested items
-- - Child can be ANY entity type (tag, note, etc.)
-- - Examples:
--   * Root Tag "Work": (parent_tag_id=NULL, child_type='tag', child_id=1, depth=0)
--   * Tag "Work" contains Tag "Projects": (parent_tag_id=1, child_type='tag', child_id=2, depth=1)
--   * Tag "Projects" contains Note "Q1 Report": (parent_tag_id=2, child_type='note', child_id=5, depth=2)
--
-- TRIGGERS:
-- - TR_workspace_items_update_stats: Updates workspaces.tag_count, relationship_count
-- - TR_workspace_items_update_depth: Updates depth based on hierarchy
--
-- PROCEDURES:
-- - usp_add_item_to_workspace: Add child to parent
-- - usp_s_workspace_items: List all items in workspace
-- - usp_move_item: Move item to different parent
-- - usp_remove_item: Soft delete item
-- - usp_s_item_path: Get hierarchical path
