-- ============================================
-- FILE: tables/entities/note_members.sql
-- PURPOSE: Note sharing with role-based permissions (3 roles)
-- DEPENDENCIES: notes.sql, users.sql
-- ============================================

PRINT '';
PRINT '📦 Creating table: note_members';
PRINT '   Purpose: Note sharing with role-based permissions';
PRINT '   Scale: ~5 members per shared note';
PRINT '   Roles: owner, editor, viewer';
PRINT '';

-- ============================================
-- TABLE: note_members
-- PURPOSE: Note sharing with role-based permissions
-- SCALE: ~5 members per shared note (~10M rows if 2M notes)
-- ============================================

CREATE TABLE note_members (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    
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
    
    -- Constraints
    CONSTRAINT fk_notemember_note FOREIGN KEY (note_id) 
        REFERENCES notes(id) ON DELETE CASCADE,
    
    CONSTRAINT fk_notemember_user FOREIGN KEY (user_id) 
        REFERENCES users(id) ON DELETE CASCADE,
    
    CONSTRAINT fk_notemember_inviter FOREIGN KEY (invited_by) 
        REFERENCES users(id),
    
    CONSTRAINT ck_notemember_role CHECK (
        role IN ('owner', 'editor', 'viewer')
    ),
    
    CONSTRAINT ck_notemember_status CHECK (
        invitation_status IN ('pending', 'active', 'declined', 'removed')
    ),
    
    -- Unique: User can only be member once per note
    CONSTRAINT uq_notemember_note_user UNIQUE (note_id, user_id, deleted_at)
);

-- Indexes for note_members

-- Primary lookup: Members of a note
CREATE INDEX ix_notemember_note ON note_members(note_id, deleted_at)
INCLUDE (user_id, role, invitation_status)
WHERE deleted_at IS NULL;

-- User lookup: Notes shared with user
CREATE INDEX ix_notemember_user ON note_members(user_id, deleted_at)
INCLUDE (note_id, role, invitation_status)
WHERE deleted_at IS NULL;

-- Owner lookup: Find note owners
CREATE INDEX ix_notemember_owner ON note_members(note_id, role, deleted_at)
WHERE role = 'owner' AND deleted_at IS NULL;

-- Comments
EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description',
    @value = N'Note sharing with 3 roles: owner, editor, viewer. Similar to workspace_members.',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE', @level1name = N'note_members';

PRINT '   ✅ note_members table created';
PRINT '   ✅ 3 indexes created (note, user, owner)';
GO

-- ============================================
-- VALIDATION
-- ============================================

PRINT '';
PRINT '📊 Verifying note_members...';

SELECT 
    name AS index_name,
    type_desc,
    filter_definition
FROM sys.indexes
WHERE object_id = OBJECT_ID('note_members')
AND name IS NOT NULL
ORDER BY name;

GO

-- ============================================
-- SUCCESS MESSAGE
-- ============================================

PRINT '';
PRINT '✅ note_members table created successfully!';
PRINT '';
PRINT '👥 ROLE-BASED PERMISSIONS:';
PRINT '   - owner: Full control (delete, manage members)';
PRINT '   - editor: Edit content, create versions';
PRINT '   - viewer: Read-only access';
PRINT '';
PRINT '📨 INVITATION WORKFLOW:';
PRINT '   1. Owner invites user → status = ''pending''';
PRINT '   2. User accepts → status = ''active'', joined_at = now';
PRINT '   3. User declines → status = ''declined''';
PRINT '   4. Owner removes → status = ''removed''';
PRINT '';
PRINT '🔒 SECURITY:';
PRINT '   - Auto-created by tr_notes_add_owner trigger';
PRINT '   - Note creator always gets ''owner'' role';
PRINT '   - Unique constraint prevents duplicate memberships';
PRINT '   - Soft delete support (deleted_at)';
PRINT '';
PRINT '🔍 COMMON QUERIES:';
PRINT '   - Notes shared with user: WHERE user_id = ? AND deleted_at IS NULL';
PRINT '   - Note members: WHERE note_id = ? AND deleted_at IS NULL';
PRINT '   - Note owners: WHERE note_id = ? AND role = ''owner'' AND deleted_at IS NULL';
PRINT '';
PRINT '📈 SCALE ESTIMATE:';
PRINT '   - ~5 members per shared note average';
PRINT '   - ~10M rows if 2M notes';
PRINT '   - 3 filtered indexes for fast lookups';
PRINT '';
PRINT '🔗 RELATED TABLES:';
PRINT '   - Similar to workspace_members (same permission model)';
PRINT '   - Triggered by notes table INSERT (tr_notes_add_owner)';
PRINT '   - Used by vw_user_notes view';
PRINT '';
GO
