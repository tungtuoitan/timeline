-- ============================================
-- FILE: tables/entities/notes.sql
-- PURPOSE: Markdown notes with version history and workspace integration
-- DEPENDENCIES: users.sql, entity_types.sql
-- ============================================

PRINT '';
PRINT '📦 Creating table: notes';
PRINT '   Purpose: Markdown notes with version history';
PRINT '   Scale: 2M+ notes (high-traffic)';
PRINT '   Features: Versioning, sharing, workspace integration';
PRINT '';

-- ============================================
-- ⚠️  IMPORTANT: Migration Warning
-- ============================================
-- If migrating from existing Notes table:
-- 1. This schema is for SuperApp, not legacy SuperCollect
-- 2. Old Notes table used 'noteID' (camelCase)
-- 3. New notes table uses 'id' (standard naming)
-- 4. Migration script required if data exists
-- ============================================

-- ============================================
-- TABLE: notes
-- PURPOSE: Markdown notes with version history and sharing
-- SCALE: 2M+ notes (high-traffic)
-- ============================================

CREATE TABLE notes (
    id INT IDENTITY(1,1) PRIMARY KEY,
    
    -- Ownership
    user_id INT NOT NULL,
    
    -- Content
    name NVARCHAR(200) NOT NULL,
    description NVARCHAR(1000),
    content NVARCHAR(MAX), -- Markdown content
    
    -- URL routing
    slug NVARCHAR(250), -- Auto-generated from name (spaces→hyphens)
    
    -- Display customization
    color NVARCHAR(7), -- Hex color (#RRGGBB)
    icon NVARCHAR(50), -- Icon identifier
    
    -- Status flags
    is_archived BIT DEFAULT 0,
    is_pinned BIT DEFAULT 0,
    is_favorite BIT DEFAULT 0,
    
    -- Metadata
    word_count INT DEFAULT 0, -- Auto-calculated
    version_count INT DEFAULT 1, -- Incremented by trigger
    
    -- Timestamps
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    updated_at DATETIME2 DEFAULT GETUTCDATE(),
    deleted_at DATETIME2 NULL,
    
    -- Constraints
    CONSTRAINT fk_notes_user FOREIGN KEY (user_id) 
        REFERENCES users(id),
    
    CONSTRAINT ck_notes_color_format CHECK (
        color IS NULL 
        OR (color LIKE '#[0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f]')
    ),
    
    CONSTRAINT ck_notes_word_count CHECK (
        word_count >= 0
    ),
    
    CONSTRAINT ck_notes_version_count CHECK (
        version_count >= 1
    )
);

-- Indexes for notes

-- Primary lookup: User's notes
CREATE INDEX ix_notes_user ON notes(user_id, deleted_at)
INCLUDE (name, is_archived, is_pinned, created_at, updated_at)
WHERE deleted_at IS NULL;

-- Search by name
CREATE INDEX ix_notes_name ON notes(user_id, name, deleted_at)
WHERE deleted_at IS NULL;

-- Search by slug (URL routing)
CREATE INDEX ix_notes_slug ON notes(user_id, slug, deleted_at)
WHERE deleted_at IS NULL AND slug IS NOT NULL;

-- Filter by status
CREATE INDEX ix_notes_archived ON notes(user_id, is_archived, deleted_at)
WHERE deleted_at IS NULL;

CREATE INDEX ix_notes_pinned ON notes(user_id, is_pinned, deleted_at)
WHERE is_pinned = 1 AND deleted_at IS NULL;

CREATE INDEX ix_notes_favorite ON notes(user_id, is_favorite, deleted_at)
WHERE is_favorite = 1 AND deleted_at IS NULL;

-- Comments
EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description',
    @value = N'Markdown notes. Each note belongs to a user and can appear in multiple workspaces.',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE', @level1name = N'notes';

PRINT '   ✅ notes table created';
PRINT '   ✅ 6 indexes created (user, name, slug, archived, pinned, favorite)';
GO

-- ============================================
-- TRIGGERS: Auto-maintenance
-- ============================================

PRINT '';
PRINT '🔧 Creating triggers for notes...';

-- Trigger: Auto-generate slug
CREATE OR ALTER TRIGGER tr_notes_generate_slug
ON notes
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE n
    SET slug = LOWER(
        REPLACE(
            REPLACE(
                REPLACE(
                    REPLACE(i.name, ' ', '-'),
                    '/', '-'),
                '_', '-'),
            '--', '-')
    )
    FROM notes n
    INNER JOIN inserted i ON n.id = i.id
    WHERE n.slug IS NULL OR n.slug = '';
END;
GO

PRINT '   ✅ tr_notes_generate_slug created';

-- Trigger: Update updated_at
CREATE OR ALTER TRIGGER tr_notes_updated_at
ON notes
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE notes
    SET updated_at = GETUTCDATE()
    WHERE id IN (SELECT id FROM inserted);
END;
GO

PRINT '   ✅ tr_notes_updated_at created';

-- Trigger: Auto-add note owner as member
CREATE OR ALTER TRIGGER tr_notes_add_owner
ON notes
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO note_members (
        note_id, user_id, role, invited_by, 
        invitation_status, joined_at
    )
    SELECT 
        i.id,
        i.user_id,
        'owner',
        i.user_id,
        'active',
        GETUTCDATE()
    FROM inserted i;
END;
GO

PRINT '   ✅ tr_notes_add_owner created';

-- Trigger: Auto-create version on note update
-- FIXED BUG 3: Race condition with concurrent updates
CREATE OR ALTER TRIGGER tr_notes_create_version
ON notes
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Only create version if content/name/description changed
    IF UPDATE(content) OR UPDATE(name) OR UPDATE(description)
    BEGIN
        -- FIXED: Use SERIALIZABLE lock + ROW_NUMBER to prevent duplicate version numbers
        -- This prevents race condition when 2 threads update same note simultaneously
        INSERT INTO note_versions (
            note_id, version_number, name, description, content,
            word_count, created_by, created_at
        )
        SELECT 
            i.id,
            -- Use TABLOCKX to prevent concurrent MAX queries
            ISNULL(
                (SELECT MAX(version_number) 
                 FROM note_versions WITH (TABLOCKX)
                 WHERE note_id = i.id), 
                0
            ) + ROW_NUMBER() OVER (PARTITION BY i.id ORDER BY (SELECT NULL)),
            i.name,
            i.description,
            i.content,
            i.word_count,
            i.user_id,
            GETUTCDATE()
        FROM inserted i;
        
        -- Update version_count
        UPDATE n
        SET version_count = (
            SELECT COUNT(*) 
            FROM note_versions 
            WHERE note_id = n.id
        )
        FROM notes n
        INNER JOIN inserted i ON n.id = i.id;
    END;
END;
GO

PRINT '   ✅ tr_notes_create_version created (race condition fixed)';

GO

-- ============================================
-- VIEWS: Simplified queries
-- ============================================

PRINT '';
PRINT '👁️  Creating views for notes...';

-- View: Note details with workspace info
CREATE OR ALTER VIEW vw_notes_with_workspaces AS
SELECT 
    n.id AS note_id,
    n.user_id,
    n.name AS note_name,
    n.description,
    n.is_archived,
    n.is_pinned,
    n.is_favorite,
    n.word_count,
    n.version_count,
    n.created_at,
    n.updated_at,
    wi.workspace_id,
    w.name AS workspace_name,
    wi.parent_tag_id,
    t.name AS parent_tag_name,
    wi.depth AS depth_in_workspace
FROM notes n
LEFT JOIN workspace_items wi 
    ON wi.child_type = 'note' 
    AND wi.child_id = n.id 
    AND wi.deleted_at IS NULL
LEFT JOIN workspaces w 
    ON wi.workspace_id = w.id 
    AND w.deleted_at IS NULL
LEFT JOIN tags t 
    ON wi.parent_tag_id = t.id 
    AND t.deleted_at IS NULL
WHERE n.deleted_at IS NULL;
GO

PRINT '   ✅ vw_notes_with_workspaces created';

-- View: User's accessible notes (owned + shared)
CREATE OR ALTER VIEW vw_user_notes AS
SELECT DISTINCT
    n.id AS note_id,
    n.name,
    n.description,
    n.is_archived,
    n.is_pinned,
    n.is_favorite,
    n.word_count,
    n.version_count,
    nm.user_id,
    nm.role,
    n.user_id AS owner_id,
    CASE WHEN n.user_id = nm.user_id THEN 1 ELSE 0 END AS is_owner,
    n.created_at,
    n.updated_at
FROM notes n
INNER JOIN note_members nm ON n.id = nm.note_id
WHERE n.deleted_at IS NULL
AND nm.deleted_at IS NULL
AND nm.invitation_status = 'active';
GO

PRINT '   ✅ vw_user_notes created';

GO

-- ============================================
-- VALIDATION
-- ============================================

PRINT '';
PRINT '📊 Verifying notes...';

SELECT 
    name AS object_name,
    type_desc,
    create_date
FROM sys.objects
WHERE name LIKE '%notes%'
AND type_desc IN ('USER_TABLE', 'SQL_TRIGGER', 'VIEW')
ORDER BY type_desc, name;

GO

-- ============================================
-- SUCCESS MESSAGE
-- ============================================

PRINT '';
PRINT '✅ notes table created successfully!';
PRINT '';
PRINT '📝 FEATURES:';
PRINT '   - Markdown content storage';
PRINT '   - Auto-generated slugs for URL routing';
PRINT '   - Version history (automatic on update)';
PRINT '   - Sharing with role-based permissions';
PRINT '   - Workspace integration';
PRINT '   - Status flags (archived, pinned, favorite)';
PRINT '';
PRINT '🔧 TRIGGERS:';
PRINT '   - tr_notes_generate_slug (URL-friendly names)';
PRINT '   - tr_notes_updated_at (timestamp maintenance)';
PRINT '   - tr_notes_add_owner (auto-create owner member)';
PRINT '   - tr_notes_create_version (version history with race condition fix)';
PRINT '';
PRINT '👁️  VIEWS:';
PRINT '   - vw_notes_with_workspaces (notes + workspace context)';
PRINT '   - vw_user_notes (accessible notes for a user)';
PRINT '';
PRINT '⚠️  MIGRATION WARNING:';
PRINT '   If upgrading from SuperCollect:';
PRINT '   - Old: Notes.noteID (camelCase)';
PRINT '   - New: notes.id (standard naming)';
PRINT '   - Migration script required';
PRINT '';
GO
