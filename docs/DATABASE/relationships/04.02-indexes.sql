-- ============================================
-- FILE: relationships/04.02-indexes.sql
-- PURPOSE: ⚠️ DEPRECATED - Performance indexes for workspace_tag_relationships
-- DEPENDENCIES: relationships/04.01-table.sql (do not use)
-- STATUS: Kept for reference only, replaced by workspace_items in 07-tables-entities.sql
-- ============================================

-- ⚠️⚠️⚠️ WARNING ⚠️⚠️⚠️
-- This file is DEPRECATED and should NOT be executed.
-- Replaced by: workspace_items (07-tables-entities.sql)
-- Migration: See 12-migration-guide.md

-- Index 1: Get all relationships in workspace (most common query)
CREATE INDEX ix_wsrel_workspace ON workspace_tag_relationships(
    workspace_id, deleted_at
)
INCLUDE (from_tag_id, to_tag_id, relationship_type, to_path, depth)
WHERE deleted_at IS NULL;

-- Index 2: Get children of a tag (tree navigation)
CREATE INDEX ix_wsrel_from_tag ON workspace_tag_relationships(
    workspace_id, from_tag_id, relationship_type, sort_order, deleted_at
)
INCLUDE (to_tag_id, to_path, depth, label)
WHERE deleted_at IS NULL;

-- Index 3: Get parents/references to a tag (reverse lookup)
CREATE INDEX ix_wsrel_to_tag ON workspace_tag_relationships(
    workspace_id, to_tag_id, deleted_at
)
INCLUDE (from_tag_id, relationship_type, depth)
WHERE deleted_at IS NULL;

-- Index 4: Path-based queries (subtree queries using LIKE)
CREATE INDEX ix_wsrel_path ON workspace_tag_relationships(
    workspace_id, to_path, deleted_at
)
INCLUDE (from_tag_id, to_tag_id, depth, relationship_type)
WHERE deleted_at IS NULL;

-- Index 5: Filter by relationship type
CREATE INDEX ix_wsrel_type ON workspace_tag_relationships(
    workspace_id, relationship_type, deleted_at
)
INCLUDE (from_tag_id, to_tag_id, depth)
WHERE deleted_at IS NULL;

-- Index 6: Find root tags (tags with no parent in workspace)
CREATE INDEX ix_wsrel_roots ON workspace_tag_relationships(
    workspace_id, depth, deleted_at
)
INCLUDE (from_tag_id, to_tag_id)
WHERE depth = 0 AND deleted_at IS NULL;

-- Index 7: Audit queries (who created/modified)
CREATE INDEX ix_wsrel_audit ON workspace_tag_relationships(
    created_by, created_at, deleted_at
)
INCLUDE (workspace_id, from_tag_id, to_tag_id)
WHERE deleted_at IS NULL;

GO

PRINT '⚠️ DEPRECATED: Indexes for workspace_tag_relationships created for reference only!';
PRINT '   Replaced by: workspace_items in 07-tables-entities.sql';
PRINT '   Migration: See 12-migration-guide.md';
PRINT '📊 Next step: Run relationships/04.03-triggers.sql (for reference only)';
GO