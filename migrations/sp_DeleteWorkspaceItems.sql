USE [SuperApp-dev]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [ws].[sp_DeleteWorkspaceItems]
    @iv_workspace_id     INT,
    @iv_items            NVARCHAR(MAX),   -- JSON input
    @iv_is_hardDelete    BIT = 0,         -- 0 = soft delete, 1 = hard delete

    @ov_deleted_count    INT OUTPUT       -- output
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        ------------------------------------------------------------
        -- Parse JSON
        ------------------------------------------------------------
        SELECT
            CAST(JSON_VALUE(value, '$.type') AS TINYINT) AS item_type,
            CAST(JSON_VALUE(value, '$.id') AS INT) AS item_id
        INTO #ItemsToDelete
        FROM OPENJSON(@iv_items);

        ------------------------------------------------------------
        -- Validate ownership
        ------------------------------------------------------------
        IF EXISTS (
            SELECT 1
            FROM #ItemsToDelete itd
            LEFT JOIN ws.workspace_items wi
                ON wi.item_type = itd.item_type
               AND wi.item_id   = itd.item_id
            WHERE wi.workspace_id IS NULL OR wi.workspace_id != @iv_workspace_id
        )
        BEGIN
            RAISERROR('Item not found or does not belong to workspace', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END

        ------------------------------------------------------------
        -- Collect all children recursively
        ------------------------------------------------------------
        CREATE TABLE #AllItemsToDelete (
            item_type TINYINT,
            item_id INT,
            PRIMARY KEY (item_type, item_id)
        );

        INSERT INTO #AllItemsToDelete
        SELECT item_type, item_id FROM #ItemsToDelete;

        DECLARE @added INT = 1;

        WHILE @added > 0
        BEGIN
            INSERT INTO #AllItemsToDelete (item_type, item_id)
            SELECT DISTINCT wi.item_type, wi.item_id
            FROM ws.workspace_items wi
            INNER JOIN #AllItemsToDelete p
                ON p.item_type = 2 
               AND p.item_id   = wi.parent_id
            WHERE wi.workspace_id = @iv_workspace_id
              AND NOT EXISTS (
                    SELECT 1 FROM #AllItemsToDelete 
                    WHERE item_type = wi.item_type AND item_id = wi.item_id
              );

            SET @added = @@ROWCOUNT;
        END

        ------------------------------------------------------------
        -- ALWAYS hard delete workspace_items mapping
        ------------------------------------------------------------
        DELETE wi
        FROM ws.workspace_items wi
        INNER JOIN #AllItemsToDelete d
            ON d.item_type = wi.item_type
           AND d.item_id   = wi.item_id
        WHERE wi.workspace_id = @iv_workspace_id;

        ------------------------------------------------------------
        -- FOLDERS (type = 2) ALWAYS HARD DELETE
        ------------------------------------------------------------
        DELETE f
        FROM ws.folders f
        INNER JOIN #AllItemsToDelete d
            ON d.item_type = 2 AND d.item_id = f.id;

        ------------------------------------------------------------
        -- NOTES (type = 3)
        ------------------------------------------------------------
        IF @iv_is_hardDelete = 1
        BEGIN
            DELETE n
            FROM dbo.notes n
            INNER JOIN #AllItemsToDelete d
                ON d.item_type = 3 AND d.item_id = n.id;
        END
        ELSE
        BEGIN
            UPDATE n
            SET deleted_at = GETUTCDATE()
            FROM dbo.notes n
            INNER JOIN #AllItemsToDelete d
                ON d.item_type = 3 AND d.item_id = n.id;
        END

        ------------------------------------------------------------
        -- FILES (type = 4)
        ------------------------------------------------------------
        IF @iv_is_hardDelete = 1
        BEGIN
            DELETE f
            FROM ws.files f
            INNER JOIN #AllItemsToDelete d
                ON d.item_type = 4 AND d.item_id = f.id;
        END
        ELSE
        BEGIN
            UPDATE f
            SET deleted_at = GETUTCDATE()
            FROM ws.files f
            INNER JOIN #AllItemsToDelete d
                ON d.item_type = 4 AND d.item_id = f.id;
        END

        ------------------------------------------------------------
        -- OUTPUT count
        ------------------------------------------------------------
        SELECT @ov_deleted_count = COUNT(*) FROM #AllItemsToDelete;

        ------------------------------------------------------------
        COMMIT TRANSACTION;

        DROP TABLE #ItemsToDelete;
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
