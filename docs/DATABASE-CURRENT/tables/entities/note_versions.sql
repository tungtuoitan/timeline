-- =============================================
-- TABLE: note_versions
-- Description: Version history for notes
-- Status: ✅ DEPLOYED (MVP)
-- =============================================

CREATE TABLE note_versions (
    -- Primary Key
    version_id BIGINT IDENTITY(1,1) PRIMARY KEY,

    -- Note reference
    note_id INT NOT NULL,

    -- Version info
    version_number INT NOT NULL, -- 1, 2, 3, ...

    -- Content snapshot
    name NVARCHAR(200) NOT NULL,
    description NVARCHAR(MAX),
    content NVARCHAR(MAX), -- Full content snapshot

    -- Metadata snapshot
    word_count INT DEFAULT 0,

    -- Change tracking
    change_summary NVARCHAR(500), -- Brief description of changes
    created_by INT NOT NULL, -- Who created this version

    -- Timestamps
    created_at DATETIME2 DEFAULT GETUTCDATE(),

    -- Foreign Keys
    CONSTRAINT FK_note_versions_note FOREIGN KEY (note_id)
        REFERENCES notes(note_id) ON DELETE CASCADE,
    CONSTRAINT FK_note_versions_user FOREIGN KEY (created_by)
        REFERENCES users(user_id),

    -- Constraints
    CONSTRAINT CK_note_versions_version_positive CHECK (version_number > 0),
    CONSTRAINT UQ_note_versions_number UNIQUE (note_id, version_number)
);

-- =============================================
-- INDEXES
-- =============================================

CREATE INDEX IX_note_versions_note ON note_versions(note_id, version_number DESC)
    INCLUDE (name, created_at, created_by);

CREATE INDEX IX_note_versions_user ON note_versions(created_by, created_at DESC);

-- =============================================
-- NOTES
-- =============================================
-- - Versions auto-created by tr_notes_create_version trigger
-- - Each version is immutable (INSERT only, no UPDATE/DELETE)
-- - version_number is auto-incremented per note
-- - Full content snapshot stored (no deltas)
