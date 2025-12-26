USE [SuperApp-dev]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- sp_DeleteWorkspace - Hard delete workspace with cascade
-- Author: SuperApp Team
-- Created: 2025-12-07
-- Modified: 2025-12-24 - Removed soft delete support (use Upsert API for soft delete)
--                        Only delete workspace + folders + workspace_items
-- Description:
--   Hard deletes workspace(s), workspace_items mapping, and folders.
--   Notes and Files are NOT deleted to preserve data - they can be
--   accessed directly or re-added to other workspaces later.
--   For soft delete, use the Upsert API with deletedAt timestamp.
-- =============================================
CREATE OR ALTER PROCEDURE [ws].[sp_DeleteWorkspace]
    @iv_workspace_ids    NVARCHAR(MAX),   -- Comma-separated workspace IDs (e.g., "1,2,3")
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
        -- DELETE workspace_items mapping (hard delete)
        ------------------------------------------------------------
        DELETE wi
        FROM ws.workspace_items wi
        INNER JOIN #WorkspacesToDelete w
            ON w.workspace_id = wi.workspace_id;

        ------------------------------------------------------------
        -- HARD DELETE: FOLDERS (type = 2)
        -- Note: Only delete folders. Notes and Files are NOT deleted
        -- to preserve data. They can be accessed directly or re-added
        -- to other workspaces.
        ------------------------------------------------------------
        DELETE f
        FROM ws.folders f
        INNER JOIN #AllItemsToDelete d
            ON d.item_type = 2 AND d.item_id = f.id;

        ------------------------------------------------------------
        -- HARD DELETE: WORKSPACES (ws.workspaces)
        ------------------------------------------------------------
        DELETE w
        FROM ws.workspaces w
        INNER JOIN #WorkspacesToDelete d
            ON d.workspace_id = w.id;

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
