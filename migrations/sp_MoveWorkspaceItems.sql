-- =============================================
-- Stored Procedure: sp_MoveWorkspaceItems
-- Description: Moves workspace items (folders/notes/files) with cascade support for children
-- Version: 1.1
-- =============================================

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [ws].[sp_MoveWorkspaceItems]
    @SourceWorkspaceId INT,
    @Items NVARCHAR(MAX),           -- JSON: [{"type":2,"id":10}, ...]
    @TargetParentId INT = NULL,     -- NULL = move to root
    @TargetWorkspaceId INT = NULL   -- NULL = same workspace, value = move to other workspace
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;

        -- Parse JSON items
        DECLARE @ItemsTable TABLE (ItemType TINYINT, ItemId INT);

        INSERT INTO @ItemsTable (ItemType, ItemId)
        SELECT ItemType, ItemId
        FROM OPENJSON(@Items)
        WITH (
            ItemType TINYINT '$.type',
            ItemId INT '$.id'
        );

        -- Validate that items exist in source workspace
        DECLARE @InvalidCount INT;

        SELECT @InvalidCount = COUNT(*)
        FROM @ItemsTable it
        LEFT JOIN ws.workspace_items wi ON wi.workspace_id = @SourceWorkspaceId
            AND wi.item_type = it.ItemType
            AND wi.item_id = it.ItemId
            AND wi.deleted_at IS NULL
        WHERE wi.id IS NULL;

        IF @InvalidCount > 0
        BEGIN
            RAISERROR('One or more items not found in source workspace', 16, 1);
            RETURN;
        END;

        -- Validate target parent exists if provided
        IF @TargetParentId IS NOT NULL
        BEGIN
            IF NOT EXISTS (
                SELECT 1 
                FROM ws.folders 
                WHERE id = @TargetParentId 
                AND deleted_at IS NULL
            )
            BEGIN
                RAISERROR('Target parent folder not found or has been deleted', 16, 1);
                RETURN;
            END;

            -- If moving to different workspace, verify target parent is in target workspace
            IF @TargetWorkspaceId IS NOT NULL AND @TargetWorkspaceId != @SourceWorkspaceId
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 
                    FROM ws.workspace_items 
                    WHERE item_type = 2 
                    AND item_id = @TargetParentId 
                    AND workspace_id = @TargetWorkspaceId
                    AND deleted_at IS NULL
                )
                BEGIN
                    RAISERROR('Target parent folder does not exist in target workspace', 16, 1);
                    RETURN;
                END;
            END;
        END;

        -- Recursive CTE to get all items to move (including children)
        ;WITH ItemsToMove AS (
            -- Selected items (root level of selection)
            SELECT
                wi.id,
                wi.workspace_id,
                wi.item_type,
                wi.item_id,
                wi.parent_id,
                0 AS depth
            FROM ws.workspace_items wi
            INNER JOIN @ItemsTable it ON wi.item_type = it.ItemType AND wi.item_id = it.ItemId
            WHERE wi.workspace_id = @SourceWorkspaceId
                AND wi.deleted_at IS NULL

            UNION ALL

            -- Children (recursive) - only folders can have children
            SELECT
                wi.id,
                wi.workspace_id,
                wi.item_type,
                wi.item_id,
                wi.parent_id,
                parent.depth + 1
            FROM ws.workspace_items wi
            INNER JOIN ItemsToMove parent ON wi.parent_id = parent.item_id
                AND parent.item_type = 2  -- Only folders can be parents
            WHERE wi.workspace_id = @SourceWorkspaceId
                AND wi.deleted_at IS NULL
        ),
        -- Temporary table to store results for conditional logic
        AllItemsToMove AS (
            SELECT * FROM ItemsToMove
        )
        
        -- Store into temp table to use in IF statement
        SELECT * INTO #TempItemsToMove FROM AllItemsToMove;

        -- Determine if this is a same-workspace move or cross-workspace move
        DECLARE @ResultMessage NVARCHAR(500);
        DECLARE @ResultCount INT;

        IF @TargetWorkspaceId IS NULL OR @TargetWorkspaceId = @SourceWorkspaceId
        BEGIN
            -- Same workspace: Update parent_id (simple move)
            -- Only update the root selected items, children will maintain their hierarchy
            UPDATE wi
            SET
                parent_id = @TargetParentId,
                updated_at = GETUTCDATE()
            FROM ws.workspace_items wi
            INNER JOIN @ItemsTable it ON wi.item_type = it.ItemType AND wi.item_id = it.ItemId
            WHERE wi.workspace_id = @SourceWorkspaceId
                AND wi.deleted_at IS NULL;

            SET @ResultCount = @@ROWCOUNT;
            SET @ResultMessage = 'Items moved successfully within workspace';
        END
        ELSE
        BEGIN
            -- Cross-workspace: Delete from source, Create in target
            -- This is more complex as we need to maintain hierarchy

            -- Create a mapping table for old to new workspace_item IDs
            DECLARE @IdMapping TABLE (
                OldId BIGINT,
                NewId BIGINT,
                ItemType TINYINT,
                ItemId INT,
                OldParentId INT,
                Depth INT
            );

            -- Insert items in order of depth (parents first)
            DECLARE @CurrentDepth INT = 0;
            DECLARE @MaxDepth INT;

            SELECT @MaxDepth = MAX(depth) FROM #TempItemsToMove;

            WHILE @CurrentDepth <= @MaxDepth
            BEGIN
                -- Insert items at current depth level
                INSERT INTO ws.workspace_items (workspace_id, parent_id, item_type, item_id, created_at)
                OUTPUT inserted.id, @CurrentDepth, inserted.item_type, inserted.item_id INTO @IdMapping(NewId, Depth, ItemType, ItemId)
                SELECT
                    @TargetWorkspaceId,
                    CASE
                        -- Root selected items go to target folder
                        WHEN itm.depth = 0 THEN @TargetParentId
                        -- Children maintain their parent relationship (mapped to new parent)
                        WHEN itm.parent_id IS NOT NULL THEN (
                            SELECT TOP 1 m.NewId
                            FROM @IdMapping m
                            INNER JOIN #TempItemsToMove p ON p.item_id = m.ItemId AND p.item_type = m.ItemType
                            WHERE p.item_id = itm.parent_id AND p.item_type = 2
                        )
                        ELSE NULL
                    END,
                    itm.item_type,
                    itm.item_id,
                    GETUTCDATE()
                FROM #TempItemsToMove itm
                WHERE itm.depth = @CurrentDepth;

                -- Update mapping with old IDs
                UPDATE m
                SET OldId = itm.id, OldParentId = itm.parent_id
                FROM @IdMapping m
                INNER JOIN #TempItemsToMove itm ON m.ItemType = itm.item_type AND m.ItemId = itm.item_id AND m.Depth = itm.depth
                WHERE m.Depth = @CurrentDepth;

                SET @CurrentDepth = @CurrentDepth + 1;
            END

            -- Delete from source workspace
            DELETE FROM ws.workspace_items
            WHERE id IN (SELECT id FROM #TempItemsToMove);

            SET @ResultCount = @@ROWCOUNT;
            SET @ResultMessage = 'Items moved successfully to target workspace';
        END;

        COMMIT TRANSACTION;

        -- Return result
        SELECT @ResultCount AS AffectedCount, @ResultMessage AS Message;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();

        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH;

    -- Cleanup temp table
    IF OBJECT_ID('tempdb..#TempItemsToMove') IS NOT NULL
        DROP TABLE #TempItemsToMove;
END
GO
