USE [SuperApp-dev]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- sp_DeleteWorkspace - Delete workspace with cascade to all items
-- Author: SuperApp Team
-- Created: 2025-12-07
-- Description: 
--   Deletes workspace(s) and ALL related items (folders, notes, files, workspace_items)
--   UNIFORMLY treats all tables: either ALL soft delete OR ALL hard delete
-- =============================================
CREATE OR ALTER PROCEDURE [ws].[sp_DeleteWorkspace]
    @iv_workspace_ids    NVARCHAR(MAX),   -- Comma-separated workspace IDs (e.g., "1,2,3")
    @iv_is_hardDelete    BIT = 0,         -- 0 = soft delete ALL, 1 = hard delete ALL

    @ov_deleted_count    INT OUTPUT       -- Total workspaces deleted
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        ------------------------------------------------------------
        -- Parse workspace IDs from comma-separated string
        ------------------------------------------------------------
        CREATE TABLE #WorkspacesToDelete (
            workspace_id INT PRIMARY KEY
        );

        INSERT INTO #WorkspacesToDelete (workspace_id)
        SELECT CAST(LTRIM(RTRIM(value)) AS INT)
        FROM STRING_SPLIT(@iv_workspace_ids, ',')
        WHERE LTRIM(RTRIM(value)) <> '';

        ------------------------------------------------------------
        -- Collect ALL items in these workspaces (with hierarchy)
        ------------------------------------------------------------
        CREATE TABLE #AllItemsToDelete (
            item_type TINYINT,
            item_id INT,
            PRIMARY KEY (item_type, item_id)
        );

        -- Get all items from workspace_items (including root and children)
        INSERT INTO #AllItemsToDelete (item_type, item_id)
        SELECT DISTINCT wi.item_type, wi.item_id
        FROM ws.workspace_items wi
        INNER JOIN #WorkspacesToDelete w
            ON w.workspace_id = wi.workspace_id;

        -- Recursively collect all children (folders can have nested children)
        DECLARE @added INT = 1;

        WHILE @added > 0
        BEGIN
            INSERT INTO #AllItemsToDelete (item_type, item_id)
            SELECT DISTINCT wi.item_type, wi.item_id
            FROM ws.workspace_items wi
            INNER JOIN #AllItemsToDelete p
                ON p.item_type = 2  -- Folders (type=2) can have children
               AND p.item_id   = wi.parent_id
            WHERE NOT EXISTS (
                    SELECT 1 FROM #AllItemsToDelete 
                    WHERE item_type = wi.item_type AND item_id = wi.item_id
              );

            SET @added = @@ROWCOUNT;
        END

        ------------------------------------------------------------
        -- DELETE workspace_items mapping (ALWAYS hard delete)
        ------------------------------------------------------------
        DELETE wi
        FROM ws.workspace_items wi
        INNER JOIN #WorkspacesToDelete w
            ON w.workspace_id = wi.workspace_id;

        ------------------------------------------------------------
        -- DELETE or SOFT DELETE: FOLDERS (type = 2)
        ------------------------------------------------------------
        IF @iv_is_hardDelete = 1
        BEGIN
            -- Hard delete folders
            DELETE f
            FROM ws.folders f
            INNER JOIN #AllItemsToDelete d
                ON d.item_type = 2 AND d.item_id = f.id;
        END
        ELSE
        BEGIN
            -- Soft delete folders (set deleted_at)
            UPDATE f
            SET deleted_at = GETUTCDATE()
            FROM ws.folders f
            INNER JOIN #AllItemsToDelete d
                ON d.item_type = 2 AND d.item_id = f.id
            WHERE f.deleted_at IS NULL;  -- Only update non-deleted folders
        END

        ------------------------------------------------------------
        -- DELETE or SOFT DELETE: NOTES (type = 3)
        ------------------------------------------------------------
        IF @iv_is_hardDelete = 1
        BEGIN
            -- Hard delete notes
            DELETE n
            FROM dbo.notes n
            INNER JOIN #AllItemsToDelete d
                ON d.item_type = 3 AND d.item_id = n.id;
        END
        ELSE
        BEGIN
            -- Soft delete notes (set deleted_at)
            UPDATE n
            SET deleted_at = GETUTCDATE()
            FROM dbo.notes n
            INNER JOIN #AllItemsToDelete d
                ON d.item_type = 3 AND d.item_id = n.id
            WHERE n.deleted_at IS NULL;  -- Only update non-deleted notes
        END

        ------------------------------------------------------------
        -- DELETE or SOFT DELETE: FILES (type = 4)
        ------------------------------------------------------------
        IF @iv_is_hardDelete = 1
        BEGIN
            -- Hard delete files
            DELETE f
            FROM ws.files f
            INNER JOIN #AllItemsToDelete d
                ON d.item_type = 4 AND d.item_id = f.id;
        END
        ELSE
        BEGIN
            -- Soft delete files (set deleted_at)
            UPDATE f
            SET deleted_at = GETUTCDATE()
            FROM ws.files f
            INNER JOIN #AllItemsToDelete d
                ON d.item_type = 4 AND d.item_id = f.id
            WHERE f.deleted_at IS NULL;  -- Only update non-deleted files
        END

        ------------------------------------------------------------
        -- DELETE or SOFT DELETE: WORKSPACES (ws.workspaces)
        ------------------------------------------------------------
        IF @iv_is_hardDelete = 1
        BEGIN
            -- Hard delete workspaces
            DELETE w
            FROM ws.workspaces w
            INNER JOIN #WorkspacesToDelete d
                ON d.workspace_id = w.id;
        END
        ELSE
        BEGIN
            -- Soft delete workspaces (set deleted_at)
            UPDATE w
            SET deleted_at = GETUTCDATE()
            FROM ws.workspaces w
            INNER JOIN #WorkspacesToDelete d
                ON d.workspace_id = w.id
            WHERE w.deleted_at IS NULL;  -- Only update non-deleted workspaces
        END

        ------------------------------------------------------------
        -- OUTPUT count
        ------------------------------------------------------------
        SELECT @ov_deleted_count = COUNT(*) FROM #WorkspacesToDelete;

        ------------------------------------------------------------
        COMMIT TRANSACTION;

        DROP TABLE #WorkspacesToDelete;
        DROP TABLE #AllItemsToDelete;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;

        DECLARE @msg NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @sev INT = ERROR_SEVERITY();
        DECLARE @st INT = ERROR_STATE();

        RAISERROR(@msg, @sev, @st);
    END CATCH
END
GO
