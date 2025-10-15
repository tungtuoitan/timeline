-- ============================================
-- FILE: constraints/06.09-note-members-constraints.sql
-- PURPOSE: Constraints for note_members table
-- DEPENDENCIES: 07-tables-entities.sql
-- ============================================

PRINT '🔒 Creating note_members table constraints...';
GO

-- Constraint: role validation (owner, editor, viewer)
ALTER TABLE note_members
ADD CONSTRAINT ck_note_members_role_valid CHECK (
    role IN ('owner', 'editor', 'viewer')
);

PRINT '   ✅ ck_note_members_role_valid added';

-- Constraint: invitation_status validation
ALTER TABLE note_members
ADD CONSTRAINT ck_note_members_status_valid CHECK (
    invitation_status IN ('pending', 'active', 'declined', 'removed')
);

PRINT '   ✅ ck_note_members_status_valid added';

-- Constraint: joined_at must be after invited_at
ALTER TABLE note_members
ADD CONSTRAINT ck_note_members_join_after_invite CHECK (
    joined_at IS NULL
    OR joined_at >= invited_at
);

PRINT '   ✅ ck_note_members_join_after_invite added';

-- Constraint: Active members must have joined_at
ALTER TABLE note_members
ADD CONSTRAINT ck_note_members_active_has_joined CHECK (
    invitation_status != 'active'
    OR joined_at IS NOT NULL
);

PRINT '   ✅ ck_note_members_active_has_joined added';

-- Constraint: Owner must be active
ALTER TABLE note_members
ADD CONSTRAINT ck_note_members_owner_active CHECK (
    role != 'owner'
    OR invitation_status = 'active'
);

PRINT '   ✅ ck_note_members_owner_active added';

GO

PRINT '✅ Note_members constraints created successfully!';
PRINT '📊 Next step: Run constraints/06.10-note-versions-constraints.sql';
GO