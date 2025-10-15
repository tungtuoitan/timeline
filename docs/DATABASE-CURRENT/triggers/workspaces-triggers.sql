-- =============================================
-- WORKSPACES TRIGGERS (3 triggers)
-- Status: ✅ DEPLOYED (MVP)
-- =============================================

-- =============================================
-- 1. tr_workspace_generate_slug (PLANNED - not deployed yet)
-- Purpose: Auto-generate URL-friendly slug from workspace name
-- Note: Mentioned in docs but column doesn't exist in schema
-- =============================================
-- NOT DEPLOYED - workspaces table has no slug column

-- =============================================
-- 2. tr_workspaces_add_owner
-- Purpose: Auto-add workspace creator as owner member
-- Fires: AFTER INSERT
-- =============================================
CREATE TRIGGER tr_workspaces_add_owner
ON workspaces
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO workspace_members (
        workspace_id,
        user_id,
        role,
        invited_by,
        invitation_status,
        joined_at
    )
    SELECT
        i.workspace_id,
        i.user_id,
        'owner',
        i.user_id,
        'active',
        GETUTCDATE()
    FROM inserted i;

    UPDATE w
    SET member_count = 1
    FROM workspaces w
    INNER JOIN inserted i ON w.workspace_id = i.workspace_id;
END;
GO

-- =============================================
-- 3. tr_workspaces_update_stats (NOT DEPLOYED - using tr_workspace_items_update_stats instead)
-- Purpose: Update workspace statistics
-- Note: Stats updated by workspace_items trigger instead
-- =============================================
-- NOT DEPLOYED - functionality in tr_workspace_items_update_stats

-- =============================================
-- NOTES
-- =============================================
-- - tr_workspaces_add_owner: Creates owner membership automatically
-- - member_count initialized to 1
-- - tr_workspace_members_update_count (on workspace_members) updates member_count
-- - tag_count/relationship_count updated by tr_workspace_items_update_stats
