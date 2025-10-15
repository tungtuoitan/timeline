-- ============================================
-- FILE: constraints/06.04-workspace-members-constraints.sql
-- PURPOSE: Constraints for workspace_members table
-- DEPENDENCIES: 03-tables-workspace.sql
-- ============================================

PRINT '🔒 Creating workspace members constraints...';
GO

-- Constraint: joined_at must be after invited_at
ALTER TABLE workspace_members
ADD CONSTRAINT ck_wsmember_join_after_invite CHECK (
    joined_at IS NULL
    OR joined_at >= invited_at
);

PRINT '   ✅ ck_wsmember_join_after_invite added';

-- Constraint: Active members must have joined_at
ALTER TABLE workspace_members
ADD CONSTRAINT ck_wsmember_active_has_joined CHECK (
    invitation_status != 'active'
    OR joined_at IS NOT NULL
);

PRINT '   ✅ ck_wsmember_active_has_joined added';

GO

PRINT '✅ Workspace members constraints created successfully!';
PRINT '📊 Next step: Run constraints/06.05-relationship-types-constraints.sql';
GO