-- ============================================
-- FILE: indexes/05.01-composite-indexes.sql
-- PURPOSE: Composite indexes for common queries
-- DEPENDENCIES: 02-tables-core.sql, 03-tables-workspace.sql
-- ============================================

PRINT '🔧 Creating composite indexes for common queries...';
GO

-- Index: User login lookup (email + password check)
CREATE INDEX ix_users_login ON users(email, password_hash, is_active, deleted_at)
INCLUDE (id, username, display_name)
WHERE deleted_at IS NULL AND is_active = 1;

PRINT '   ✅ ix_users_login created';

-- Index: Tag search within user (autocomplete)
-- Optimized for: "SELECT * FROM tags WHERE user_id = X AND name LIKE 'React%'"
CREATE INDEX ix_tags_user_search ON tags(user_id, name, deleted_at)
INCLUDE (id, slug, color, icon, usage_count)
WHERE deleted_at IS NULL;

PRINT '   ✅ ix_tags_user_search created';

-- Index: Find tags by slug (URL routing)
CREATE INDEX ix_tags_slug_lookup ON tags(slug, user_id, deleted_at)
INCLUDE (id, name)
WHERE deleted_at IS NULL AND slug IS NOT NULL;

PRINT '   ✅ ix_tags_slug_lookup created';

-- Index: User's workspaces sorted by recent access
CREATE INDEX ix_workspace_user_recent ON workspaces(
    user_id, last_accessed_at DESC, deleted_at
)
INCLUDE (id, name, type, color, icon, tag_count, is_archived)
WHERE deleted_at IS NULL;

PRINT '   ✅ ix_workspace_user_recent created';

-- Index: Workspace members with role (permission checks)
CREATE INDEX ix_wsmember_permission_check ON workspace_members(
    workspace_id, user_id, role, invitation_status, deleted_at
)
WHERE deleted_at IS NULL AND invitation_status = 'active';

PRINT '   ✅ ix_wsmember_permission_check created';

-- Index: User's pending invitations
CREATE INDEX ix_wsmember_user_pending ON workspace_members(
    user_id, invitation_status, invited_at DESC, deleted_at
)
INCLUDE (workspace_id, role, invited_by)
WHERE deleted_at IS NULL AND invitation_status = 'pending';

PRINT '   ✅ ix_wsmember_user_pending created';

GO

PRINT '✅ Composite indexes created successfully!';
PRINT '📊 Next step: Run indexes/05.02-covering-indexes.sql';
GO