-- ============================================
-- FILE: indexes/05.02-covering-indexes.sql
-- PURPOSE: Covering indexes to avoid key lookups
-- DEPENDENCIES: 02-tables-core.sql, 03-tables-workspace.sql
-- ============================================

PRINT '🔧 Creating covering indexes for workspace access...';
GO

-- Covering index: Get tag with all display properties
CREATE INDEX ix_tags_display_all ON tags(id, user_id, deleted_at)
INCLUDE (name, slug, color, icon, description, usage_count, created_at, updated_at)
WHERE deleted_at IS NULL;

PRINT '   ✅ ix_tags_display_all created';

-- Covering index: Workspace list with all summary info
CREATE INDEX ix_workspace_summary ON workspaces(user_id, deleted_at)
INCLUDE (
    id, name, description, type, color, icon, 
    tag_count, relationship_count, member_count,
    is_default, is_archived, last_accessed_at, created_at
)
WHERE deleted_at IS NULL;

PRINT '   ✅ ix_workspace_summary created';

GO

PRINT '✅ Covering indexes created successfully!';
PRINT '📊 Next step: Run indexes/05.03-workspace-items-indexes.sql';
GO