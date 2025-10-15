-- ============================================
-- FILE: indexes/05.05-filtered-indexes.sql
-- PURPOSE: Filtered indexes for specific scenarios
-- DEPENDENCIES: 02-tables-core.sql, 03-tables-workspace.sql, 07-tables-entities.sql
-- ============================================

PRINT '🔧 Creating filtered indexes for specific scenarios...';
GO

-- Filtered index: Only unused tags (for cleanup)
CREATE INDEX ix_tags_unused ON tags(user_id, usage_count, updated_at, deleted_at)
INCLUDE (id, name)
WHERE usage_count = 0 AND deleted_at IS NULL;

PRINT '   ✅ ix_tags_unused created';

-- Filtered index: Only archived workspaces
CREATE INDEX ix_workspace_archived ON workspaces(user_id, is_archived, deleted_at)
INCLUDE (id, name, created_at)
WHERE is_archived = 1 AND deleted_at IS NULL;

PRINT '   ✅ ix_workspace_archived created';

-- Filtered index: Only public workspaces (for discovery)
CREATE INDEX ix_workspace_public ON workspaces(is_public, type, deleted_at)
INCLUDE (id, user_id, name, description, created_at)
WHERE is_public = 1 AND deleted_at IS NULL;

PRINT '   ✅ ix_workspace_public created';

-- Filtered index: Only workspace templates
CREATE INDEX ix_workspace_templates ON workspaces(
    is_template, type, is_public, deleted_at
)
INCLUDE (id, user_id, name, description, tag_count)
WHERE is_template = 1 AND deleted_at IS NULL;

PRINT '   ✅ ix_workspace_templates created';

-- Filtered index: Only root items (depth = 0)
CREATE INDEX ix_wsitems_depth_zero ON workspace_items(
    workspace_id, depth, deleted_at, sort_order
)
INCLUDE (child_type, child_id)
WHERE depth = 0 AND deleted_at IS NULL;

PRINT '   ✅ ix_wsitems_depth_zero created';

-- Filtered index: Tag children only
CREATE INDEX ix_wsitems_tags_only ON workspace_items(
    workspace_id, child_type, deleted_at
)
INCLUDE (parent_tag_id, child_id, sort_order)
WHERE child_type = 'tag' AND deleted_at IS NULL;

PRINT '   ✅ ix_wsitems_tags_only created';

-- Filtered index: Note children only
CREATE INDEX ix_wsitems_notes_only ON workspace_items(
    workspace_id, child_type, deleted_at
)
INCLUDE (parent_tag_id, child_id)
WHERE child_type = 'note' AND deleted_at IS NULL;

PRINT '   ✅ ix_wsitems_notes_only created';

-- Filtered index: Archived notes
CREATE INDEX ix_notes_archived ON notes(user_id, is_archived, deleted_at)
INCLUDE (id, name, updated_at)
WHERE is_archived = 1 AND deleted_at IS NULL;

PRINT '   ✅ ix_notes_archived created';

-- Filtered index: Pinned notes
CREATE INDEX ix_notes_pinned ON notes(user_id, is_pinned, deleted_at)
INCLUDE (id, name, updated_at)
WHERE is_pinned = 1 AND deleted_at IS NULL;

PRINT '   ✅ ix_notes_pinned created';

-- Filtered index: Favorite notes
CREATE INDEX ix_notes_favorite ON notes(user_id, is_favorite, deleted_at)
INCLUDE (id, name, updated_at)
WHERE is_favorite = 1 AND deleted_at IS NULL;

PRINT '   ✅ ix_notes_favorite created';

GO

PRINT '✅ Filtered indexes created successfully!';
PRINT '📊 Next step: Run indexes/05.06-fulltext-search.sql';
GO