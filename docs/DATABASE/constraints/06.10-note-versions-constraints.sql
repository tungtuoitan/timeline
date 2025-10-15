-- ============================================
-- FILE: constraints/06.10-note-versions-constraints.sql
-- PURPOSE: Constraints for note_versions table
-- DEPENDENCIES: 07-tables-entities.sql
-- ============================================

PRINT '🔒 Creating note_versions table constraints...';
GO

-- Constraint: version_number positive
ALTER TABLE note_versions
ADD CONSTRAINT ck_note_versions_version_number_positive CHECK (
    version_number > 0
);

PRINT '   ✅ ck_note_versions_version_number_positive added';

-- Constraint: content not null
ALTER TABLE note_versions
ADD CONSTRAINT ck_note_versions_content_not_null CHECK (
    content IS NOT NULL
);

PRINT '   ✅ ck_note_versions_content_not_null added';

-- Constraint: word_count non-negative
ALTER TABLE note_versions
ADD CONSTRAINT ck_note_versions_word_count_positive CHECK (
    word_count >= 0
);

PRINT '   ✅ ck_note_versions_word_count_positive added';

-- Constraint: created_by valid user
ALTER TABLE note_versions
ADD CONSTRAINT ck_note_versions_created_by_valid CHECK (
    created_by IS NOT NULL
);

PRINT '   ✅ ck_note_versions_created_by_valid added';

-- Constraint: content reasonable length
ALTER TABLE note_versions
ADD CONSTRAINT ck_note_versions_content_length CHECK (
    LEN(content) <= 10000000  -- 10MB text limit
);

PRINT '   ✅ ck_note_versions_content_length added';

GO

PRINT '✅ Note_versions constraints created successfully!';
PRINT '📊 Next step: Run constraints/06.11-deprecated-constraints.sql';
GO