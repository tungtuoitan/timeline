-- =============================================
-- Stored Procedure: sp_DeleteWorkspaceItems
-- Description: Deletes multiple workspace items (folders/notes/files) with cascade support
-- Created: 2025-11-30
-- =============================================

USE [SuperApp-dev];
GO

IF OBJECT_ID('[ws].[sp_DeleteWorkspaceItems]', 'P') IS NOT NULL
    DROP PROCEDURE [ws].[sp_DeleteWorkspaceItems];
GO

CREATE PROCEDURE [ws].[sp_DeleteWorkspaceItems]
    @WorkspaceId INT,
    @Items NVARCHAR(MAX) -- JSON array of objects: [{"type": 2, "id": 10}, {"type": 3, "id": 25}]
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Parse JSON input into temp table
        SELECT
            CAST(JSON_VALUE(value, '$.type') AS TINYINT) AS item_type,
            CAST(JSON_VALUE(value, '$.id') AS INT) AS item_id
        INTO #ItemsToDelete
        FROM OPENJSON(@Items);

        -- Validate items exist and belong to workspace
        IF EXISTS (
            SELECT 1 
            FROM #ItemsToDelete itd
            LEFT JOIN [ws].[workspace_items] wi ON wi.item_type = itd.item_type AND wi.item_id = itd.item_id
            WHERE wi.workspace_id IS NULL OR wi.workspace_id != @WorkspaceId
        )
        BEGIN
            RAISERROR('One or more items not found or do not belong to the specified workspace', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END

        -- Create temp table for all items to delete (including children)
        CREATE TABLE #AllItemsToDelete (
            item_type TINYINT,
            item_id INT,
            PRIMARY KEY (item_type, item_id)
        );

        -- Insert root items
        INSERT INTO #AllItemsToDelete (item_type, item_id)
        SELECT item_type, item_id FROM #ItemsToDelete;

        -- Recursively get all children (folders can have folders, notes, files as children)
        DECLARE @AddedRows INT = 1;
        WHILE @AddedRows > 0
        BEGIN
            INSERT INTO #AllItemsToDelete (item_type, item_id)
            SELECT DISTINCT wi.item_type, wi.item_id
            FROM [ws].[workspace_items] wi
            INNER JOIN #AllItemsToDelete parent ON parent.item_id = wi.parent_id AND parent.item_type = 2
            WHERE wi.workspace_id = @WorkspaceId
            AND NOT EXISTS (
                SELECT 1 FROM #AllItemsToDelete WHERE item_type = wi.item_type AND item_id = wi.item_id
            );

            SET @AddedRows = @@ROWCOUNT;
        END

        -- Delete workspace_items entries
        DELETE wi
        FROM [ws].[workspace_items] wi
        INNER JOIN #AllItemsToDelete del ON del.item_type = wi.item_type AND del.item_id = wi.item_id
        WHERE wi.workspace_id = @WorkspaceId;

        -- Delete actual entities
        -- Delete folders (item_type = 2)
        DELETE f
        FROM [ws].[folders] f
        INNER JOIN #AllItemsToDelete del ON del.item_type = 2 AND del.item_id = f.id;

        -- Delete notes (item_type = 3)
        DELETE n
        FROM [dbo].[notes] n
        INNER JOIN #AllItemsToDelete del ON del.item_type = 3 AND del.item_id = n.id;

        -- Delete files (item_type = 4) - if files table exists
        -- DELETE f
        -- FROM [ws].[files] f
        -- INNER JOIN #AllItemsToDelete del ON del.item_type = 4 AND del.item_id = f.file_id;

        COMMIT TRANSACTION;

        -- Return count of deleted items
        SELECT COUNT(*) AS DeletedCount FROM #AllItemsToDelete;

        -- Cleanup
        DROP TABLE #ItemsToDelete;
        DROP TABLE #AllItemsToDelete;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        -- Re-throw error
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();

        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END
GO

-- Grant execute permission
GRANT EXECUTE ON [ws].[sp_DeleteWorkspaceItems] TO [public];
GO

PRINT 'Stored Procedure [ws].[sp_DeleteWorkspaceItems] created successfully';
GO
