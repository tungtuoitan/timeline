-- ============================================
-- FILE: relationships/04.05-functions.sql
-- PURPOSE: ⚠️ DEPRECATED - Helper functions for path calculations
-- DEPENDENCIES: None (do not use)
-- STATUS: Kept for reference only, replaced by workspace_items in 07-tables-entities.sql
-- ============================================

-- ⚠️⚠️⚠️ WARNING ⚠️⚠️⚠️
-- This file is DEPRECATED and should NOT be executed.
-- Replaced by: workspace_items (07-tables-entities.sql)
-- Migration: See 12-migration-guide.md

CREATE OR ALTER FUNCTION dbo.fn_calculate_depth(@path NVARCHAR(4000))
RETURNS INT
AS
BEGIN
    IF @path IS NULL OR @path = ''
        RETURN 0;
    
    RETURN LEN(@path) - LEN(REPLACE(@path, '.', '')) + 1;
END;
GO

CREATE OR ALTER FUNCTION dbo.fn_get_parent_path(@path NVARCHAR(4000))
RETURNS NVARCHAR(4000)
AS
BEGIN
    IF @path IS NULL OR @path = '' OR CHARINDEX('.', @path) = 0
        RETURN NULL;
    
    RETURN LEFT(@path, LEN(@path) - CHARINDEX('.', REVERSE(@path)));
END;
GO

CREATE OR ALTER FUNCTION dbo.fn_is_ancestor(
    @ancestor_path NVARCHAR(4000),
    @descendant_path NVARCHAR(4000)
)
RETURNS BIT
AS
BEGIN
    IF @ancestor_path IS NULL OR @descendant_path IS NULL
        RETURN 0;
    
    IF @descendant_path LIKE @ancestor_path + '.%'
        OR @descendant_path = @ancestor_path
        RETURN 1;
    
    RETURN 0;
END;
GO

PRINT '⚠️ DEPRECATED: Path helper functions created for reference only!';
PRINT '   Replaced by: workspace_items in 07-tables-entities.sql';
PRINT '   Migration: See 12-migration-guide.md';
PRINT '📊 Next step: Run relationships/04.06-validation-queries.sql (for reference only)';
GO