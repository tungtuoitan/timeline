-- ============================================
-- FILE: indexes/05.03-workspace-items-indexes.sql
-- PURPOSE: Indexes for workspace_items hierarchy queries
-- DEPENDENCIES: 02-tables-core.sql, 03-tables-workspace.sql, 07-tables-entities.sql
-- ============================================

PRINT '🔧 Creating workspace_items indexes for hierarchy queries...';
GO

-- Index: Get all items in workspace
CREATE INDEX ix_wsitems_workspace ON workspace_items(
    workspace_id, deleted_at
)
INCLUDE (
    id, parent_tag_id, child_type, child_id, 
    item_path, depth, relationship_type, label, sort_order
)
WHERE deleted_at IS NULL;

PRINT '   ✅ ix_wsitems_workspace created';

-- Index: Find children under a parent tag
CREATE INDEX ix_wsitems_parent ON workspace_items(
    parent_tag_id, workspace_id, deleted_at, sort_order
)
INCLUDE (child_type, child_id, item_path, depth)
WHERE deleted_at IS NULL;

PRINT '   ✅ ix_wsitems_parent created';

-- Index: Find all workspace_items for a specific child entity
CREATE INDEX ix_wsitems_child ON workspace_items(
    child_type, child_id, deleted_at
)
INCLUDE (workspace_id, parent_tag_id, item_path)
WHERE deleted_at IS NULL;

PRINT '   ✅ ix_wsitems_child created';

-- Index: Hierarchy path queries (find descendants)
CREATE INDEX ix_wsitems_path ON workspace_items(
    workspace_id, item_path, deleted_at
)
INCLUDE (id, child_type, child_id, depth)
WHERE deleted_at IS NULL;

PRINT '   ✅ ix_wsitems_path created';

-- Index: Filter by depth (get specific level)
CREATE INDEX ix_wsitems_depth ON workspace_items(
    workspace_id, depth, deleted_at
)
INCLUDE (parent_tag_id, child_type, child_id, sort_order)
WHERE deleted_at IS NULL;

PRINT '   ✅ ix_wsitems_depth created';

-- Index: Filter by relationship type
CREATE INDEX ix_wsitems_rel_type ON workspace_items(
    workspace_id, relationship_type, deleted_at
)
INCLUDE (parent_tag_id, child_type, child_id)
WHERE deleted_at IS NULL;

PRINT '   ✅ ix_wsitems_rel_type created';

GO

PRINT '✅ Workspace items indexes created successfully!';
PRINT '📊 Next step: Run indexes/05.04-notes-indexes.sql';
GO