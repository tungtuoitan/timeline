-- =============================================
-- MIGRATION PATCH: Note Tags → Entity Tags
-- Description: Migrate note_tags to polymorphic entity_tags table
-- Author: Claude Code
-- Date: 2025-01-29
-- =============================================

-- NOTE: This patch should be run AFTER the main migration script
-- (MIGRATION_TAGS_TO_FOLDERS_WITH_SHARE_FORK.sql)
-- Specifically, run this after PHASE 6 creates the entity_tags table

PRINT '========================================';
PRINT 'MIGRATION PATCH: Note Tags → Entity Tags';
PRINT 'Time: ' + CONVERT(VARCHAR, GETUTCDATE(), 120);
PRINT '========================================';
GO

-- =============================================
-- PHASE 6A: MIGRATE NOTE_TAGS TO ENTITY_TAGS
-- =============================================
PRINT '';
PRINT 'PHASE 6A: Migrating note_tags to entity_tags...';

-- Check if note_tags table exists
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'note_tags')
BEGIN
    PRINT '  Found note_tags table, proceeding with migration...';

    -- Check if entity_tags table exists
    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'entity_tags')
    BEGIN
        PRINT '  ERROR: entity_tags table does not exist!';
        PRINT '  Please run the main migration script first (MIGRATION_TAGS_TO_FOLDERS_WITH_SHARE_FORK.sql)';
        RAISERROR('entity_tags table not found', 16, 1);
        RETURN;
    END;

    -- Check if tags_new table exists (created in PHASE 6 of main migration)
    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'tags_new')
    BEGIN
        PRINT '  ERROR: tags_new table does not exist!';
        PRINT '  Please run PHASE 6 of the main migration script first';
        RAISERROR('tags_new table not found', 16, 1);
        RETURN;
    END;

    -- Validate note_tags data
    DECLARE @note_tags_count INT;
    SELECT @note_tags_count = COUNT(*) FROM note_tags;
    PRINT '  Total note_tags records to migrate: ' + CAST(@note_tags_count AS VARCHAR(10));

    -- Check for orphaned records (notes that don't exist)
    DECLARE @orphaned_notes INT;
    SELECT @orphaned_notes = COUNT(*)
    FROM note_tags nt
    WHERE NOT EXISTS (SELECT 1 FROM notes n WHERE n.note_id = nt.note_id);

    IF @orphaned_notes > 0
    BEGIN
        PRINT '  WARNING: Found ' + CAST(@orphaned_notes AS VARCHAR(10)) + ' orphaned note_tags (notes don''t exist)';
        PRINT '  These will be skipped during migration';
    END;

    -- Check for tags that will become folders (old tags table)
    DECLARE @invalid_tags INT;
    SELECT @invalid_tags = COUNT(DISTINCT nt.tag_id)
    FROM note_tags nt
    WHERE NOT EXISTS (SELECT 1 FROM notes n WHERE n.note_id = nt.note_id);

    PRINT '  Valid note-tag associations: ' + CAST(@note_tags_count - @orphaned_notes AS VARCHAR(10));

    -- =============================================
    -- MIGRATION STRATEGY:
    -- =============================================
    -- Problem: note_tags references OLD tags table (which becomes folders)
    -- Solution: Create NEW hashtags in tags_new table for each unique tag name
    --           then migrate note_tags relationships to entity_tags
    -- =============================================

    PRINT '';
    PRINT '  Step 1: Creating hashtags from old tags used in note_tags...';

    -- Create hashtags in tags_new from distinct tags used in note_tags
    -- Only for tags that are actively used by notes
    INSERT INTO tags_new (user_id, name, slug, color, usage_count, created_at, updated_at)
    SELECT DISTINCT
        t.user_id,
        t.name,
        t.slug,
        COALESCE(t.color, '#6B7280') as color, -- Default gray for hashtags
        0 as usage_count, -- Will be updated by trigger
        t.created_at,
        GETUTCDATE() as updated_at
    FROM tags t
    INNER JOIN note_tags nt ON t.tag_id = nt.tag_id
    INNER JOIN notes n ON n.note_id = nt.note_id -- Only migrate tags for existing notes
    WHERE NOT EXISTS (
        -- Don't create duplicates if tag already exists in tags_new
        SELECT 1 FROM tags_new tn
        WHERE tn.user_id = t.user_id
        AND tn.slug = t.slug
    );

    DECLARE @hashtags_created INT = @@ROWCOUNT;
    PRINT '    Created ' + CAST(@hashtags_created AS VARCHAR(10)) + ' new hashtags from old tags';

    PRINT '';
    PRINT '  Step 2: Migrating note_tags to entity_tags...';

    -- Migrate note_tags to entity_tags
    -- Map old tag_id to new tag_id via slug matching
    INSERT INTO entity_tags (tag_id, entity_type, entity_id, tagged_by, created_at, deleted_at)
    SELECT
        tn.tag_id as tag_id,           -- New tag ID from tags_new
        'note' as entity_type,          -- Entity type
        nt.note_id as entity_id,        -- Note ID
        COALESCE(n.user_id, 1) as tagged_by, -- User who owns the note (or default user)
        nt.created_at as created_at,
        NULL as deleted_at
    FROM note_tags nt
    INNER JOIN notes n ON n.note_id = nt.note_id -- Only migrate for existing notes
    INNER JOIN tags t ON t.tag_id = nt.tag_id    -- Get old tag info
    INNER JOIN tags_new tn ON tn.slug = t.slug AND tn.user_id = t.user_id -- Match to new tag
    WHERE NOT EXISTS (
        -- Avoid duplicates
        SELECT 1 FROM entity_tags et
        WHERE et.entity_type = 'note'
        AND et.entity_id = nt.note_id
        AND et.tag_id = tn.tag_id
    );

    DECLARE @migrated_count INT = @@ROWCOUNT;
    PRINT '    Migrated ' + CAST(@migrated_count AS VARCHAR(10)) + ' note-tag associations to entity_tags';

    -- =============================================
    -- VALIDATION
    -- =============================================
    PRINT '';
    PRINT '  Step 3: Validating migration...';

    DECLARE @entity_tags_note_count INT;
    SELECT @entity_tags_note_count = COUNT(*)
    FROM entity_tags
    WHERE entity_type = 'note';

    PRINT '    Total entity_tags with entity_type=''note'': ' + CAST(@entity_tags_note_count AS VARCHAR(10));

    -- Check for any note_tags that weren't migrated
    DECLARE @unmigrated_count INT;
    SELECT @unmigrated_count = COUNT(*)
    FROM note_tags nt
    INNER JOIN notes n ON n.note_id = nt.note_id
    WHERE NOT EXISTS (
        SELECT 1
        FROM entity_tags et
        INNER JOIN tags_new tn ON et.tag_id = tn.tag_id
        INNER JOIN tags t ON t.slug = tn.slug
        WHERE et.entity_type = 'note'
        AND et.entity_id = nt.note_id
        AND t.tag_id = nt.tag_id
    );

    IF @unmigrated_count > 0
    BEGIN
        PRINT '    WARNING: ' + CAST(@unmigrated_count AS VARCHAR(10)) + ' note_tags were not migrated!';
        PRINT '    This may indicate missing tags in tags_new table';
    END
    ELSE
    BEGIN
        PRINT '    ✓ All valid note_tags migrated successfully';
    END;

    -- =============================================
    -- DROP OLD NOTE_TAGS TABLE
    -- =============================================
    PRINT '';
    PRINT '  Step 4: Dropping note_tags table...';

    -- Drop foreign key constraints first
    DECLARE @constraint_name NVARCHAR(200);

    -- Find and drop FK to notes
    SELECT @constraint_name = name
    FROM sys.foreign_keys
    WHERE parent_object_id = OBJECT_ID('note_tags')
    AND referenced_object_id = OBJECT_ID('notes');

    IF @constraint_name IS NOT NULL
    BEGIN
        EXEC('ALTER TABLE note_tags DROP CONSTRAINT ' + @constraint_name);
        PRINT '    Dropped FK constraint: ' + @constraint_name;
    END;

    -- Find and drop FK to tags (old table, now folders)
    SET @constraint_name = NULL;
    SELECT @constraint_name = name
    FROM sys.foreign_keys
    WHERE parent_object_id = OBJECT_ID('note_tags')
    AND referenced_object_id = OBJECT_ID('tags');

    IF @constraint_name IS NOT NULL
    BEGIN
        EXEC('ALTER TABLE note_tags DROP CONSTRAINT ' + @constraint_name);
        PRINT '    Dropped FK constraint: ' + @constraint_name;
    END;

    -- Drop the table
    DROP TABLE IF EXISTS note_tags;
    PRINT '    ✓ note_tags table dropped successfully';

    PRINT '';
    PRINT '  ✓ PHASE 6A completed successfully!';
END
ELSE
BEGIN
    PRINT '  Note: note_tags table not found (may have been dropped already)';
END;
GO

-- =============================================
-- FINAL VALIDATION
-- =============================================
PRINT '';
PRINT '========================================';
PRINT 'MIGRATION PATCH COMPLETED';
PRINT '========================================';

-- Summary report
PRINT '';
PRINT 'SUMMARY:';

DECLARE @final_hashtags INT, @final_entity_tags INT;

SELECT @final_hashtags = COUNT(*) FROM tags_new WHERE deleted_at IS NULL;
SELECT @final_entity_tags = COUNT(*) FROM entity_tags WHERE entity_type = 'note' AND deleted_at IS NULL;

PRINT '  Total hashtags in tags_new: ' + CAST(@final_hashtags AS VARCHAR(10));
PRINT '  Total note tags in entity_tags: ' + CAST(@final_entity_tags AS VARCHAR(10));
PRINT '  note_tags table: ' + CASE WHEN EXISTS(SELECT 1 FROM sys.tables WHERE name = 'note_tags')
                                   THEN 'STILL EXISTS (ERROR!)'
                                   ELSE 'DROPPED ✓'
                              END;

PRINT '';
PRINT 'Next steps:';
PRINT '  1. Update Note model to use entity_tags navigation property';
PRINT '  2. Update NoteConfiguration.cs to configure entity_tags relationship';
PRINT '  3. Update NoteDTO DTO to load tags from entity_tags';
PRINT '  4. Test notes API to ensure tags are loaded correctly';
PRINT '';
PRINT 'Migration patch complete at: ' + CONVERT(VARCHAR, GETUTCDATE(), 120);
GO
