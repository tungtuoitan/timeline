-- =============================================
-- NOTES TRIGGERS (4 triggers)
-- Status: ✅ DEPLOYED (MVP) - WITH RECURSION FIX
-- =============================================
-- All triggers include TRIGGER_NESTLEVEL() checks to prevent recursion
-- See FIX-NOTES-TRIGGERS.sql for details
-- =============================================

-- =============================================
-- 1. tr_notes_generate_slug
-- Purpose: Auto-generate URL-friendly slug from note name
-- Fires: AFTER INSERT, UPDATE
-- Recursion: Protected with TRIGGER_NESTLEVEL()
-- =============================================
CREATE TRIGGER tr_notes_generate_slug
ON notes
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Prevent recursion
    IF TRIGGER_NESTLEVEL() > 1 RETURN;

    UPDATE n
    SET slug = LOWER(
        REPLACE(
            REPLACE(
                REPLACE(
                    REPLACE(i.name, ' ', '-'),
                    '/', '-'),
                '_', '-'),
            '--', '-')
    )
    FROM notes n
    INNER JOIN inserted i ON n.note_id = i.note_id
    WHERE n.slug IS NULL OR n.slug = '';
END;
GO

-- =============================================
-- 2. tr_notes_updated_at
-- Purpose: Update updated_at timestamp on meaningful changes
-- Fires: AFTER UPDATE
-- Recursion: Protected with TRIGGER_NESTLEVEL()
-- =============================================
CREATE TRIGGER tr_notes_updated_at
ON notes
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Prevent recursion
    IF TRIGGER_NESTLEVEL() > 1 RETURN;

    -- Only update if meaningful columns changed
    -- Excludes: updated_at, version_count, slug (to prevent recursion)
    IF UPDATE(name) OR UPDATE(description) OR UPDATE(content) OR
       UPDATE(color) OR UPDATE(icon) OR UPDATE(is_archived) OR
       UPDATE(is_pinned) OR UPDATE(is_favorite) OR UPDATE(word_count)
    BEGIN
        UPDATE notes
        SET updated_at = GETUTCDATE()
        WHERE note_id IN (SELECT note_id FROM inserted);
    END;
END;
GO

-- =============================================
-- 3. tr_notes_add_owner
-- Purpose: Auto-add note creator as owner member
-- Fires: AFTER INSERT
-- Recursion: No risk (inserts to different table)
-- =============================================
CREATE TRIGGER tr_notes_add_owner
ON notes
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO note_members (
        note_id, user_id, role, invited_by,
        invitation_status, joined_at
    )
    SELECT
        i.note_id,
        i.user_id,
        'owner',
        i.user_id,
        'active',
        GETUTCDATE()
    FROM inserted i;
END;
GO

-- =============================================
-- 4. tr_notes_create_version
-- Purpose: Auto-create version snapshot on content changes
-- Fires: AFTER INSERT, UPDATE
-- Recursion: Protected with TRIGGER_NESTLEVEL()
-- =============================================
CREATE TRIGGER tr_notes_create_version
ON notes
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Prevent recursion
    IF TRIGGER_NESTLEVEL() > 1 RETURN;

    -- Only create version if content/name/description changed
    IF UPDATE(content) OR UPDATE(name) OR UPDATE(description)
    BEGIN
        -- Insert version
        INSERT INTO note_versions (
            note_id, version_number, name, description, content,
            word_count, created_by, created_at
        )
        SELECT
            i.note_id,
            ISNULL(
                (SELECT MAX(version_number)
                 FROM note_versions
                 WHERE note_id = i.note_id),
                0
            ) + ROW_NUMBER() OVER (PARTITION BY i.note_id ORDER BY (SELECT NULL)),
            i.name,
            i.description,
            i.content,
            i.word_count,
            i.user_id,
            GETUTCDATE()
        FROM inserted i;

        -- Update version_count
        -- (This will NOT trigger updated_at due to exclusion in tr_notes_updated_at)
        UPDATE n
        SET version_count = (
            SELECT COUNT(*)
            FROM note_versions
            WHERE note_id = n.note_id
        )
        FROM notes n
        INNER JOIN inserted i ON n.note_id = i.note_id;
    END;
END;
GO

-- =============================================
-- RECURSION PREVENTION STRATEGY
-- =============================================
-- Problem: Triggers could cause infinite loops
--   tr_notes_generate_slug → UPDATE → tr_notes_updated_at → UPDATE → ...
--
-- Solution:
-- 1. TRIGGER_NESTLEVEL() > 1 RETURN - Prevents nested execution
-- 2. Conditional UPDATE() checks - Only fires on specific columns
-- 3. Exclude metadata columns - updated_at, version_count, slug excluded
--
-- Result: No recursion, all triggers work correctly
--
-- See: DATABASE/FIX-NOTES-TRIGGERS.sql for full fix details
