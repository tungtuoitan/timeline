-- ============================================
-- FILE: indexes/05.04-notes-indexes.sql
-- PURPOSE: Indexes for notes system
-- DEPENDENCIES: 02-tables-core.sql, 07-tables-entities.sql
-- ============================================

PRINT '🔧 Creating notes system indexes...';
GO

-- Index: User's notes lookup
CREATE INDEX ix_notes_user ON notes(user_id, deleted_at)
INCLUDE (
    id, name, type, is_archived, is_pinned, is_favorite,
    word_count, version_count, created_at, updated_at
)
WHERE deleted_at IS NULL;

PRINT '   ✅ ix_notes_user created';

-- Index: Note slug lookup (URL routing)
CREATE INDEX ix_notes_slug ON notes(slug, user_id, deleted_at)
INCLUDE (id, name)
WHERE deleted_at IS NULL AND slug IS NOT NULL;

PRINT '   ✅ ix_notes_slug created';

-- Index: Search notes by name/description
CREATE INDEX ix_notes_search ON notes(user_id, deleted_at)
INCLUDE (id, name, description, type, updated_at)
WHERE deleted_at IS NULL;

PRINT '   ✅ ix_notes_search created';

-- Index: Note members permission check
CREATE INDEX ix_note_members_permission ON note_members(
    note_id, user_id, role, invitation_status, deleted_at
)
WHERE deleted_at IS NULL AND invitation_status = 'active';

PRINT '   ✅ ix_note_members_permission created';

-- Index: User's accessible notes
CREATE INDEX ix_note_members_user ON note_members(
    user_id, invitation_status, deleted_at, last_accessed_at DESC
)
INCLUDE (note_id, role)
WHERE deleted_at IS NULL;

PRINT '   ✅ ix_note_members_user created';

-- Index: Note versions lookup
CREATE INDEX ix_note_versions_note ON note_versions(
    note_id, version_number DESC
)
INCLUDE (name, created_by, created_at, word_count);

PRINT '   ✅ ix_note_versions_note created';

GO

PRINT '✅ Notes system indexes created successfully!';
PRINT '📊 Next step: Run indexes/05.05-filtered-indexes.sql';
GO