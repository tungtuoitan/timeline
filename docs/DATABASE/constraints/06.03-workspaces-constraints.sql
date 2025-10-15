-- ============================================
-- FILE: constraints/06.03-workspaces-constraints.sql
-- PURPOSE: Constraints for workspaces table
-- DEPENDENCIES: 03-tables-workspace.sql
-- ============================================

PRINT '🔒 Creating workspace table constraints...';
GO

-- 📝 NOTE ON NAMING CONSISTENCY:
-- Some constraint names use different patterns for historical reasons:
-- - ck_workspace_name_not_empty (should be ck_workspaces_name_valid)
-- Keeping existing names for backward compatibility. New constraints use:
-- Pattern: ck_{table}_{column}_{type} where type = format|valid|range|positive

-- Constraint: Workspace name not empty
ALTER TABLE workspaces
ADD CONSTRAINT ck_workspace_name_not_empty CHECK (
    LEN(LTRIM(RTRIM(name))) > 0
    AND LEN(name) BETWEEN 1 AND 200
);

PRINT '   ✅ ck_workspace_name_not_empty added';

-- Constraint: Workspace name trimmed
ALTER TABLE workspaces
ADD CONSTRAINT ck_workspace_name_trimmed CHECK (
    name = LTRIM(RTRIM(name))
);

PRINT '   ✅ ck_workspace_name_trimmed added';

-- Constraint: Color format (hex color)
ALTER TABLE workspaces
ADD CONSTRAINT ck_workspace_color_format CHECK (
    color IS NULL 
    OR (color LIKE '#[0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f]')
);

PRINT '   ✅ ck_workspace_color_format added';

-- Constraint: Statistics non-negative
ALTER TABLE workspaces
ADD CONSTRAINT ck_workspace_stats_positive CHECK (
    tag_count >= 0
    AND relationship_count >= 0
    AND member_count >= 1
);

PRINT '   ✅ ck_workspace_stats_positive added';

GO

PRINT '✅ Workspace table constraints created successfully!';
PRINT '📊 Next step: Run constraints/06.04-workspace-members-constraints.sql';
GO