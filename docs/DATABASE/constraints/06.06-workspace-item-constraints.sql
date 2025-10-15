-- ============================================
-- FILE: constraints/06.06-workspace-items-constraints.sql
-- PURPOSE: Constraints for workspace_items table (unified system)
-- DEPENDENCIES: 07-tables-entities.sql
-- ============================================

PRINT '🔒 Creating workspace_items table constraints...';
GO

-- 📝 NOTE ON NAMING CONSISTENCY:
-- Some constraint names use different patterns for historical reasons:
-- - ck_wsitems_* (should be ck_workspace_items_*)
-- Keeping existing names for backward compatibility. New constraints use:
-- Pattern: ck_{table}_{column}_{type} where type = format|valid|range|positive

-- Constraint: item_path format validation (segments with dots, type:id format)
ALTER TABLE workspace_items
ADD CONSTRAINT ck_wsitems_path_format CHECK (
    item_path IS NULL
    OR (
        item_path NOT LIKE '.%'
        AND item_path NOT LIKE '%.'
        AND item_path NOT LIKE '%..'
        AND item_path LIKE '%:%'
        AND LEN(item_path) >= 5
    )
);

PRINT '   ✅ ck_wsitems_path_format added';

-- Constraint: depth non-negative
ALTER TABLE workspace_items
ADD CONSTRAINT ck_wsitems_depth_positive CHECK (
    depth >= 0
);

PRINT '   ✅ ck_wsitems_depth_positive added';

-- Constraint: child_type must be valid entity type
ALTER TABLE workspace_items
ADD CONSTRAINT ck_wsitems_child_type_valid CHECK (
    child_type IN ('tag', 'note')
    OR LEN(child_type) BETWEEN 2 AND 50
);

PRINT '   ✅ ck_wsitems_child_type_valid added';

-- Constraint: child_id positive
ALTER TABLE workspace_items
ADD CONSTRAINT ck_wsitems_child_id_positive CHECK (
    child_id > 0
);

PRINT '   ✅ ck_wsitems_child_id_positive added';

-- Constraint: label length
ALTER TABLE workspace_items
ADD CONSTRAINT ck_wsitems_label_length CHECK (
    label IS NULL
    OR LEN(label) BETWEEN 1 AND 200
);

PRINT '   ✅ ck_wsitems_label_length added';

-- Constraint: sort_order non-negative
ALTER TABLE workspace_items
ADD CONSTRAINT ck_wsitems_sort_order CHECK (
    sort_order >= 0
);

PRINT '   ✅ ck_wsitems_sort_order added';

GO

PRINT '✅ Workspace_items constraints created successfully!';
PRINT '📊 Next step: Run constraints/06.07-entity-types-constraints.sql';
GO