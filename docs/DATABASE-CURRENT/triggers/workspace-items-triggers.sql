-- =============================================
-- WORKSPACE_ITEMS TRIGGERS (1 trigger)
-- Status: ✅ DEPLOYED (MVP)
-- =============================================

-- =============================================
-- tr_workspace_items_update_stats
-- Purpose: Update workspace and tag statistics
-- Fires: AFTER INSERT, UPDATE, DELETE
-- =============================================
CREATE TRIGGER tr_workspace_items_update_stats
ON workspace_items
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    -- Update workspace stats
    UPDATE w
    SET
        tag_count = COALESCE((
            SELECT COUNT(DISTINCT tag_id)
            FROM (
                SELECT child_id AS tag_id
                FROM workspace_items
                WHERE workspace_id = w.workspace_id
                AND deleted_at IS NULL
                AND child_type = 'tag'

                UNION

                SELECT parent_tag_id AS tag_id
                FROM workspace_items
                WHERE workspace_id = w.workspace_id
                AND deleted_at IS NULL
            ) tag_union
        ), 0),
        relationship_count = COALESCE((
            SELECT COUNT(*)
            FROM workspace_items
            WHERE workspace_id = w.workspace_id
            AND deleted_at IS NULL
        ), 0),
        updated_at = GETUTCDATE()
    FROM workspaces w
    WHERE w.workspace_id IN (
        SELECT DISTINCT workspace_id FROM inserted
        UNION
        SELECT DISTINCT workspace_id FROM deleted
    );

    -- Update tags.usage_count
    UPDATE t
    SET usage_count = COALESCE((
        SELECT COUNT(DISTINCT workspace_id)
        FROM workspace_items wi
        WHERE (
            (wi.child_type = 'tag' AND wi.child_id = t.tag_id)
            OR wi.parent_tag_id = t.tag_id
        )
        AND wi.deleted_at IS NULL
    ), 0)
    FROM tags t
    WHERE t.tag_id IN (
        SELECT DISTINCT parent_tag_id FROM inserted
        UNION
        SELECT DISTINCT parent_tag_id FROM deleted
        UNION
        SELECT DISTINCT child_id FROM inserted WHERE child_type = 'tag'
        UNION
        SELECT DISTINCT child_id FROM deleted WHERE child_type = 'tag'
    );
END;
GO

-- =============================================
-- NOTES
-- =============================================
-- Updates:
-- 1. workspaces.tag_count - Distinct tags used (parent or child)
-- 2. workspaces.relationship_count - Total items in workspace
-- 3. workspaces.updated_at - Timestamp
-- 4. tags.usage_count - Number of workspaces using this tag
--
-- Performance: O(n) where n = items in affected workspaces
-- Should be fast for MVP (<100 items per workspace)
