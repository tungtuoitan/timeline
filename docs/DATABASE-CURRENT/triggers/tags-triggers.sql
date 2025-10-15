-- =============================================
-- TAGS TRIGGERS (1 trigger)
-- Status: ✅ DEPLOYED (MVP)
-- =============================================

-- =============================================
-- tr_tags_generate_slug
-- Purpose: Auto-generate URL-friendly slug from tag name
-- Fires: AFTER INSERT, UPDATE
-- =============================================
CREATE TRIGGER tr_tags_generate_slug
ON tags
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE t
    SET slug = LOWER(
        REPLACE(
            REPLACE(
                REPLACE(
                    REPLACE(i.name, ' ', '-'),
                    '/', '-'),
                '_', '-'),
            '--', '-')
    )
    FROM tags t
    INNER JOIN inserted i ON t.tag_id = i.tag_id
    WHERE t.slug IS NULL OR t.slug = '';
END;
GO

-- =============================================
-- NOTES
-- =============================================
-- - Slug generated AFTER INSERT (tags.slug is nullable)
-- - Replaces spaces, slashes, underscores with hyphens
-- - Converts to lowercase
-- - Only updates if slug is NULL or empty
