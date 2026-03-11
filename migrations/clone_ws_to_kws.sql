-- ============================================================
-- Clone data từ ws → kws
-- Chạy SAU khi đã tạo xong tables (create_kworkspace_tables.sql)
--
-- Thứ tự:
--   1. kws.workspaces   ← ws.workspaces
--   2. kws.folders      ← ws.folders  (chỉ folders nằm trong ws.workspace_items)
--   3. kws.workspace_items ← ws.workspace_items  (giữ nguyên id, PathIds, PathDepth)
--
-- Sau khi clone, IDENTITY seed được reset về MAX(id) hiện tại
-- để INSERT mới tiếp tục đúng sequence.
-- ============================================================

USE [SuperApp-dev]
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    -- ============================================================
    -- STEP 1: Clone kws.workspaces ← ws.workspaces
    -- SET IDENTITY_INSERT để giữ nguyên id gốc
    -- ============================================================
    SET IDENTITY_INSERT [kws].[workspaces] ON;

    INSERT INTO [kws].[workspaces]
        ([id], [user_id], [name], [description], [created_at], [updated_at], [deleted_at], [status_code])
    SELECT
        [id], [user_id], [name], [description], [created_at], [updated_at], [deleted_at], [status_code]
    FROM [ws].[workspaces]
    WHERE [id] NOT IN (SELECT [id] FROM [kws].[workspaces]);  -- idempotent

    SET IDENTITY_INSERT [kws].[workspaces] OFF;

    DECLARE @wsCount INT = @@ROWCOUNT;
    PRINT CONCAT('Cloned workspaces: ', @wsCount);

    -- ============================================================
    -- STEP 2: Clone kws.folders ← ws.folders
    -- Chỉ clone folders đang được dùng trong ws.workspace_items
    -- ============================================================
    SET IDENTITY_INSERT [kws].[folders] ON;

    INSERT INTO [kws].[folders]
        ([id], [user_id], [name], [description], [color], [icon], [created_at], [updated_at], [deleted_at])
    SELECT
        f.[id], f.[user_id], f.[name], f.[description], f.[color], f.[icon],
        f.[created_at], f.[updated_at], f.[deleted_at]
    FROM [ws].[folders] f
    WHERE f.[id] IN (
        SELECT DISTINCT [entity_id]
        FROM [ws].[workspace_items]
        WHERE [entity_type] = 2
    )
    AND f.[id] NOT IN (SELECT [id] FROM [kws].[folders]);  -- idempotent

    SET IDENTITY_INSERT [kws].[folders] OFF;

    DECLARE @fCount INT = @@ROWCOUNT;
    PRINT CONCAT('Cloned folders: ', @fCount);

    -- ============================================================
    -- STEP 3: Clone kws.workspace_items ← ws.workspace_items
    -- Giữ nguyên id, PathIds, PathDepth, parent_id (self-ref)
    -- ============================================================
    SET IDENTITY_INSERT [kws].[workspace_items] ON;

    INSERT INTO [kws].[workspace_items]
        ([id], [workspace_id], [parent_id], [entity_type], [entity_id],
         [created_at], [updated_at], [deleted_at], [PathIds], [PathDepth])
    SELECT
        [id], [workspace_id], [parent_id], [entity_type], [entity_id],
        [created_at], [updated_at], [deleted_at], [PathIds], [PathDepth]
    FROM [ws].[workspace_items]
    WHERE [id] NOT IN (SELECT [id] FROM [kws].[workspace_items]);  -- idempotent

    SET IDENTITY_INSERT [kws].[workspace_items] OFF;

    DECLARE @wiCount INT = @@ROWCOUNT;
    PRINT CONCAT('Cloned workspace_items: ', @wiCount);

    COMMIT TRANSACTION;
    PRINT '=== Clone completed successfully ===';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @msg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @sev INT            = ERROR_SEVERITY();
    DECLARE @st  INT            = ERROR_STATE();
    PRINT CONCAT('ERROR: ', @msg);
    RAISERROR(@msg, @sev, @st);
END CATCH
GO

-- ============================================================
-- STEP 4: Reset IDENTITY seeds về MAX(id) hiện tại
-- Cần thiết để INSERT mới không bị conflict
-- ============================================================
DECLARE @maxWs  INT = (SELECT ISNULL(MAX(id), 0) FROM [kws].[workspaces]);
DECLARE @maxF   INT = (SELECT ISNULL(MAX(id), 0) FROM [kws].[folders]);
DECLARE @maxWi  INT = (SELECT ISNULL(MAX(id), 0) FROM [kws].[workspace_items]);

DBCC CHECKIDENT ('[kws].[workspaces]',      RESEED, @maxWs);
DBCC CHECKIDENT ('[kws].[folders]',         RESEED, @maxF);
DBCC CHECKIDENT ('[kws].[workspace_items]', RESEED, @maxWi);

PRINT CONCAT('Seeds reset — workspaces: ', @maxWs, ', folders: ', @maxF, ', workspace_items: ', @maxWi);
GO

-- ============================================================
-- STEP 5: Verify
-- ============================================================
SELECT 'ws.workspaces'       AS [source], COUNT(*) AS [count] FROM [ws].[workspaces]
UNION ALL
SELECT 'kws.workspaces',                  COUNT(*) FROM [kws].[workspaces]
UNION ALL
SELECT 'ws.folders (used)',               COUNT(DISTINCT entity_id) FROM [ws].[workspace_items] WHERE entity_type = 2
UNION ALL
SELECT 'kws.folders',                     COUNT(*) FROM [kws].[folders]
UNION ALL
SELECT 'ws.workspace_items',              COUNT(*) FROM [ws].[workspace_items]
UNION ALL
SELECT 'kws.workspace_items',             COUNT(*) FROM [kws].[workspace_items];
GO
