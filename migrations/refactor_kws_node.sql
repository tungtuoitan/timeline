-- ============================================================
-- KWorkspace Node Refactor Migration
--
-- Mục tiêu: Bỏ kws.folders, kws.workspace_items trở thành node tự chứa
--   - Thêm: name, description, color, icon vào kws.workspace_items
--   - Xóa:  entity_type, entity_id khỏi kws.workspace_items
--   - Drop: bảng kws.folders
--   - Cập nhật stored procedures
--
-- LƯU Ý: Xóa hết data cũ (TRUNCATE) trước khi thay đổi schema
-- ============================================================

USE [SuperApp-dev]
GO

-- ============================================================
-- STEP 1: Xóa hết data cũ trong kws.workspace_items
-- (phải truncate trước vì có self-ref FK)
-- ============================================================

-- Tắt FK constraint tạm thời để TRUNCATE
ALTER TABLE [kws].[workspace_items] NOCHECK CONSTRAINT [FK_kwi_parent];
GO

DELETE FROM [kws].[workspace_items];
GO

ALTER TABLE [kws].[workspace_items] CHECK CONSTRAINT [FK_kwi_parent];
GO

PRINT 'Cleared all data from kws.workspace_items';
GO

-- ============================================================
-- STEP 2: Xóa unique constraint và index dùng entity_type/entity_id
-- ============================================================

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_kworkspace_items_unique' AND object_id = OBJECT_ID('[kws].[workspace_items]'))
    ALTER TABLE [kws].[workspace_items] DROP CONSTRAINT [UQ_kworkspace_items_unique];
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_kwi_entity' AND object_id = OBJECT_ID('[kws].[workspace_items]'))
    DROP INDEX [IX_kwi_entity] ON [kws].[workspace_items];
GO

PRINT 'Dropped old constraints and indexes';
GO

-- ============================================================
-- STEP 3: Xóa cột entity_type, entity_id
-- ============================================================

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[kws].[workspace_items]') AND name = 'entity_type')
    ALTER TABLE [kws].[workspace_items] DROP COLUMN [entity_type];
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[kws].[workspace_items]') AND name = 'entity_id')
    ALTER TABLE [kws].[workspace_items] DROP COLUMN [entity_id];
GO

PRINT 'Dropped entity_type and entity_id columns';
GO

-- ============================================================
-- STEP 4: Thêm cột name, description, color, icon
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[kws].[workspace_items]') AND name = 'name')
    ALTER TABLE [kws].[workspace_items] ADD [name] NVARCHAR(255) NOT NULL DEFAULT '';
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[kws].[workspace_items]') AND name = 'description')
    ALTER TABLE [kws].[workspace_items] ADD [description] NVARCHAR(MAX) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[kws].[workspace_items]') AND name = 'color')
    ALTER TABLE [kws].[workspace_items] ADD [color] NVARCHAR(7) NULL DEFAULT '#F59E0B';
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[kws].[workspace_items]') AND name = 'icon')
    ALTER TABLE [kws].[workspace_items] ADD [icon] NVARCHAR(50) NULL DEFAULT N'📁';
GO

PRINT 'Added name, description, color, icon columns to kws.workspace_items';
GO

-- ============================================================
-- STEP 5: Drop kws.folders
-- ============================================================

IF EXISTS (
    SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = 'kws' AND t.name = 'folders'
)
BEGIN
    DROP TABLE [kws].[folders];
    PRINT 'Dropped table: kws.folders';
END
ELSE
    PRINT 'Table kws.folders not found, skipping.';
GO

-- ============================================================
-- STEP 6: sp_DeleteWorkspace — cập nhật, bỏ phần xóa kws.folders
-- ============================================================

IF EXISTS (
    SELECT 1 FROM sys.procedures p JOIN sys.schemas s ON p.schema_id = s.schema_id
    WHERE s.name = 'kws' AND p.name = 'sp_DeleteWorkspace'
)
    DROP PROCEDURE [kws].[sp_DeleteWorkspace];
GO

CREATE PROCEDURE [kws].[sp_DeleteWorkspace]
    @iv_workspace_ids NVARCHAR(MAX),  -- Comma-separated: '1,2,3'
    @ov_deleted_count INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        CREATE TABLE #WorkspacesToDelete (workspace_id INT PRIMARY KEY);

        INSERT INTO #WorkspacesToDelete (workspace_id)
        SELECT CAST(LTRIM(RTRIM(value)) AS INT)
        FROM STRING_SPLIT(@iv_workspace_ids, ',')
        WHERE LTRIM(RTRIM(value)) <> '';

        -- Xóa kws.workspaces → CASCADE tự xóa kws.workspace_items
        DELETE w
        FROM [kws].[workspaces] w
        INNER JOIN #WorkspacesToDelete d ON d.workspace_id = w.id;

        SELECT @ov_deleted_count = COUNT(*) FROM #WorkspacesToDelete;

        COMMIT TRANSACTION;

        DROP TABLE #WorkspacesToDelete;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        DECLARE @msg NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @sev INT            = ERROR_SEVERITY();
        DECLARE @st  INT            = ERROR_STATE();
        RAISERROR(@msg, @sev, @st);
    END CATCH
END
GO

PRINT 'Updated sp_DeleteWorkspace';
GO

-- ============================================================
-- STEP 7: sp_DeleteWorkspaceItems — chỉ xóa workspace_items rows (không còn entity)
-- ============================================================

IF EXISTS (
    SELECT 1 FROM sys.procedures p JOIN sys.schemas s ON p.schema_id = s.schema_id
    WHERE s.name = 'kws' AND p.name = 'sp_DeleteWorkspaceItems'
)
    DROP PROCEDURE [kws].[sp_DeleteWorkspaceItems];
GO

CREATE PROCEDURE [kws].[sp_DeleteWorkspaceItems]
    @iv_workspace_id  INT,
    @iv_item_ids      NVARCHAR(MAX),  -- JSON: [1, 2, 3] (workspace_items.id)
    @ov_deleted_count INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Parse JSON → workspace_item IDs
        SELECT CAST(value AS INT) AS workspace_item_id
        INTO #RootItemIds
        FROM OPENJSON(@iv_item_ids);

        -- Validate tất cả items thuộc workspace này
        IF (SELECT COUNT(*) FROM #RootItemIds) != (
            SELECT COUNT(*) FROM [kws].[workspace_items] wi
            INNER JOIN #RootItemIds r ON wi.id = r.workspace_item_id
            WHERE wi.workspace_id = @iv_workspace_id
        )
        BEGIN
            RAISERROR('One or more items not found in this workspace', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END

        -- Đệ quy thu thập con cháu qua self-ref parent_id
        CREATE TABLE #AllItemIds (workspace_item_id INT PRIMARY KEY);
        INSERT INTO #AllItemIds SELECT workspace_item_id FROM #RootItemIds;

        DECLARE @added INT = 1;
        WHILE @added > 0
        BEGIN
            INSERT INTO #AllItemIds (workspace_item_id)
            SELECT wi.id
            FROM [kws].[workspace_items] wi
            INNER JOIN #AllItemIds p ON wi.parent_id = p.workspace_item_id
            WHERE wi.workspace_id = @iv_workspace_id
              AND NOT EXISTS (SELECT 1 FROM #AllItemIds WHERE workspace_item_id = wi.id);
            SET @added = @@ROWCOUNT;
        END

        -- Hard delete workspace_items rows (luôn xóa node)
        DELETE FROM [kws].[workspace_items]
        WHERE id IN (SELECT workspace_item_id FROM #AllItemIds);

        SELECT @ov_deleted_count = COUNT(*) FROM #AllItemIds;

        COMMIT TRANSACTION;

        DROP TABLE #RootItemIds;
        DROP TABLE #AllItemIds;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        DECLARE @msg NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @sev INT            = ERROR_SEVERITY();
        DECLARE @st  INT            = ERROR_STATE();
        RAISERROR(@msg, @sev, @st);
    END CATCH
END
GO

PRINT 'Updated sp_DeleteWorkspaceItems';
GO

-- ============================================================
-- STEP 8: sp_MoveWorkspaceItems — bỏ validate entity_type=2, mọi node đều là parent được
-- ============================================================

IF EXISTS (
    SELECT 1 FROM sys.procedures p JOIN sys.schemas s ON p.schema_id = s.schema_id
    WHERE s.name = 'kws' AND p.name = 'sp_MoveWorkspaceItems'
)
    DROP PROCEDURE [kws].[sp_MoveWorkspaceItems];
GO

CREATE PROCEDURE [kws].[sp_MoveWorkspaceItems]
    @SourceWorkspaceId INT,
    @ItemIds           NVARCHAR(MAX),  -- JSON: [1, 2, 3] (workspace_items.id)
    @TargetParentId    INT = NULL,     -- workspace_items.id của parent đích, NULL = root
    @TargetWorkspaceId INT = NULL      -- NULL = same workspace
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @EffectiveTargetWorkspaceId INT = ISNULL(@TargetWorkspaceId, @SourceWorkspaceId);

        -- Parse JSON → workspace_item IDs
        CREATE TABLE #RootItemIds (workspace_item_id INT PRIMARY KEY);
        INSERT INTO #RootItemIds (workspace_item_id)
        SELECT CAST(value AS INT)
        FROM OPENJSON(@ItemIds);

        -- Validate items thuộc source workspace
        IF (SELECT COUNT(*) FROM #RootItemIds) != (
            SELECT COUNT(*) FROM [kws].[workspace_items] wi
            INNER JOIN #RootItemIds r ON wi.id = r.workspace_item_id
            WHERE wi.workspace_id = @SourceWorkspaceId AND wi.deleted_at IS NULL
        )
        BEGIN
            RAISERROR('One or more items not found in source workspace', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END

        -- Validate target parent tồn tại (không cần check entity_type nữa)
        IF @TargetParentId IS NOT NULL
        BEGIN
            IF NOT EXISTS (
                SELECT 1 FROM [kws].[workspace_items]
                WHERE id = @TargetParentId
                  AND workspace_id = @EffectiveTargetWorkspaceId
                  AND deleted_at IS NULL
            )
            BEGIN
                RAISERROR('Target parent node not found in target workspace', 16, 1);
                ROLLBACK TRANSACTION;
                RETURN;
            END
        END

        -- Đệ quy thu thập toàn bộ cây con
        CREATE TABLE #AllItemIds (
            workspace_item_id INT PRIMARY KEY,
            is_root           BIT NOT NULL DEFAULT 0
        );
        INSERT INTO #AllItemIds (workspace_item_id, is_root)
        SELECT workspace_item_id, 1 FROM #RootItemIds;

        DECLARE @added INT = 1;
        WHILE @added > 0
        BEGIN
            INSERT INTO #AllItemIds (workspace_item_id, is_root)
            SELECT wi.id, 0
            FROM [kws].[workspace_items] wi
            INNER JOIN #AllItemIds p ON wi.parent_id = p.workspace_item_id
            WHERE wi.workspace_id = @SourceWorkspaceId
              AND wi.deleted_at IS NULL
              AND NOT EXISTS (SELECT 1 FROM #AllItemIds WHERE workspace_item_id = wi.id);
            SET @added = @@ROWCOUNT;
        END

        -- Update: root items → parent_id mới; tất cả → workspace_id mới
        UPDATE wi
        SET
            wi.workspace_id = @EffectiveTargetWorkspaceId,
            wi.parent_id    = CASE WHEN a.is_root = 1 THEN @TargetParentId ELSE wi.parent_id END,
            wi.updated_at   = GETUTCDATE()
        FROM [kws].[workspace_items] wi
        INNER JOIN #AllItemIds a ON a.workspace_item_id = wi.id;

        DECLARE @ResultCount INT = @@ROWCOUNT;

        COMMIT TRANSACTION;

        DROP TABLE #RootItemIds;
        DROP TABLE #AllItemIds;

        SELECT @ResultCount AS AffectedCount, 'Items moved successfully' AS Message;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;

        IF OBJECT_ID('tempdb..#RootItemIds') IS NOT NULL DROP TABLE #RootItemIds;
        IF OBJECT_ID('tempdb..#AllItemIds')  IS NOT NULL DROP TABLE #AllItemIds;

        DECLARE @ErrorMessage  NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT            = ERROR_SEVERITY();
        DECLARE @ErrorState    INT            = ERROR_STATE();
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END
GO

PRINT 'Updated sp_MoveWorkspaceItems';
GO

-- ============================================================
-- STEP 9: Verify schema
-- ============================================================
SELECT
    c.name AS column_name,
    t.name AS type_name,
    c.is_nullable
FROM sys.columns c
JOIN sys.types t ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID('[kws].[workspace_items]')
ORDER BY c.column_id;
GO

PRINT '=== KWorkspace Node Refactor migration completed successfully ===';
GO
