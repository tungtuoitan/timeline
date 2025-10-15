-- =============================================
-- TABLE: notes
-- Description: Markdown notes with version history
-- Status: ✅ DEPLOYED (MVP)
-- =============================================

CREATE TABLE notes (
    -- Primary Key
    note_id INT IDENTITY(1,1) PRIMARY KEY,

    -- Ownership
    user_id INT NOT NULL,

    -- Content
    name NVARCHAR(200) NOT NULL,
    description NVARCHAR(1000),
    content NVARCHAR(MAX), -- Markdown content

    -- URL routing
    slug NVARCHAR(250), -- Auto-generated from name by trigger

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

    -- Foreign Keys
    CONSTRAINT FK_notes_user FOREIGN KEY (user_id)
        REFERENCES users(user_id) ON DELETE CASCADE,

    -- Constraints
    CONSTRAINT CK_notes_color_format CHECK (
        color IS NULL OR (color LIKE '#[0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f]')
    ),
    CONSTRAINT CK_notes_word_count CHECK (word_count >= 0),
    CONSTRAINT CK_notes_version_count CHECK (version_count >= 1)
);

-- =============================================
-- INDEXES
-- =============================================

CREATE INDEX IX_notes_user ON notes(user_id, deleted_at)
    INCLUDE (name, is_archived, is_pinned, created_at, updated_at)
    WHERE deleted_at IS NULL;

CREATE INDEX IX_notes_name ON notes(user_id, name, deleted_at)
    WHERE deleted_at IS NULL;

CREATE INDEX IX_notes_slug ON notes(user_id, slug, deleted_at)
    WHERE deleted_at IS NULL AND slug IS NOT NULL;

CREATE INDEX IX_notes_archived ON notes(user_id, is_archived, deleted_at)
    WHERE deleted_at IS NULL;

CREATE INDEX IX_notes_pinned ON notes(user_id, is_pinned, deleted_at)
    WHERE is_pinned = 1 AND deleted_at IS NULL;

CREATE INDEX IX_notes_favorite ON notes(user_id, is_favorite, deleted_at)
    WHERE is_favorite = 1 AND deleted_at IS NULL;

-- =============================================
-- NOTES
-- =============================================
-- TRIGGERS:
-- - tr_notes_generate_slug: Auto-generates slug from name (with recursion check)
-- - tr_notes_updated_at: Updates updated_at timestamp (with recursion check)
-- - tr_notes_add_owner: Auto-adds creator as owner in note_members
-- - tr_notes_create_version: Creates version snapshot on content changes (with recursion check)
--
-- All triggers include TRIGGER_NESTLEVEL() checks to prevent recursion.
-- See FIX-NOTES-TRIGGERS.sql for details.
