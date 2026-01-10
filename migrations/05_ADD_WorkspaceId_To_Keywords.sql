-- =============================================
-- Migration: Add WorkspaceId column to Keywords table and sync workspace keywords
-- Description:
--   - Add WorkspaceId column (INT NULL, FK to ws.workspaces)
--   - Modify constraints to allow workspace keywords to use WorkspaceId instead of TargetItemId
--   - Add index on WorkspaceId
--   - Populate existing workspace keywords from ws.workspaces table
-- Author: Claude Code
-- Date: 2026-01-08
-- =============================================

USE [Timeline]
GO

BEGIN TRANSACTION;

BEGIN TRY
    PRINT 'Starting migration: Add WorkspaceId to Keywords table';

    -- Step 1: Drop existing constraint that prevents workspace keywords
    IF EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_Keywords_Item')
    BEGIN
        ALTER TABLE [dbo].[Keywords] DROP CONSTRAINT [CK_Keywords_Item];
        PRINT 'Dropped constraint CK_Keywords_Item';
    END

    -- Step 2: Add WorkspaceId column
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Keywords]') AND name = 'WorkspaceId')
    BEGIN
        ALTER TABLE [dbo].[Keywords]
        ADD [WorkspaceId] INT NULL;
        PRINT 'Added WorkspaceId column';
    END

    -- Step 3: Add foreign key constraint to ws.workspaces
    IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Keywords_Workspaces')
    BEGIN
        ALTER TABLE [dbo].[Keywords]
        ADD CONSTRAINT [FK_Keywords_Workspaces] FOREIGN KEY ([WorkspaceId])
            REFERENCES [ws].[workspaces] ([Id]) ON DELETE NO ACTION;
        PRINT 'Added foreign key FK_Keywords_Workspaces';
    END

    -- Step 4: Add new constraint that allows workspace keywords to use WorkspaceId
    -- Workspace keywords: Type='workspace' AND WorkspaceId IS NOT NULL AND TargetItemId IS NULL
    -- Other item keywords: Type IN ('folder','note','file') AND TargetItemId IS NOT NULL AND WorkspaceId IS NULL
    ALTER TABLE [dbo].[Keywords]
    ADD CONSTRAINT [CK_Keywords_Item] CHECK (
        -- Workspace keyword: must have WorkspaceId, no TargetItemId
        ([Type] = 'workspace' AND [WorkspaceId] IS NOT NULL AND [TargetItemId] IS NULL AND [NoteItemId] IS NULL AND [ExternalUrl] IS NULL)
        OR
        -- Folder/Note/File keyword: must have TargetItemId, no WorkspaceId
        ([Type] IN ('folder','note','file') AND [TargetItemId] IS NOT NULL AND [WorkspaceId] IS NULL AND [NoteItemId] IS NULL AND [ExternalUrl] IS NULL)
        OR
        -- Heading keyword: must have NoteItemId
        ([Type] IN ('h1','h2','h3','h4','h5','h6') AND [NoteItemId] IS NOT NULL AND [HeadingPath] IS NOT NULL AND [TargetItemId] IS NULL AND [WorkspaceId] IS NULL)
        OR
        -- External keyword: must have ExternalUrl
        ([Type] = 'external' AND [ExternalUrl] IS NOT NULL AND [TargetItemId] IS NULL AND [NoteItemId] IS NULL AND [WorkspaceId] IS NULL)
    );
    PRINT 'Added updated constraint CK_Keywords_Item';

    -- Step 5: Add index on WorkspaceId for fast lookups
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Keywords_WorkspaceId')
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_Keywords_WorkspaceId]
            ON [dbo].[Keywords] ([WorkspaceId])
            WHERE [WorkspaceId] IS NOT NULL;
        PRINT 'Added index IX_Keywords_WorkspaceId';
    END

    -- Step 6: Populate workspace keywords from ws.workspaces table
    -- Group by Name to calculate NameIndex, then insert
    PRINT 'Populating workspace keywords from ws.workspaces...';

    -- Create temp table to store workspace keywords with calculated NameIndex
    IF OBJECT_ID('tempdb..#WorkspaceKeywords') IS NOT NULL DROP TABLE #WorkspaceKeywords;

    CREATE TABLE #WorkspaceKeywords (
        WorkspaceId INT,
        UserId INT,
        Name NVARCHAR(255),
        NameIndex INT,
        Link NVARCHAR(2000),
        Description NVARCHAR(MAX),
        CreatedAt DATETIME2(7)
    );

    -- Calculate NameIndex for each workspace name group
    WITH WorkspaceNameGroups AS (
        SELECT
            Id AS WorkspaceId,
            UserId,
            Name,
            Description,
            CreatedAt,
            ROW_NUMBER() OVER (PARTITION BY Name ORDER BY CreatedAt) AS NameIndex
        FROM [ws].[workspaces]
        WHERE DeletedAt IS NULL -- Only sync active workspaces
    )
    INSERT INTO #WorkspaceKeywords (WorkspaceId, UserId, Name, NameIndex, Link, Description, CreatedAt)
    SELECT
        WorkspaceId,
        UserId,
        Name,
        NameIndex,
        'w' + CAST(WorkspaceId AS NVARCHAR(20)) AS Link,
        Description,
        CreatedAt
    FROM WorkspaceNameGroups;

    -- Insert workspace keywords (only if not already exists)
    INSERT INTO [dbo].[Keywords] (
        UserId,
        Name,
        NameIndex,
        Type,
        WorkspaceId,
        Link,
        Description,
        CreatedAt,
        UpdatedAt,
        HardDeletedAt,
        TargetItemId,
        NoteItemId,
        ExternalUrl,
        PathIds,
        HeadingPath
    )
    SELECT
        wk.UserId,
        wk.Name,
        wk.NameIndex,
        'workspace' AS Type,
        wk.WorkspaceId,
        wk.Link,
        wk.Description,
        wk.CreatedAt,
        NULL AS UpdatedAt,
        NULL AS HardDeletedAt,
        NULL AS TargetItemId,
        NULL AS NoteItemId,
        NULL AS ExternalUrl,
        NULL AS PathIds,
        NULL AS HeadingPath
    FROM #WorkspaceKeywords wk
    WHERE NOT EXISTS (
        SELECT 1
        FROM [dbo].[Keywords] k
        WHERE k.WorkspaceId = wk.WorkspaceId AND k.Type = 'workspace'
    );

    DECLARE @insertedCount INT = @@ROWCOUNT;
    PRINT 'Inserted ' + CAST(@insertedCount AS NVARCHAR(20)) + ' workspace keywords';

    -- Clean up temp table
    DROP TABLE #WorkspaceKeywords;

    COMMIT TRANSACTION;
    PRINT 'Migration completed successfully!';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrorState INT = ERROR_STATE();

    PRINT 'Migration failed: ' + @ErrorMessage;
    RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH
GO
