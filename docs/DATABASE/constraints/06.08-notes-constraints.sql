-- ============================================
-- FILE: constraints/06.08-notes-constraints.sql
-- PURPOSE: Constraints for notes table
-- DEPENDENCIES: 07-tables-entities.sql
-- ============================================

PRINT '🔒 Creating notes table constraints...';
GO

-- Constraint: name not empty
ALTER TABLE notes
ADD CONSTRAINT ck_notes_name_not_empty CHECK (
    LEN(LTRIM(RTRIM(name))) > 0
    AND LEN(name) BETWEEN 1 AND 200
);

PRINT '   ✅ ck_notes_name_not_empty added';

-- Constraint: name trimmed
ALTER TABLE notes
ADD CONSTRAINT ck_notes_name_trimmed CHECK (
    name = LTRIM(RTRIM(name))
);

PRINT '   ✅ ck_notes_name_trimmed added';

-- Constraint: word_count non-negative
ALTER TABLE notes
ADD CONSTRAINT ck_notes_word_count_positive CHECK (
    word_count >= 0
);

PRINT '   ✅ ck_notes_word_count_positive added';

-- Constraint: version_count positive
ALTER TABLE notes
ADD CONSTRAINT ck_notes_version_count_positive CHECK (
    version_count >= 1
);

PRINT '   ✅ ck_notes_version_count_positive added';

-- Constraint: slug format (lowercase, alphanumeric, dash only)
ALTER TABLE notes
ADD CONSTRAINT ck_notes_slug_format CHECK (
    slug IS NULL
    OR (
        slug NOT LIKE '%[^a-z0-9-]%'
        AND slug NOT LIKE '-%'
        AND slug NOT LIKE '%-'
        AND slug NOT LIKE '%[-][-]%'
        AND LEN(slug) BETWEEN 3 AND 200
    )
);

PRINT '   ✅ ck_notes_slug_format added';

-- Constraint: color format (hex color)
ALTER TABLE notes
ADD CONSTRAINT ck_notes_color_format CHECK (
    color IS NULL 
    OR (color LIKE '#[0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f]')
);

PRINT '   ✅ ck_notes_color_format added';

-- Constraint: content reasonable length (optional, adjust as needed)
ALTER TABLE notes
ADD CONSTRAINT ck_notes_content_length CHECK (
    content IS NULL
    OR LEN(content) <= 10000000  -- 10MB text limit
);

PRINT '   ✅ ck_notes_content_length added';

GO

PRINT '✅ Notes table constraints created successfully!';
PRINT '📊 Next step: Run constraints/06.09-note-members-constraints.sql';
GO