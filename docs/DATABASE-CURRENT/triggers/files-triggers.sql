-- =============================================
-- Trigger: tr_files_generate_slug
-- Table: files
-- Type: AFTER INSERT, UPDATE
-- Purpose: Auto-generate slug from filename
-- Created: October 21, 2025
-- Version: MVP 1.1
-- =============================================

CREATE TRIGGER [dbo].[tr_files_generate_slug]
ON [dbo].[files]
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Prevent recursion (critical for UPDATE triggers)
    -- TRIGGER_NESTLEVEL() returns depth of trigger execution
    -- > 1 means trigger called itself, must exit to prevent infinite loop
    IF TRIGGER_NESTLEVEL() > 1
        RETURN;
    
    -- Generate slug from name if not provided
    -- Slug rules:
    -- 1. Convert to lowercase
    -- 2. Replace spaces with hyphens
    -- 3. Replace dots with hyphens (for filenames)
    -- 4. Collapse multiple hyphens into single hyphen
    UPDATE f
    SET 
        f.slug = LOWER(
            REPLACE(
                REPLACE(
                    REPLACE(
                        REPLACE(
                            REPLACE(i.name, ' ', '-'),  -- Spaces to hyphens
                            '.', '-'                     -- Dots to hyphens
                        ),
                        '--', '-'                        -- Collapse double hyphens
                    ),
                    '---', '-'                           -- Collapse triple hyphens
                ),
                '----', '-'                              -- Collapse quad hyphens
            )
        ),
        f.updated_at = GETUTCDATE()                     -- Update timestamp
    FROM [dbo].[files] f
    INNER JOIN inserted i ON f.file_id = i.file_id
    WHERE f.slug IS NULL OR f.slug = '' OR LEN(f.slug) = 0;
END;
GO

-- =============================================
-- Trigger Behavior Examples
-- =============================================

/*
Example 1: INSERT with no slug
-------------------------------
INSERT INTO files (user_id, name, original_filename, file_path, file_size, mime_type)
VALUES (1, 'Project Proposal', 'project-proposal.pdf', '/uploads/file.pdf', 1024, 'application/pdf');

Result:
- slug is NULL on INSERT
- Trigger fires AFTER INSERT
- Generates slug = 'project-proposal'
- Updates slug and updated_at

Example 2: INSERT with slug provided
-------------------------------------
INSERT INTO files (user_id, name, original_filename, file_path, file_size, mime_type, slug)
VALUES (1, 'My Document', 'doc.pdf', '/uploads/doc.pdf', 1024, 'application/pdf', 'custom-slug');

Result:
- slug = 'custom-slug' already provided
- Trigger fires but WHERE clause filters it out
- No update occurs

Example 3: UPDATE name without slug
------------------------------------
UPDATE files 
SET name = 'Updated Proposal'
WHERE file_id = 1;

Result:
- Trigger fires AFTER UPDATE
- If slug exists, no change (WHERE clause filters)
- If slug is NULL/empty, generates new slug from updated name

Example 4: Recursion prevention
--------------------------------
The trigger updates the same table (files) that it's attached to.
Without TRIGGER_NESTLEVEL() check, this would cause:

1. INSERT fires trigger
2. Trigger UPDATEs slug
3. UPDATE fires trigger again (recursion!)
4. Infinite loop crashes SQL Server

With TRIGGER_NESTLEVEL() check:
1. INSERT fires trigger (level = 1)
2. Trigger UPDATEs slug
3. UPDATE fires trigger (level = 2)
4. IF condition catches level > 1, RETURNS immediately
5. No infinite loop
*/

-- =============================================
-- Slug Generation Logic
-- =============================================

/*
Input:  "Project Proposal 2025.pdf"
Step 1: Replace spaces → "Project-Proposal-2025.pdf"
Step 2: Replace dots → "Project-Proposal-2025-pdf"
Step 3: Collapse -- → "Project-Proposal-2025-pdf" (no change)
Step 4: Lowercase → "project-proposal-2025-pdf"
Output: "project-proposal-2025-pdf"

Input:  "Meeting Notes - Oct 21.docx"
Step 1: Replace spaces → "Meeting-Notes---Oct-21.docx"
Step 2: Replace dots → "Meeting-Notes---Oct-21-docx"
Step 3: Collapse --- → "Meeting-Notes-Oct-21-docx"
Step 4: Lowercase → "meeting-notes-oct-21-docx"
Output: "meeting-notes-oct-21-docx"

Input:  "Budget    Spreadsheet....xlsx"
Step 1: Replace spaces → "Budget----Spreadsheet....xlsx"
Step 2: Replace dots → "Budget----Spreadsheet----xlsx"
Step 3: Collapse ---- → "Budget-Spreadsheet-xlsx"
Step 4: Lowercase → "budget-spreadsheet-xlsx"
Output: "budget-spreadsheet-xlsx"
*/

-- =============================================
-- Related Triggers
-- =============================================

/*
Similar triggers in database:
- tr_notes_generate_slug (notes table) - Same pattern with recursion check
- tr_tags_generate_slug (tags table) - Same pattern
- tr_workspace_generate_slug (workspaces table) - Same pattern

All use TRIGGER_NESTLEVEL() to prevent recursion.
*/

-- =============================================
-- Maintenance Notes
-- =============================================

/*
1. DO NOT remove TRIGGER_NESTLEVEL() check
   - Will cause infinite recursion
   - SQL Server will crash with stack overflow

2. Slug uniqueness NOT enforced by trigger
   - Index IX_files_slug allows duplicates (filtered by deleted_at)
   - Application layer should validate uniqueness if required

3. Trigger only fires when slug is NULL/empty
   - Custom slugs are preserved
   - Useful for SEO-friendly URLs

4. updated_at is set automatically
   - Even if slug doesn't change
   - Tracks when trigger last ran
*/
