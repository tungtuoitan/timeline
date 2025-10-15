-- ============================================
-- FILE: constraints/06.07-entity-types-constraints.sql
-- PURPOSE: Constraints for entity_types table
-- DEPENDENCIES: 07-tables-entities.sql
-- ============================================

PRINT '🔒 Creating entity_types table constraints...';
GO

-- Constraint: type_name format (lowercase, alphanumeric, underscore)
ALTER TABLE entity_types
ADD CONSTRAINT ck_entity_types_name_format CHECK (
    type_name NOT LIKE '%[^a-z0-9_]%'
    AND type_name NOT LIKE '[0-9]%'
    AND LEN(type_name) BETWEEN 2 AND 50
);

PRINT '   ✅ ck_entity_types_name_format added';

-- Constraint: display_name not empty
ALTER TABLE entity_types
ADD CONSTRAINT ck_entity_types_display_name CHECK (
    LEN(LTRIM(RTRIM(display_name))) > 0
    AND LEN(display_name) BETWEEN 1 AND 100
);

PRINT '   ✅ ck_entity_types_display_name added';

-- Constraint: icon_name format (lowercase with dash)
ALTER TABLE entity_types
ADD CONSTRAINT ck_entity_types_icon_format CHECK (
    icon_name IS NULL
    OR (
        icon_name NOT LIKE '%[^a-z0-9-]%'
        AND LEN(icon_name) BETWEEN 2 AND 50
    )
);

PRINT '   ✅ ck_entity_types_icon_format added';

-- Constraint: color format (hex color)
ALTER TABLE entity_types
ADD CONSTRAINT ck_entity_types_color_format CHECK (
    default_color IS NULL 
    OR (default_color LIKE '#[0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f]')
);

PRINT '   ✅ ck_entity_types_color_format added';

GO

PRINT '✅ Entity_types constraints created successfully!';
PRINT '📊 Next step: Run constraints/06.08-notes-constraints.sql';
GO