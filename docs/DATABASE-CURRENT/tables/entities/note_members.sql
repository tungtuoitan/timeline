-- =============================================
-- TABLE: note_members
-- Description: Note collaboration and access control
-- Status: ✅ DEPLOYED (MVP)
-- =============================================

CREATE TABLE note_members (
    -- Primary Key
    member_id BIGINT IDENTITY(1,1) PRIMARY KEY,

    -- Note reference
    note_id INT NOT NULL,
    user_id INT NOT NULL,

    -- Permission level
    role NVARCHAR(20) NOT NULL DEFAULT 'viewer',
    -- 'owner': Full control (delete note, manage members)
    -- 'editor': Edit content, create versions
    -- 'viewer': Read-only access

    -- Invitation tracking
    invited_by INT NULL,
    invitation_status NVARCHAR(20) DEFAULT 'active',
    -- 'pending': Invitation sent
    -- 'active': Member is active
    -- 'declined': Invitation declined
    -- 'removed': Member was removed

    -- Timestamps
    invited_at DATETIME2 DEFAULT GETUTCDATE(),
    joined_at DATETIME2 NULL,
    last_accessed_at DATETIME2 NULL,
    deleted_at DATETIME2 NULL,

    -- Foreign Keys
    CONSTRAINT FK_note_members_note FOREIGN KEY (note_id)
        REFERENCES notes(note_id) ON DELETE CASCADE,
    CONSTRAINT FK_note_members_user FOREIGN KEY (user_id)
        REFERENCES users(user_id) ON DELETE NO ACTION,
    CONSTRAINT FK_note_members_inviter FOREIGN KEY (invited_by)
        REFERENCES users(user_id),

    -- Constraints
    CONSTRAINT CK_note_members_role CHECK (role IN ('owner', 'editor', 'viewer')),
    CONSTRAINT CK_note_members_status CHECK (
        invitation_status IN ('pending', 'active', 'declined', 'removed')
    ),
    CONSTRAINT UQ_note_members_note_user UNIQUE (note_id, user_id)
);

-- =============================================
-- INDEXES
-- =============================================

CREATE INDEX IX_note_members_note ON note_members(note_id, deleted_at)
    INCLUDE (user_id, role, invitation_status)
    WHERE deleted_at IS NULL;

CREATE INDEX IX_note_members_user ON note_members(user_id, deleted_at)
    INCLUDE (note_id, role, invitation_status)
    WHERE deleted_at IS NULL;

CREATE INDEX IX_note_members_owner ON note_members(note_id, role, deleted_at)
    WHERE role = 'owner' AND deleted_at IS NULL;

-- =============================================
-- NOTES
-- =============================================
-- - Owner is auto-added by tr_notes_add_owner trigger
-- - Each user can only be member once per note (UNIQUE constraint)
