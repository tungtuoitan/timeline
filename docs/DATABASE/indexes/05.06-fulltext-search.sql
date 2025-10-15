-- ============================================
-- FILE: indexes/05.06-fulltext-search.sql
-- PURPOSE: Full-text search setup for content search
-- DEPENDENCIES: 02-tables-core.sql, 07-tables-entities.sql
-- ============================================

PRINT '🔧 Setting up full-text search...';
GO

-- Create full-text catalog if not exists
IF NOT EXISTS (SELECT * FROM sys.fulltext_catalogs WHERE name = 'ft_superapp')
BEGIN
    CREATE FULLTEXT CATALOG ft_superapp AS DEFAULT;
    PRINT '   ✅ ft_superapp catalog created';
END
ELSE
BEGIN
    PRINT '   ℹ️  ft_superapp catalog already exists';
END

GO

-- Full-text index on notes.name, description, and content
IF NOT EXISTS (
    SELECT * FROM sys.fulltext_indexes fi
    JOIN sys.tables t ON fi.object_id = t.object_id
    WHERE t.name = 'notes'
)
BEGIN
    CREATE FULLTEXT INDEX ON notes(
        name LANGUAGE 1033,        -- English
        description LANGUAGE 1033,
        content LANGUAGE 1033
    )
    KEY INDEX PK_notes
    ON ft_superapp
    WITH CHANGE_TRACKING AUTO;
    
    PRINT '   ✅ Full-text index on notes created';
    PRINT '   ℹ️  Usage: SELECT * FROM notes WHERE CONTAINS(content, ''React'')';
END
ELSE
BEGIN
    PRINT '   ℹ️  Full-text index on notes already exists';
END

GO

-- Optional: Full-text index on tags (commented out for future use)
/*
IF NOT EXISTS (
    SELECT * FROM sys.fulltext_indexes fi
    JOIN sys.tables t ON fi.object_id = t.object_id
    WHERE t.name = 'tags'
)
BEGIN
    CREATE FULLTEXT INDEX ON tags(
        name LANGUAGE 1033,
        description LANGUAGE 1033
    )
    KEY INDEX PK_tags
    ON ft_superapp
    WITH CHANGE_TRACKING AUTO;
    
    PRINT '   ✅ Full-text index on tags created';
END
*/

PRINT '✅ Full-text search enabled for notes table!';
PRINT '📊 Next step: Run indexes/05.07-statistics-update.sql';
GO