
-- ============================================
-- FILE: relationships/04.01-table.sql
-- PURPOSE: ⚠️ DEPRECATED - Workspace tag relationships table definition
-- DEPENDENCIES: None (do not use)
-- STATUS: Kept for reference only, replaced by workspace_items in 07-tables-entities.sql
-- ============================================

-- ⚠️⚠️⚠️ WARNING ⚠️⚠️⚠️
-- This file is DEPRECATED and should NOT be executed.
-- Replaced by: workspace_items (07-tables-entities.sql)
-- Migration: See 12-migration-guide.md

CREATE TABLE workspace_tag_relationships (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    
    -- Context
    workspace_id INT NOT NULL,
    
    -- Relationship endpoints
    from_tag_id INT NOT NULL, -- Parent/source tag
    to_tag_id INT NOT NULL, -- Child/target tag
    
    -- Relationship metadata
    relationship_type NVARCHAR(50) DEFAULT 'parent_child',
    -- Type must exist in workspace_relationship_types
    -- Examples: 'parent_child', 'connected_to', 'reports_to', 'depends_on'
    
    -- Materialized path for performance (Option D strategy)
    from_path NVARCHAR(4000), -- Ancestor chain: '1.5.10' (root to from_tag)
    to_path NVARCHAR(4000), -- Full path: '1.5.10.15' (root to to_tag)
    depth INT DEFAULT 0, -- Distance from root (0 = root)
    
    -- Visual/UI properties
    sort_order INT DEFAULT 0, -- Child order under parent
    label NVARCHAR(200), -- Edge label for diagrams
    color NVARCHAR(7), -- Override edge color
    line_style NVARCHAR(20), -- 'solid', 'dashed', 'dotted'
    
    -- Relationship properties
    is_bidirectional BIT DEFAULT 0, -- If true, reverse edge exists
    strength DECIMAL(3,2) DEFAULT 1.0, -- Weight/importance (0.0-1.0)
    
    -- Metadata (flexible storage for future features)
    metadata NVARCHAR(MAX), -- JSON: { notes, tags, custom_fields, ... }
    
    -- Audit
    created_by INT NOT NULL, -- User who created this relationship
    updated_by INT NULL, -- User who last updated
    
    -- Timestamps
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    updated_at DATETIME2 DEFAULT GETUTCDATE(),
    deleted_at DATETIME2 NULL, -- Soft delete
    
    -- Constraints
    CONSTRAINT fk_wsrel_workspace FOREIGN KEY (workspace_id) 
        REFERENCES workspaces(id) ON DELETE CASCADE,
    
    CONSTRAINT fk_wsrel_from_tag FOREIGN KEY (from_tag_id) 
        REFERENCES tags(id) ON DELETE CASCADE,
    
    CONSTRAINT fk_wsrel_to_tag FOREIGN KEY (to_tag_id) 
        REFERENCES tags(id),
        -- Note: No CASCADE on to_tag to prevent accidental deletions
        -- We want to validate tag deletion separately
    
    CONSTRAINT fk_wsrel_created_by FOREIGN KEY (created_by) 
        REFERENCES users(id),
    
    CONSTRAINT fk_wsrel_updated_by FOREIGN KEY (updated_by) 
        REFERENCES users(id),
    
    CONSTRAINT ck_wsrel_line_style CHECK (
        line_style IS NULL OR line_style IN ('solid', 'dashed', 'dotted')
    ),
    
    CONSTRAINT ck_wsrel_strength CHECK (
        strength >= 0.0 AND strength <= 1.0
    ),
    
    CONSTRAINT ck_wsrel_depth CHECK (
        depth >= 0 AND depth <= 50
    ),
    
    -- Prevent self-loops (tag pointing to itself)
    CONSTRAINT ck_wsrel_no_self_loop CHECK (
        from_tag_id != to_tag_id
    ),
    
    -- Unique: Can't have duplicate relationships
    -- Same workspace, same tags, same type (unless one is deleted)
    CONSTRAINT uq_wsrel_unique UNIQUE (
        workspace_id, from_tag_id, to_tag_id, relationship_type, deleted_at
    )
);

-- Comments
EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description',
    @value = N'Context-specific tag relationships. Uses materialized paths for performance (Option D). Supports multiple relationship types per workspace.',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE', @level1name = N'workspace_tag_relationships';

EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description',
    @value = N'Materialized path from root to from_tag. Example: "1.5.10" means tag chain: 1 → 5 → 10',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE', @level1name = N'workspace_tag_relationships',
    @level2type = N'COLUMN', @level2name = N'from_path';

EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description',
    @value = N'Materialized path from root to to_tag. Example: "1.5.10.15" means full chain: 1 → 5 → 10 → 15',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE', @level1name = N'workspace_tag_relationships',
    @level2type = N'COLUMN', @level2name = N'to_path';

EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description',
    @value = N'Distance from root. Root tags have depth=0. Count of dots in to_path indicates depth.',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE', @level1name = N'workspace_tag_relationships',
    @level2type = N'COLUMN', @level2name = N'depth';

GO

PRINT '⚠️ DEPRECATED: workspace_tag_relationships table created for reference only!';
PRINT '   Replaced by: workspace_items in 07-tables-entities.sql';
PRINT '   Migration: See 12-migration-guide.md';
PRINT '📊 Next step: Run relationships/04.02-indexes.sql (for reference only)';
GO