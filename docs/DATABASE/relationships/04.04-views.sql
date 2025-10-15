-- ============================================
-- FILE: relationships/04.04-views.sql
-- PURPOSE: ⚠️ DEPRECATED - Views for simplified relationship queries
-- DEPENDENCIES: relationships/04.01-table.sql (do not use)
-- STATUS: Kept for reference only, replaced by workspace_items in 07-tables-entities.sql
-- ============================================

-- ⚠️⚠️⚠️ WARNING ⚠️⚠️⚠️
-- This file is DEPRECATED and should NOT be executed.
-- Replaced by: workspace_items (07-tables-entities.sql)
-- Migration: See 12-migration-guide.md

CREATE OR ALTER VIEW vw_workspace_relationships AS
SELECT 
    wtr.id,
    wtr.workspace_id,
    w.name AS workspace_name,
    wtr.from_tag_id,
    t_from.name AS from_tag_name,
    t_from.color AS from_tag_color,
    t_from.icon AS from_tag_icon,
    wtr.to_tag_id,
    t_to.name AS to_tag_name,
    t_to.color AS to_tag_color,
    t_to.icon AS to_tag_icon,
    wtr.relationship_type,
    wrt.display_name AS relationship_display_name,
    wtr.from_path,
    wtr.to_path,
    wtr.depth,
    wtr.sort_order,
    wtr.label,
    wtr.is_bidirectional,
    wtr.strength,
    wtr.created_at,
    wtr.updated_at
FROM workspace_tag_relationships wtr
INNER JOIN workspaces w ON wtr.workspace_id = w.id
INNER JOIN tags t_from ON wtr.from_tag_id = t_from.id
INNER JOIN tags t_to ON wtr.to_tag_id = t_to.id
LEFT JOIN workspace_relationship_types wrt 
    ON wrt.workspace_id = wtr.workspace_id 
    AND wrt.type_name = wtr.relationship_type
    AND wrt.deleted_at IS NULL
WHERE wtr.deleted_at IS NULL
AND w.deleted_at IS NULL
AND t_from.deleted_at IS NULL
AND t_to.deleted_at IS NULL;
GO

CREATE OR ALTER VIEW vw_workspace_root_tags AS
SELECT DISTINCT
    wtr.workspace_id,
    w.name AS workspace_name,
    wtr.from_tag_id AS root_tag_id,
    t.name AS root_tag_name,
    t.color AS root_tag_color,
    t.icon AS root_tag_icon,
    COUNT(*) OVER (PARTITION BY wtr.workspace_id, wtr.from_tag_id) AS child_count
FROM workspace_tag_relationships wtr
INNER JOIN workspaces w ON wtr.workspace_id = w.id
INNER JOIN tags t ON wtr.from_tag_id = t.id
WHERE wtr.depth = 0
AND wtr.deleted_at IS NULL
AND w.deleted_at IS NULL
AND t.deleted_at IS NULL;
GO

CREATE OR ALTER VIEW vw_tag_workspace_usage AS
SELECT 
    t.id AS tag_id,
    t.user_id,
    t.name AS tag_name,
    t.color,
    t.icon,
    w.id AS workspace_id,
    w.name AS workspace_name,
    w.type AS workspace_type,
    COUNT(*) AS relationship_count,
    MIN(wtr.created_at) AS first_used_at,
    MAX(wtr.updated_at) AS last_used_at
FROM tags t
INNER JOIN workspace_tag_relationships wtr 
    ON (wtr.from_tag_id = t.id OR wtr.to_tag_id = t.id)
INNER JOIN workspaces w ON wtr.workspace_id = w.id
WHERE t.deleted_at IS NULL
AND wtr.deleted_at IS NULL
AND w.deleted_at IS NULL
GROUP BY 
    t.id, t.user_id, t.name, t.color, t.icon,
    w.id, w.name, w.type;
GO

PRINT '⚠️ DEPRECATED: Views for workspace_tag_relationships created for reference only!';
PRINT '   Replaced by: workspace_items in 07-tables-entities.sql';
PRINT '   Migration: See 12-migration-guide.md';
PRINT '📊 Next step: Run relationships/04.05-functions.sql (for reference only)';
GO