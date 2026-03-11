-- ============================================================
-- KWorkspace Tables Migration
-- Tham chiếu từ schema thực tế:
--   URM.users, dbo.notes, dbo.files, ws.folders, ws.workspaces, ws.workspace_items
--
-- Tạo mới:
--   kws.workspaces      -- giống ws.workspaces
--   kws.workspace_items -- giống ws.workspace_items (PathIds/PathDepth PascalCase)
--   kws.folders         -- giống ws.folders
--   kws.sp_DeleteWorkspace
--   kws.sp_DeleteWorkspaceItems
--   kws.sp_MoveWorkspaceItems
--
-- workspace_items.parent_id = self-ref → kws.workspace_items.id
-- ============================================================

USE [SuperApp-dev]
GO

-- ============================================================
-- STEP 1: Create schema kws
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'kws')
    EXEC('CREATE SCHEMA kws');
GO

-- ============================================================
-- STEP 2: kws.workspaces  (copy ws.workspaces)
-- ============================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = 'kws' AND t.name = 'workspaces'
)
BEGIN
    CREATE TABLE [kws].[workspaces] (
        [id]          INT            IDENTITY(1,1) NOT NULL,
        [user_id]     INT            NOT NULL,
        [name]        NVARCHAR(255)  NOT NULL,
        [description] NVARCHAR(1000) NULL,
        [created_at]  DATETIME2(7)   NULL,
        [updated_at]  DATETIME2(7)   NULL,
        [deleted_at]  DATETIME2(7)   NULL,
        [status_code] NVARCHAR(50)   NULL,

        CONSTRAINT [PK_kworkspaces] PRIMARY KEY CLUSTERED ([id] ASC),

        CONSTRAINT [FK_kworkspaces_user] FOREIGN KEY ([user_id])
            REFERENCES [URM].[users] ([id])
    );

    ALTER TABLE [kws].[workspaces]
        ADD CONSTRAINT [DF_kworkspaces_created_at] DEFAULT (GETUTCDATE()) FOR [created_at];

    PRINT 'Created table: kws.workspaces';
END
ELSE
    PRINT 'Table kws.workspaces already exists, skipping.';
GO

-- ============================================================
-- STEP 3: kws.folders  (copy ws.folders)
-- Folders thuộc kws tách biệt hoàn toàn với ws.folders
-- ============================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = 'kws' AND t.name = 'folders'
)
BEGIN
    CREATE TABLE [kws].[folders] (
        [id]          INT            IDENTITY(1,1) NOT NULL,
        [user_id]     INT            NOT NULL,
        [name]        NVARCHAR(255)  NOT NULL,
        [description] NVARCHAR(MAX)  NULL,
        [color]       NVARCHAR(7)    NULL,
        [icon]        NVARCHAR(50)   NULL,
        [created_at]  DATETIME2(7)   NULL,
        [updated_at]  DATETIME2(7)   NULL,
        [deleted_at]  DATETIME2(7)   NULL,

        CONSTRAINT [PK_kfolders] PRIMARY KEY CLUSTERED ([id] ASC),

        CONSTRAINT [FK_kfolders_user] FOREIGN KEY ([user_id])
            REFERENCES [URM].[users] ([id])
    );

    ALTER TABLE [kws].[folders] ADD CONSTRAINT [DF_kfolders_color]      DEFAULT ('#F59E0B') FOR [color];
    ALTER TABLE [kws].[folders] ADD CONSTRAINT [DF_kfolders_icon]       DEFAULT ('📁')      FOR [icon];
    ALTER TABLE [kws].[folders] ADD CONSTRAINT [DF_kfolders_created_at] DEFAULT (GETUTCDATE()) FOR [created_at];

    PRINT 'Created table: kws.folders';
END
ELSE
    PRINT 'Table kws.folders already exists, skipping.';
GO

-- ============================================================
-- STEP 4: kws.workspace_items  (copy ws.workspace_items)
-- Giống hệt ws.workspace_items:
--   - entity_type / entity_id
--   - parent_id = self-ref → kws.workspace_items.id
--   - PathIds / PathDepth (PascalCase, giống ws thực tế)
-- ============================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = 'kws' AND t.name = 'workspace_items'
)
BEGIN
    CREATE TABLE [kws].[workspace_items] (
        [id]           INT            IDENTITY(1,1) NOT NULL,
        [workspace_id] INT            NOT NULL,
        [parent_id]    INT            NULL,            -- self-ref → kws.workspace_items.id, NULL = root
        [entity_type]  TINYINT        NOT NULL,        -- 2=folder, 3=note, 4=file
        [entity_id]    INT            NOT NULL,
        [created_at]   DATETIME2(7)   NOT NULL,
        [updated_at]   DATETIME2(7)   NULL,
        [deleted_at]   DATETIME2(7)   NULL,
        [PathIds]      NVARCHAR(1000) NOT NULL,
        [PathDepth]    INT            NOT NULL,

        CONSTRAINT [PK_kworkspace_items] PRIMARY KEY CLUSTERED ([id] ASC),

        CONSTRAINT [UQ_kworkspace_items_unique] UNIQUE NONCLUSTERED
            ([workspace_id] ASC, [entity_type] ASC, [entity_id] ASC),

        CONSTRAINT [FK_kwi_workspace] FOREIGN KEY ([workspace_id])
            REFERENCES [kws].[workspaces] ([id])
            ON DELETE CASCADE,

        CONSTRAINT [FK_kwi_parent] FOREIGN KEY ([parent_id])
            REFERENCES [kws].[workspace_items] ([id]),

        CONSTRAINT [CK_kwi_MaxDepth] CHECK ([PathDepth] <= 10)
    );

    ALTER TABLE [kws].[workspace_items] ADD CONSTRAINT [DF_kwi_created_at]  DEFAULT (GETUTCDATE()) FOR [created_at];
    ALTER TABLE [kws].[workspace_items] ADD CONSTRAINT [DF_kwi_PathIds]     DEFAULT ('/')          FOR [PathIds];
    ALTER TABLE [kws].[workspace_items] ADD CONSTRAINT [DF_kwi_PathDepth]   DEFAULT (0)            FOR [PathDepth];

    PRINT 'Created table: kws.workspace_items';
END
ELSE
    PRINT 'Table kws.workspace_items already exists, skipping.';
GO

-- ============================================================
-- STEP 5: Indexes
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_kworkspaces_user')
    CREATE INDEX [IX_kworkspaces_user]
        ON [kws].[workspaces] ([user_id])
        WHERE [deleted_at] IS NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_kwi_workspace')
    CREATE INDEX [IX_kwi_workspace]
        ON [kws].[workspace_items] ([workspace_id], [deleted_at])
        WHERE [deleted_at] IS NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_kwi_parent')
    CREATE INDEX [IX_kwi_parent]
        ON [kws].[workspace_items] ([parent_id])
        WHERE [deleted_at] IS NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_kwi_entity')
    CREATE INDEX [IX_kwi_entity]
        ON [kws].[workspace_items] ([entity_type], [entity_id]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_kwi_path')
    CREATE INDEX [IX_kwi_path]
        ON [kws].[workspace_items] ([PathIds], [PathDepth])
        WHERE [deleted_at] IS NULL;
GO

-- ============================================================
-- STEP 6: sp_DeleteWorkspace
-- Hard delete workspace + kws.folders + kws.workspace_items.
-- Notes (dbo.notes) và Files (dbo.files) KHÔNG bị xóa.
-- CASCADE trên FK_kwi_workspace tự xóa kws.workspace_items.
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

        -- Thu thập folder_id để xóa kws.folders
        CREATE TABLE #FolderIdsToDelete (folder_id INT PRIMARY KEY);

        INSERT INTO #FolderIdsToDelete (folder_id)
        SELECT DISTINCT wi.entity_id
        FROM [kws].[workspace_items] wi
        INNER JOIN #WorkspacesToDelete w ON w.workspace_id = wi.workspace_id
        WHERE wi.entity_type = 2;

        -- Xóa kws.folders (Notes/Files giữ nguyên)
        DELETE f
        FROM [kws].[folders] f
        INNER JOIN #FolderIdsToDelete d ON d.folder_id = f.id;

        -- Xóa kws.workspaces → CASCADE tự xóa kws.workspace_items
        DELETE w
        FROM [kws].[workspaces] w
        INNER JOIN #WorkspacesToDelete d ON d.workspace_id = w.id;

        SELECT @ov_deleted_count = COUNT(*) FROM #WorkspacesToDelete;

        COMMIT TRANSACTION;

        DROP TABLE #WorkspacesToDelete;
        DROP TABLE #FolderIdsToDelete;

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

-- ============================================================
-- STEP 7: sp_DeleteWorkspaceItems
-- Xóa items + con cháu đệ quy qua self-ref parent_id.
-- - Folders: hard delete kws.folders
-- - Notes (dbo.notes): soft/hard delete tùy @iv_is_hardDelete
-- - Files (dbo.files): soft/hard delete tùy @iv_is_hardDelete
-- - workspace_items rows: luôn hard delete (xóa mapping)
-- ============================================================
IF EXISTS (
    SELECT 1 FROM sys.procedures p JOIN sys.schemas s ON p.schema_id = s.schema_id
    WHERE s.name = 'kws' AND p.name = 'sp_DeleteWorkspaceItems'
)
    DROP PROCEDURE [kws].[sp_DeleteWorkspaceItems];
GO

CREATE PROCEDURE [kws].[sp_DeleteWorkspaceItems]
    @iv_workspace_id  INT,
    @iv_items         NVARCHAR(MAX),  -- JSON: [{"type":2,"id":10}, ...]
    @iv_is_hardDelete BIT = 0,        -- 0=soft delete, 1=hard delete
    @ov_deleted_count INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Parse JSON → (entity_type, entity_id)
        SELECT
            CAST(JSON_VALUE(value, '$.type') AS TINYINT) AS entity_type,
            CAST(JSON_VALUE(value, '$.id')   AS INT)     AS entity_id
        INTO #ItemsInput
        FROM OPENJSON(@iv_items);

        -- Tìm workspace_items.id của root items
        SELECT wi.id AS workspace_item_id
        INTO #RootItemIds
        FROM [kws].[workspace_items] wi
        INNER JOIN #ItemsInput inp
            ON wi.entity_type = inp.entity_type
           AND wi.entity_id   = inp.entity_id
        WHERE wi.workspace_id = @iv_workspace_id;

        -- Validate
        IF (SELECT COUNT(*) FROM #ItemsInput) != (SELECT COUNT(*) FROM #RootItemIds)
        BEGIN
            RAISERROR('One or more items not found in this workspace', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END

        -- Đệ quy thu thập con cháu qua parent_id → workspace_items.id
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

        -- Folders: luôn hard delete kws.folders (như ws version)
        DELETE f
        FROM [kws].[folders] f
        INNER JOIN [kws].[workspace_items] wi ON wi.entity_id = f.id AND wi.entity_type = 2
        INNER JOIN #AllItemIds d ON d.workspace_item_id = wi.id;

        -- Notes (dbo.notes)
        IF @iv_is_hardDelete = 1
            DELETE n
            FROM [dbo].[notes] n
            INNER JOIN [kws].[workspace_items] wi ON wi.entity_id = n.id AND wi.entity_type = 3
            INNER JOIN #AllItemIds d ON d.workspace_item_id = wi.id;
        ELSE
            UPDATE n
            SET n.deleted_at = GETUTCDATE()
            FROM [dbo].[notes] n
            INNER JOIN [kws].[workspace_items] wi ON wi.entity_id = n.id AND wi.entity_type = 3
            INNER JOIN #AllItemIds d ON d.workspace_item_id = wi.id;

        -- Files (dbo.files)
        IF @iv_is_hardDelete = 1
            DELETE f
            FROM [dbo].[files] f
            INNER JOIN [kws].[workspace_items] wi ON wi.entity_id = f.id AND wi.entity_type = 4
            INNER JOIN #AllItemIds d ON d.workspace_item_id = wi.id;
        ELSE
            UPDATE f
            SET f.deleted_at = GETUTCDATE()
            FROM [dbo].[files] f
            INNER JOIN [kws].[workspace_items] wi ON wi.entity_id = f.id AND wi.entity_type = 4
            INNER JOIN #AllItemIds d ON d.workspace_item_id = wi.id;

        -- Hard delete workspace_items rows (luôn xóa mapping)
        DELETE FROM [kws].[workspace_items]
        WHERE id IN (SELECT workspace_item_id FROM #AllItemIds);

        SELECT @ov_deleted_count = COUNT(*) FROM #AllItemIds;

        COMMIT TRANSACTION;

        DROP TABLE #ItemsInput;
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

-- ============================================================
-- STEP 8: sp_MoveWorkspaceItems
-- Move items + con cháu. parent_id = self-ref workspace_items.id.
-- Same-workspace: UPDATE parent_id của root items.
-- Cross-workspace: UPDATE workspace_id + parent_id toàn bộ cây.
-- ============================================================
IF EXISTS (
    SELECT 1 FROM sys.procedures p JOIN sys.schemas s ON p.schema_id = s.schema_id
    WHERE s.name = 'kws' AND p.name = 'sp_MoveWorkspaceItems'
)
    DROP PROCEDURE [kws].[sp_MoveWorkspaceItems];
GO

CREATE PROCEDURE [kws].[sp_MoveWorkspaceItems]
    @SourceWorkspaceId INT,
    @Items             NVARCHAR(MAX),  -- JSON: [{"type":2,"id":10}, ...]
    @TargetParentId    INT = NULL,     -- workspace_items.id của parent đích, NULL = root
    @TargetWorkspaceId INT = NULL      -- NULL = same workspace
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @EffectiveTargetWorkspaceId INT = ISNULL(@TargetWorkspaceId, @SourceWorkspaceId);

        -- Parse JSON
        DECLARE @ItemsTable TABLE (entity_type TINYINT, entity_id INT);
        INSERT INTO @ItemsTable (entity_type, entity_id)
        SELECT
            CAST(JSON_VALUE(value, '$.type') AS TINYINT),
            CAST(JSON_VALUE(value, '$.id')   AS INT)
        FROM OPENJSON(@Items);

        -- Tìm workspace_items.id của root items
        CREATE TABLE #RootItemIds (workspace_item_id INT PRIMARY KEY);
        INSERT INTO #RootItemIds (workspace_item_id)
        SELECT wi.id
        FROM [kws].[workspace_items] wi
        INNER JOIN @ItemsTable it
            ON wi.entity_type = it.entity_type
           AND wi.entity_id   = it.entity_id
        WHERE wi.workspace_id = @SourceWorkspaceId
          AND wi.deleted_at IS NULL;

        -- Validate
        IF (SELECT COUNT(*) FROM @ItemsTable) != (SELECT COUNT(*) FROM #RootItemIds)
        BEGIN
            RAISERROR('One or more items not found in source workspace', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END

        -- Validate target parent
        IF @TargetParentId IS NOT NULL
        BEGIN
            IF NOT EXISTS (
                SELECT 1 FROM [kws].[workspace_items]
                WHERE id = @TargetParentId
                  AND workspace_id = @EffectiveTargetWorkspaceId
                  AND entity_type = 2
                  AND deleted_at IS NULL
            )
            BEGIN
                RAISERROR('Target parent folder not found in target workspace', 16, 1);
                ROLLBACK TRANSACTION;
                RETURN;
            END
        END

        -- Đệ quy thu thập toàn bộ cây con
        CREATE TABLE #AllItemIds (
            workspace_item_id INT  PRIMARY KEY,
            is_root           BIT  NOT NULL DEFAULT 0
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

-- ============================================================
-- STEP 9: Verify
-- ============================================================
SELECT
    s.name AS [schema],
    t.name AS [table],
    (SELECT COUNT(*) FROM sys.columns c WHERE c.object_id = t.object_id) AS col_count
FROM sys.tables t
JOIN sys.schemas s ON t.schema_id = s.schema_id
WHERE s.name = 'kws'
ORDER BY t.name;

SELECT
    s.name AS [schema],
    p.name AS [procedure]
FROM sys.procedures p
JOIN sys.schemas s ON p.schema_id = s.schema_id
WHERE s.name = 'kws'
ORDER BY p.name;
GO

PRINT '=== KWorkspace migration completed successfully ===';
GO
