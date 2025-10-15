-- ============================================
-- FILE: constraints/06.02-tags-constraints.sql
-- PURPOSE: Constraints for tags table
-- DEPENDENCIES: 02-tables-core.sql
-- ============================================

PRINT '🔒 Creating tag table constraints...';
GO

-- 📝 NOTE ON NAMING CONSISTENCY:
-- Some constraint names use different patterns for historical reasons:
-- - ck_tags_name_not_empty (should be ck_tags_name_valid)
-- Keeping existing names for backward compatibility. New constraints use:
-- Pattern: ck_{table}_{column}_{type} where type = format|valid|range|positive

-- Constraint: Tag name not empty
ALTER TABLE tags
ADD CONSTRAINT ck_tags_name_not_empty CHECK (
    LEN(LTRIM(RTRIM(name))) > 0
    AND LEN(name) BETWEEN 1 AND 255
);

PRINT '   ✅ ck_tags_name_not_empty added';

-- Constraint: Tag name doesn't start/end with whitespace
ALTER TABLE tags
ADD CONSTRAINT ck_tags_name_trimmed CHECK (
    name = LTRIM(RTRIM(name))
);

PRINT '   ✅ ck_tags_name_trimmed added';

-- Constraint: Color format (hex color)
ALTER TABLE tags
ADD CONSTRAINT ck_tags_color_format CHECK (
    color IS NULL 
    OR (color LIKE '#[0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f]')
);

PRINT '   ✅ ck_tags_color_format added';

-- Constraint: Usage count non-negative
ALTER TABLE tags
ADD CONSTRAINT ck_tags_usage_count CHECK (
    usage_count >= 0
);

PRINT '   ✅ ck_tags_usage_count added';

-- Constraint: Slug format (lowercase, alphanumeric, dash only)
ALTER TABLE tags
ADD CONSTRAINT ck_tags_slug_format CHECK (
    slug IS NULL
    OR (
        slug NOT LIKE '%[^a-z0-9-]%'
        AND slug NOT LIKE '-%'
        AND slug NOT LIKE '%-'
        AND slug NOT LIKE '%[-][-]%'
    )
);

PRINT '   ✅ ck_tags_slug_format added';

GO

PRINT '✅ Tag table constraints created successfully!';
PRINT '📊 Next step: Run constraints/06.03-workspaces-constraints.sql';
GO