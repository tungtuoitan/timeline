-- =============================================
-- MIGRATION: Tags → Folders + Share/Fork Support
-- Description: Migrate from tags to folders with materialized path and share/fork capabilities
-- Author: Claude Code
-- Date: 2025-01-29
-- =============================================

-- IMPORTANT: Run this migration during maintenance window (downtime required)
-- Estimated time: 5-10 minutes for small DB, 30-60 minutes for large DB

PRINT '========================================';
PRINT 'MIGRATION START: Tags → Folders + Share/Fork';
PRINT 'Time: ' + CONVERT(VARCHAR, GETUTCDATE(), 120);
PRINT '========================================';
GO

-- =============================================
-- PHASE 1: BACKUP
-- =============================================
PRINT '';
PRINT 'PHASE 1: Creating backup...';

-- Recommend: Backup database before running this script
-- BACKUP DATABASE SuperApp TO DISK = 'C:\backup\superapp_before_folders_migration.bak';

GO

-- =============================================
-- PHASE 2: DROP DEPENDENT OBJECTS
-- =============================================
PRINT '';
PRINT 'PHASE 2: Dropping dependent objects...';

-- Drop triggers
PRINT '  Dropping triggers...';
DROP TRIGGER IF EXISTS tr_tags_generate_slug;
DROP TRIGGER IF EXISTS tr_workspace_items_update_stats;
DROP TRIGGER IF EXISTS tr_workspace_items_update_depth;
GO

-- Drop stored procedures (if using procedures)
PRINT '  Dropping stored procedures...';
DROP PROCEDURE IF EXISTS usp_i_tag;
DROP PROCEDURE IF EXISTS usp_s_tag;
DROP PROCEDURE IF EXISTS usp_u_tag;
DROP PROCEDURE IF EXISTS usp_d_tag;
DROP PROCEDURE IF EXISTS usp_s_user_tags;
DROP PROCEDURE IF EXISTS usp_s_tag_tree;
DROP PROCEDURE IF EXISTS usp_add_item_to_workspace;
DROP PROCEDURE IF EXISTS usp_s_workspace_items;
DROP PROCEDURE IF EXISTS usp_move_item;
DROP PROCEDURE IF EXISTS usp_remove_item;
DROP PROCEDURE IF EXISTS usp_s_item_path;
GO

-- Drop foreign key constraints that reference tags table
PRINT '  Dropping foreign key constraints...';
ALTER TABLE workspace_items DROP CONSTRAINT IF EXISTS FK_workspace_items_parent_tag;
ALTER TABLE workspace_items DROP CONSTRAINT IF EXISTS FK_workspace_items_type;
GO

-- Drop indexes on workspace_items (will recreate later)
PRINT '  Dropping indexes...';
DROP INDEX IF EXISTS IX_workspace_items_workspace ON workspace_items;
DROP INDEX IF EXISTS IX_workspace_items_parent ON workspace_items;
DROP INDEX IF EXISTS IX_workspace_items_child ON workspace_items;
DROP INDEX IF EXISTS IX_workspace_items_path ON workspace_items;
DROP INDEX IF EXISTS IX_workspace_items_workspace_parent ON workspace_items;
DROP INDEX IF EXISTS UQ_workspace_items_unique ON workspace_items;
GO

-- =============================================
-- PHASE 3: RENAME TAGS → FOLDERS
-- =============================================
PRINT '';
PRINT 'PHASE 3: Renaming tags to folders...';

-- Rename table
PRINT '  Renaming table: tags → folders...';
EXEC sp_rename 'tags', 'folders';
GO

-- Rename columns
PRINT '  Renaming columns...';
EXEC sp_rename 'folders.tag_id', 'folder_id', 'COLUMN';
GO

-- Rename indexes
PRINT '  Renaming indexes...';
EXEC sp_rename 'IX_tags_user', 'IX_folders_user', 'INDEX';
EXEC sp_rename 'IX_tags_name', 'IX_folders_name', 'INDEX';
EXEC sp_rename 'UQ_tags_user_slug', 'UQ_folders_user_slug', 'INDEX';
EXEC sp_rename 'IX_tags_usage_count', 'IX_folders_usage', 'INDEX';
GO

-- Drop indexes that don't exist in new schema
DROP INDEX IF EXISTS IX_tags_created_at ON folders;
GO

-- Rename primary key constraint
DECLARE @pk_name NVARCHAR(200);
SELECT @pk_name = name
FROM sys.key_constraints
WHERE parent_object_id = OBJECT_ID('folders') AND type = 'PK';

IF @pk_name IS NOT NULL
BEGIN
    PRINT '  Renaming primary key: ' + @pk_name + ' → PK_folders...';
    EXEC sp_rename @pk_name, 'PK_folders', 'OBJECT';
END;
GO

-- Rename foreign key
DECLARE @fk_name NVARCHAR(200);
SELECT @fk_name = name
FROM sys.foreign_keys
WHERE parent_object_id = OBJECT_ID('folders') AND referenced_object_id = OBJECT_ID('users');

IF @fk_name IS NOT NULL
BEGIN
    PRINT '  Renaming foreign key: ' + @fk_name + ' → FK_folders_user...';
    EXEC sp_rename @fk_name, 'FK_folders_user', 'OBJECT';
END;
GO

-- =============================================
-- PHASE 4: UPDATE WORKSPACE_ITEMS
-- =============================================
PRINT '';
PRINT 'PHASE 4: Updating workspace_items table...';

-- Rename column: parent_tag_id → parent_folder_id
PRINT '  Renaming parent_tag_id → parent_folder_id...';
EXEC sp_rename 'workspace_items.parent_tag_id', 'parent_folder_id', 'COLUMN';
GO

-- Add new columns for SHARE/FORK support
PRINT '  Adding link_type column...';
ALTER TABLE workspace_items
ADD link_type NVARCHAR(20) NOT NULL DEFAULT 'owned'
    CONSTRAINT CK_workspace_items_link_type CHECK (link_type IN ('owned', 'shared'));
GO

-- Update all existing items to 'owned' (already done by DEFAULT, but explicit for clarity)
UPDATE workspace_items SET link_type = 'owned' WHERE link_type IS NULL;
GO

-- Add materialized path columns (may already exist)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('workspace_items') AND name = 'item_path')
BEGIN
    PRINT '  Adding item_path column...';
    ALTER TABLE workspace_items
    ADD item_path NVARCHAR(4000) NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('workspace_items') AND name = 'depth')
BEGIN
    PRINT '  Adding depth column...';
    ALTER TABLE workspace_items
    ADD depth INT NOT NULL DEFAULT 0
        CONSTRAINT CK_workspace_items_depth CHECK (depth >= 0 AND depth < 50);
END;
GO

-- Drop unnecessary columns (simplified design)
PRINT '  Dropping unnecessary columns...';
ALTER TABLE workspace_items DROP COLUMN IF EXISTS relationship_type;
ALTER TABLE workspace_items DROP COLUMN IF EXISTS label;
ALTER TABLE workspace_items DROP COLUMN IF EXISTS notes;
ALTER TABLE workspace_items DROP COLUMN IF EXISTS color;
ALTER TABLE workspace_items DROP COLUMN IF EXISTS icon;
GO

-- =============================================
-- PHASE 5: UPDATE ENTITY_TYPES
-- =============================================
PRINT '';
PRINT 'PHASE 5: Updating entity_types...';

-- Update 'tag' → 'folder'
UPDATE entity_types
SET type_name = 'folder', display_name = 'Folder', description = 'Container for organizing items'
WHERE type_name = 'tag';
GO

-- Update workspace_items.child_type references
UPDATE workspace_items
SET child_type = 'folder'
WHERE child_type = 'tag';
GO

-- Add 'file' type (for future)
IF NOT EXISTS (SELECT 1 FROM entity_types WHERE type_name = 'file')
BEGIN
    INSERT INTO entity_types (type_name, display_name, description)
    VALUES ('file', 'File', 'Uploaded file (future support)');
END;
GO

-- =============================================
-- PHASE 6: CREATE NEW TABLES
-- =============================================
PRINT '';
PRINT 'PHASE 6: Creating new tables...';

-- Create tags table (NEW - Hashtags)
PRINT '  Creating tags table (hashtags)...';
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'tags_new')
BEGIN
    CREATE TABLE tags_new (
        tag_id INT IDENTITY(1,1) PRIMARY KEY,
        user_id INT NOT NULL,
        name NVARCHAR(100) NOT NULL,
        slug NVARCHAR(100) NOT NULL,
        color NVARCHAR(7) NULL DEFAULT '#6B7280',
        usage_count INT NOT NULL DEFAULT 0,
        created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        updated_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        deleted_at DATETIME2 NULL,

        CONSTRAINT FK_tags_new_user FOREIGN KEY (user_id) REFERENCES users(user_id),
        CONSTRAINT UQ_tags_new_user_slug UNIQUE (user_id, slug)
    );

    CREATE INDEX IX_tags_new_user ON tags_new(user_id, deleted_at) WHERE deleted_at IS NULL;
    CREATE INDEX IX_tags_new_usage ON tags_new(user_id, usage_count DESC) WHERE deleted_at IS NULL;

    PRINT '    Tags table created successfully.';
END;
GO

-- Create entity_tags table (Junction table)
PRINT '  Creating entity_tags table...';
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'entity_tags')
BEGIN
    CREATE TABLE entity_tags (
        entity_tag_id BIGINT IDENTITY(1,1) PRIMARY KEY,
        tag_id INT NOT NULL,
        entity_type NVARCHAR(50) NOT NULL,
        entity_id INT NOT NULL,
        tagged_by INT NOT NULL,
        created_at DATETIME2 DEFAULT GETUTCDATE(),
        deleted_at DATETIME2 NULL,

        CONSTRAINT FK_entity_tags_tag FOREIGN KEY (tag_id) REFERENCES tags_new(tag_id) ON DELETE CASCADE,
        CONSTRAINT FK_entity_tags_entity_type FOREIGN KEY (entity_type) REFERENCES entity_types(type_name),
        CONSTRAINT FK_entity_tags_user FOREIGN KEY (tagged_by) REFERENCES users(user_id),
        CONSTRAINT UQ_entity_tags_unique UNIQUE (tag_id, entity_type, entity_id)
    );

    CREATE INDEX IX_entity_tags_tag ON entity_tags(tag_id, deleted_at) WHERE deleted_at IS NULL;
    CREATE INDEX IX_entity_tags_entity ON entity_tags(entity_type, entity_id, deleted_at)
        INCLUDE (tag_id) WHERE deleted_at IS NULL;

    PRINT '    Entity_tags table created successfully.';
END;
GO

-- =============================================
-- PHASE 7: RECREATE CONSTRAINTS & INDEXES
-- =============================================
PRINT '';
PRINT 'PHASE 7: Recreating constraints and indexes...';

-- Recreate foreign keys
PRINT '  Creating foreign keys...';
ALTER TABLE workspace_items
ADD CONSTRAINT FK_workspace_items_parent_folder
    FOREIGN KEY (parent_folder_id) REFERENCES folders(folder_id);

ALTER TABLE workspace_items
ADD CONSTRAINT FK_workspace_items_type
    FOREIGN KEY (child_type) REFERENCES entity_types(type_name);
GO

-- Recreate indexes (optimized with INCLUDE)
PRINT '  Creating optimized indexes...';

CREATE NONCLUSTERED INDEX IX_workspace_items_workspace
    ON workspace_items(workspace_id, deleted_at)
    INCLUDE (parent_folder_id, child_type, child_id, link_type, item_path, depth, sort_order)
    WHERE deleted_at IS NULL;

CREATE NONCLUSTERED INDEX IX_workspace_items_parent
    ON workspace_items(workspace_id, parent_folder_id, deleted_at)
    INCLUDE (child_type, child_id, sort_order, depth)
    WHERE deleted_at IS NULL;

CREATE NONCLUSTERED INDEX IX_workspace_items_child
    ON workspace_items(child_type, child_id, deleted_at)
    INCLUDE (workspace_id, link_type)
    WHERE deleted_at IS NULL;

CREATE NONCLUSTERED INDEX IX_workspace_items_path
    ON workspace_items(workspace_id, item_path, deleted_at)
    INCLUDE (child_type, child_id, depth, sort_order)
    WHERE deleted_at IS NULL AND item_path IS NOT NULL;

CREATE NONCLUSTERED INDEX IX_workspace_items_shared
    ON workspace_items(workspace_id, link_type, deleted_at)
    INCLUDE (child_type, child_id)
    WHERE deleted_at IS NULL AND link_type = 'shared';

CREATE NONCLUSTERED INDEX IX_workspace_items_roots
    ON workspace_items(workspace_id, depth, deleted_at)
    WHERE depth = 0 AND deleted_at IS NULL;

-- Unique constraint
CREATE UNIQUE NONCLUSTERED INDEX UQ_workspace_items_location
    ON workspace_items(workspace_id, parent_folder_id, child_type, child_id)
    WHERE deleted_at IS NULL;

PRINT '  Indexes created successfully.';
GO

-- =============================================
-- PHASE 8: CREATE TRIGGERS
-- =============================================
PRINT '';
PRINT 'PHASE 8: Creating triggers...';

-- Trigger 1: Auto-update materialized path
PRINT '  Creating tr_workspace_items_update_path...';
GO
CREATE OR ALTER TRIGGER tr_workspace_items_update_path
ON workspace_items
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Only update if parent_folder_id changed or new insert
    IF UPDATE(parent_folder_id) OR UPDATE(workspace_id) OR NOT UPDATE(item_id)
    BEGIN
        -- Update path and depth for inserted/updated items
        UPDATE wi
        SET
            item_path = CASE
                WHEN wi.parent_folder_id IS NULL THEN
                    '/' + CAST(wi.item_id AS NVARCHAR(10)) + '/'
                ELSE
                    ISNULL(parent_wi.item_path, '/') + CAST(wi.item_id AS NVARCHAR(10)) + '/'
            END,
            depth = CASE
                WHEN wi.parent_folder_id IS NULL THEN 0
                ELSE ISNULL(parent_wi.depth, 0) + 1
            END,
            updated_at = GETUTCDATE()
        FROM workspace_items wi
        INNER JOIN inserted i ON wi.item_id = i.item_id
        LEFT JOIN workspace_items parent_wi
            ON wi.parent_folder_id = parent_wi.child_id
            AND parent_wi.workspace_id = wi.workspace_id
            AND parent_wi.child_type = 'folder'
            AND parent_wi.deleted_at IS NULL;

        -- Recursive update: Update children paths
        DECLARE @affected_items TABLE (item_id BIGINT);
        INSERT INTO @affected_items SELECT item_id FROM inserted;

        DECLARE @level INT = 0;
        DECLARE @rows_affected INT = 1;

        WHILE @rows_affected > 0 AND @level < 50
        BEGIN
            UPDATE wi
            SET
                item_path = parent_wi.item_path + CAST(wi.item_id AS NVARCHAR(10)) + '/',
                depth = parent_wi.depth + 1,
                updated_at = GETUTCDATE()
            FROM workspace_items wi
            INNER JOIN workspace_items parent_wi
                ON wi.parent_folder_id = parent_wi.child_id
                AND wi.workspace_id = parent_wi.workspace_id
                AND parent_wi.child_type = 'folder'
            INNER JOIN @affected_items ai ON parent_wi.item_id = ai.item_id
            WHERE wi.deleted_at IS NULL;

            SET @rows_affected = @@ROWCOUNT;

            DELETE FROM @affected_items;
            INSERT INTO @affected_items
            SELECT wi.item_id
            FROM workspace_items wi
            INNER JOIN workspace_items parent_wi
                ON wi.parent_folder_id = parent_wi.child_id
                AND wi.workspace_id = parent_wi.workspace_id
            WHERE parent_wi.item_id IN (SELECT item_id FROM @affected_items);

            SET @level = @level + 1;
        END;
    END;
END;
GO

-- Trigger 2: Update stats
PRINT '  Creating tr_workspace_items_update_stats...';
GO
CREATE OR ALTER TRIGGER tr_workspace_items_update_stats
ON workspace_items
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    -- Update workspace stats
    UPDATE w
    SET
        tag_count = COALESCE((
            SELECT COUNT(DISTINCT child_id)
            FROM workspace_items
            WHERE workspace_id = w.workspace_id
            AND deleted_at IS NULL
            AND child_type = 'folder'
        ), 0),
        relationship_count = COALESCE((
            SELECT COUNT(*)
            FROM workspace_items
            WHERE workspace_id = w.workspace_id
            AND deleted_at IS NULL
        ), 0),
        updated_at = GETUTCDATE()
    FROM workspaces w
    WHERE w.workspace_id IN (
        SELECT DISTINCT workspace_id FROM inserted
        UNION
        SELECT DISTINCT workspace_id FROM deleted
    );

    -- Update folders.usage_count
    UPDATE f
    SET usage_count = COALESCE((
        SELECT COUNT(DISTINCT workspace_id)
        FROM workspace_items wi
        WHERE wi.child_type = 'folder'
        AND wi.child_id = f.folder_id
        AND wi.deleted_at IS NULL
    ), 0)
    FROM folders f
    WHERE f.folder_id IN (
        SELECT DISTINCT child_id FROM inserted WHERE child_type = 'folder'
        UNION
        SELECT DISTINCT child_id FROM deleted WHERE child_type = 'folder'
    );
END;
GO

PRINT '  Triggers created successfully.';

-- =============================================
-- PHASE 9: INITIALIZE PATHS FOR EXISTING DATA
-- =============================================
PRINT '';
PRINT 'PHASE 9: Initializing paths for existing data...';

-- Update root-level items first (depth = 0)
UPDATE workspace_items
SET
    item_path = '/' + CAST(item_id AS NVARCHAR(10)) + '/',
    depth = 0
WHERE parent_folder_id IS NULL AND item_path IS NULL;

PRINT '  Root items initialized: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));

-- Update children recursively (trigger will handle this, but we can do manual update for speed)
DECLARE @current_depth INT = 0;
DECLARE @updated_count INT = 1;

WHILE @updated_count > 0 AND @current_depth < 50
BEGIN
    UPDATE wi
    SET
        item_path = parent_wi.item_path + CAST(wi.item_id AS NVARCHAR(10)) + '/',
        depth = parent_wi.depth + 1
    FROM workspace_items wi
    INNER JOIN workspace_items parent_wi
        ON wi.parent_folder_id = parent_wi.child_id
        AND wi.workspace_id = parent_wi.workspace_id
        AND parent_wi.child_type = 'folder'
    WHERE wi.item_path IS NULL
    AND parent_wi.item_path IS NOT NULL
    AND wi.deleted_at IS NULL;

    SET @updated_count = @@ROWCOUNT;
    SET @current_depth = @current_depth + 1;

    PRINT '    Level ' + CAST(@current_depth AS NVARCHAR(10)) + ': ' + CAST(@updated_count AS NVARCHAR(10)) + ' items updated';
END;

PRINT '  Path initialization complete.';
GO

-- =============================================
-- PHASE 10: DROP OLD TABLE (workspace_relationship_types)
-- =============================================
PRINT '';
PRINT 'PHASE 10: Dropping workspace_relationship_types...';

-- This table was not used effectively, drop it
DROP TABLE IF EXISTS workspace_relationship_types;
PRINT '  Table dropped successfully.';
GO

-- =============================================
-- PHASE 11: VALIDATION
-- =============================================
PRINT '';
PRINT 'PHASE 11: Validating migration...';

DECLARE @folders_count INT, @workspace_items_count INT, @items_without_path INT;

SELECT @folders_count = COUNT(*) FROM folders WHERE deleted_at IS NULL;
SELECT @workspace_items_count = COUNT(*) FROM workspace_items WHERE deleted_at IS NULL;
SELECT @items_without_path = COUNT(*) FROM workspace_items WHERE deleted_at IS NULL AND item_path IS NULL;

PRINT '  Folders count: ' + CAST(@folders_count AS NVARCHAR(10));
PRINT '  Workspace items count: ' + CAST(@workspace_items_count AS NVARCHAR(10));
PRINT '  Items without path: ' + CAST(@items_without_path AS NVARCHAR(10));

IF @items_without_path > 0
BEGIN
    PRINT '';
    PRINT '  ⚠️ WARNING: ' + CAST(@items_without_path AS NVARCHAR(10)) + ' items have NULL path. Check data integrity!';
END
ELSE
BEGIN
    PRINT '  ✅ All items have valid paths!';
END;

-- =============================================
-- MIGRATION COMPLETE
-- =============================================
PRINT '';
PRINT '========================================';
PRINT 'MIGRATION COMPLETE!';
PRINT 'Time: ' + CONVERT(VARCHAR, GETUTCDATE(), 120);
PRINT '========================================';
PRINT '';
PRINT 'NEXT STEPS:';
PRINT '1. Rename tags_new → tags (manual step after testing)';
PRINT '2. Update C# code to use Folder entities';
PRINT '3. Test SHARE and FORK functionality';
PRINT '4. Update frontend to use new API';
PRINT '';
GO
