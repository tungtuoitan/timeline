-- ============================================
-- FILE: constraints/06.05-relationship-types-constraints.sql
-- PURPOSE: Constraints for workspace_relationship_types table
-- DEPENDENCIES: 03-tables-workspace.sql
-- ============================================

PRINT '🔒 Creating relationship type constraints...';
GO

-- Constraint: Display name not empty
ALTER TABLE workspace_relationship_types
ADD CONSTRAINT ck_wsreltype_display_name CHECK (
    LEN(LTRIM(RTRIM(display_name))) > 0
);

PRINT '   ✅ ck_wsreltype_display_name added';

-- Constraint: Type name format (lowercase, alphanumeric, underscore)
ALTER TABLE workspace_relationship_types
ADD CONSTRAINT ck_wsreltype_name_format CHECK (
    type_name NOT LIKE '%[^a-z0-9_]%'
    AND type_name NOT LIKE '[0-9]%'
    AND LEN(type_name) BETWEEN 2 AND 50
);

PRINT '   ✅ ck_wsreltype_name_format added';

-- Constraint: Line width positive
ALTER TABLE workspace_relationship_types
ADD CONSTRAINT ck_wsreltype_line_width CHECK (
    line_width > 0 AND line_width <= 10
);

PRINT '   ✅ ck_wsreltype_line_width added';

-- Constraint: Color format
ALTER TABLE workspace_relationship_types
ADD CONSTRAINT ck_wsreltype_color_format CHECK (
    color IS NULL 
    OR (color LIKE '#[0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f]')
);

PRINT '   ✅ ck_wsreltype_color_format added';

GO

PRINT '✅ Relationship type constraints created successfully!';
PRINT '📊 Next step: Run constraints/06.06-workspace-items-constraints.sql';
GO