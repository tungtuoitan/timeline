-- ============================================
-- FILE: tables/entities/workspace_items.sql
-- PURPOSE: UNIFIED SYSTEM - Manages both tag-to-tag and entity-to-tag relationships
-- DEPENDENCIES: workspaces.sql, tags.sql, entity_types.sql
-- ============================================

PRINT '';
PRINT '📦 Creating table: workspace_items';
PRINT '   Purpose: UNIFIED SYSTEM for workspace content';
PRINT '   Scale: 15M+ items (largest table)';
PRINT '   Architecture: Replaces workspace_tag_relationships + entity_tags';
PRINT '';

-- ============================================
-- TABLE: workspace_items
-- PURPOSE: UNIFIED system for managing workspace content
-- SCALE: 15M+ items (largest table, high-traffic)
-- ARCHITECTURE: Replaces 2 legacy tables (workspace_tag_relationships + entity_tags)
-- ============================================

CREATE TABLE workspace_items (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    
    -- Workspace context
    workspace_id INT NOT NULL,
    
    -- Parent (always a tag)
    parent_tag_id INT NOT NULL,
    
    -- Child (can be tag or any entity)
    child_type NVARCHAR(50) NOT NULL, -- 'tag', 'note', 'project', etc.
    child_id INT NOT NULL, -- References tags.id, notes.id, projects.id, etc.
    
    -- Relationship metadata
    relationship_type NVARCHAR(100), -- Custom relationship names (e.g., 'depends_on', 'implements')
    label NVARCHAR(500), -- Custom label for this specific relationship
    notes NVARCHAR(MAX), -- Relationship-specific notes
    
    -- Materialized path for tree queries
    item_path NVARCHAR(4000), -- '/parent_id/child_id/' for ancestry queries
    depth INT DEFAULT 0, -- Tree depth (0 = root item)
    
    -- Display properties
    sort_order INT DEFAULT 0, -- Custom ordering within parent
    color NVARCHAR(7), -- Override color (#RRGGBB)
    icon NVARCHAR(50), -- Override icon
    
    -- Audit fields
    added_by INT NOT NULL,
    
    -- Timestamps
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    updated_at DATETIME2 DEFAULT GETUTCDATE(),
    deleted_at DATETIME2 NULL,
    
    -- Constraints
    CONSTRAINT fk_wsitem_workspace FOREIGN KEY (workspace_id) 
        REFERENCES workspaces(id) ON DELETE CASCADE,
    
    CONSTRAINT fk_wsitem_parent_tag FOREIGN KEY (parent_tag_id) 
        REFERENCES tags(id),
    
    CONSTRAINT fk_wsitem_type FOREIGN KEY (child_type) 
        REFERENCES entity_types(type_name),
    
    CONSTRAINT fk_wsitem_user FOREIGN KEY (added_by) 
        REFERENCES users(id),
    
    CONSTRAINT ck_wsitem_color_format CHECK (
        color IS NULL 
        OR (color LIKE '#[0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f]')
    ),
    
    CONSTRAINT ck_wsitem_child_id_positive CHECK (
        child_id > 0
    ),
    
    CONSTRAINT ck_wsitem_depth CHECK (
        depth >= 0
    ),
    
    -- No duplicate items: Same child can't appear under same parent in same workspace
    CONSTRAINT uq_wsitem_unique UNIQUE (
        workspace_id, parent_tag_id, child_type, child_id, deleted_at
    )
);

-- Indexes for workspace_items

-- Primary lookup: Items in a workspace
CREATE INDEX ix_wsitem_workspace ON workspace_items(workspace_id, deleted_at)
INCLUDE (parent_tag_id, child_type, child_id, sort_order)
WHERE deleted_at IS NULL;

-- Parent-child lookup
CREATE INDEX ix_wsitem_parent ON workspace_items(parent_tag_id, deleted_at)
INCLUDE (workspace_id, child_type, child_id, sort_order)
WHERE deleted_at IS NULL;

-- Child lookup (find all parents of an entity)
CREATE INDEX ix_wsitem_child ON workspace_items(child_type, child_id, deleted_at)
INCLUDE (workspace_id, parent_tag_id)
WHERE deleted_at IS NULL;

-- Path-based queries (ancestry, descendants)
CREATE INDEX ix_wsitem_path ON workspace_items(workspace_id, item_path, deleted_at)
WHERE deleted_at IS NULL AND item_path IS NOT NULL;

-- Root items (top-level items in workspace)
CREATE INDEX ix_wsitem_roots ON workspace_items(workspace_id, depth, deleted_at)
WHERE depth = 0 AND deleted_at IS NULL;

-- Relationship type filtering
CREATE INDEX ix_wsitem_reltype ON workspace_items(workspace_id, relationship_type, deleted_at)
WHERE deleted_at IS NULL AND relationship_type IS NOT NULL;

-- Comments
EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description',
    @value = N'UNIFIED SYSTEM: Manages both tag-to-tag and entity-to-tag relationships. Replaces workspace_tag_relationships + entity_tags.',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE', @level1name = N'workspace_items';

PRINT '   ✅ workspace_items table created';
PRINT '   ✅ 6 indexes created (workspace, parent, child, path, roots, reltype)';
GO

-- ============================================
-- TRIGGER: Update workspace statistics
-- ============================================

PRINT '';
PRINT '🔧 Creating triggers for workspace_items...';

CREATE OR ALTER TRIGGER tr_wsitems_update_stats
ON workspace_items
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Update workspace stats
    UPDATE w
    SET 
        tag_count = (
            SELECT COUNT(DISTINCT 
                CASE WHEN child_type = 'tag' THEN child_id 
                     ELSE parent_tag_id 
                END
            )
            FROM workspace_items
            WHERE workspace_id = w.id
            AND deleted_at IS NULL
        ),
        relationship_count = (
            SELECT COUNT(*)
            FROM workspace_items
            WHERE workspace_id = w.id
            AND deleted_at IS NULL
        ),
        updated_at = GETUTCDATE()
    FROM workspaces w
    WHERE w.id IN (
        SELECT DISTINCT workspace_id FROM inserted
        UNION
        SELECT DISTINCT workspace_id FROM deleted
    );
    
    -- Update tags.usage_count
    UPDATE t
    SET usage_count = (
        SELECT COUNT(DISTINCT workspace_id)
        FROM workspace_items wi
        WHERE (
            (wi.child_type = 'tag' AND wi.child_id = t.id)
            OR wi.parent_tag_id = t.id
        )
        AND wi.deleted_at IS NULL
    )
    FROM tags t
    WHERE t.id IN (
        SELECT DISTINCT parent_tag_id FROM inserted
        UNION SELECT DISTINCT parent_tag_id FROM deleted
        UNION SELECT DISTINCT child_id FROM inserted WHERE child_type = 'tag'
        UNION SELECT DISTINCT child_id FROM deleted WHERE child_type = 'tag'
    );
END;
GO

PRINT '   ✅ tr_wsitems_update_stats created';

-- Trigger: Validate child entity exists
PRINT '   ⚠️  tr_wsitems_validate_child removed (validation moved to procedures)';
PRINT '      Reason: Performance - row-by-row cursor processing was killing bulk inserts';
PRINT '      Solution: Validation now in usp_add_item_to_workspace procedure';

GO

-- ============================================
-- VALIDATION
-- ============================================

PRINT '';
PRINT '📊 Verifying workspace_items...';

SELECT 
    name AS index_name,
    type_desc
FROM sys.indexes
WHERE object_id = OBJECT_ID('workspace_items')
AND name IS NOT NULL
ORDER BY name;

GO

-- ============================================
-- SUCCESS MESSAGE
-- ============================================

PRINT '';
PRINT '✅ workspace_items table created successfully!';
PRINT '';
PRINT '🎯 KEY FEATURES:';
PRINT '   - UNIFIED SYSTEM: One table for all workspace content';
PRINT '   - Replaces 2 legacy tables (workspace_tag_relationships + entity_tags)';
PRINT '   - Supports tag-to-tag AND entity-to-tag relationships';
PRINT '   - Materialized path for efficient tree queries';
PRINT '   - Custom relationship types and labels';
PRINT '   - 6 optimized indexes for common queries';
PRINT '';
PRINT '📈 SCALE:';
PRINT '   - Expected: 15M+ items';
PRINT '   - High-traffic table';
PRINT '   - Optimized for bulk operations';
PRINT '';
PRINT '⚠️  IMPORTANT:';
PRINT '   - Child entity validation is in stored procedures, not triggers';
PRINT '   - Use usp_add_item_to_workspace for safe insertions';
PRINT '   - Direct INSERT may create orphaned references';
PRINT '';
GO
