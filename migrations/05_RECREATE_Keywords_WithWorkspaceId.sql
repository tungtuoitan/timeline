-- =============================================
-- Migration: Recreate Keywords table with WorkspaceId column
-- Description:
--   - Drop existing Keywords table (dev data, no backup needed)
--   - Recreate with new schema including WorkspaceId
--   - Workspace keywords will be synced via application code
-- Author: Claude Code
-- Date: 2026-01-08
-- =============================================

USE [Timeline]
GO

BEGIN TRANSACTION;

BEGIN TRY
    PRINT 'Starting migration: Recreate Keywords table with WorkspaceId';

    -- Step 1: Drop existing Keywords table (dev data)
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Keywords]') AND type in (N'U'))
    BEGIN
        DROP TABLE [dbo].[Keywords];
        PRINT 'Dropped existing Keywords table';
    END

    -- Step 2: Create Keywords table with new schema including WorkspaceId
    CREATE TABLE [dbo].[Keywords] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [UserId] INT NOT NULL,
        [Name] NVARCHAR(255) NOT NULL,
        [NameIndex] INT NOT NULL DEFAULT 1,
        [Type] NVARCHAR(50) NOT NULL,

        -- Polymorphic references (only 1 should have value based on Type)
        [WorkspaceId] INT NULL,         -- For workspace keywords -> ws.workspaces.Id
        [TargetItemId] INT NULL,        -- For folder/note/file keywords -> workspace_items.Id
        [NoteItemId] INT NULL,          -- For heading keywords (parent note) -> workspace_items.Id
        [ExternalUrl] NVARCHAR(2000) NULL,  -- For external keywords

        -- Path components
        [PathIds] NVARCHAR(1000) NULL,  -- Copy from workspace_items (NULL for workspace/heading/external)
        [HeadingPath] NVARCHAR(500) NULL,  -- 'h1-Intro/h2-Setup' (heading only)

        -- Cached Link (NO LongLink - computed at runtime)
        [Link] NVARCHAR(2000) NOT NULL,

        [Description] NVARCHAR(MAX) NULL,
        [CreatedAt] DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] DATETIME2(7) NULL,
        [HardDeletedAt] DATETIME2(7) NULL,

        -- Primary Key
        CONSTRAINT [PK_Keywords] PRIMARY KEY CLUSTERED ([Id] ASC),

        -- Foreign Keys
        CONSTRAINT [FK_Keywords_Users] FOREIGN KEY ([UserId])
            REFERENCES [urm].[Users] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Keywords_Workspaces] FOREIGN KEY ([WorkspaceId])
            REFERENCES [ws].[workspaces] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Keywords_TargetItem] FOREIGN KEY ([TargetItemId])
            REFERENCES [ws].[workspace_items] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Keywords_NoteItem] FOREIGN KEY ([NoteItemId])
            REFERENCES [ws].[workspace_items] ([Id]) ON DELETE NO ACTION,

        -- Type-based validation
        CONSTRAINT [CK_Keywords_Workspace] CHECK (
            [Type] != 'workspace' OR
            ([WorkspaceId] IS NOT NULL AND [TargetItemId] IS NULL AND [NoteItemId] IS NULL AND [ExternalUrl] IS NULL)
        ),
        CONSTRAINT [CK_Keywords_Item] CHECK (
            [Type] NOT IN ('folder','note','file') OR
            ([TargetItemId] IS NOT NULL AND [WorkspaceId] IS NULL AND [NoteItemId] IS NULL AND [ExternalUrl] IS NULL)
        ),
        CONSTRAINT [CK_Keywords_Heading] CHECK (
            [Type] NOT IN ('h1','h2','h3','h4','h5','h6') OR
            ([NoteItemId] IS NOT NULL AND [HeadingPath] IS NOT NULL AND [TargetItemId] IS NULL AND [WorkspaceId] IS NULL)
        ),
        CONSTRAINT [CK_Keywords_External] CHECK (
            [Type] != 'external' OR
            ([ExternalUrl] IS NOT NULL AND [TargetItemId] IS NULL AND [NoteItemId] IS NULL AND [WorkspaceId] IS NULL)
        ),

        -- Unique constraints
        CONSTRAINT [UQ_Keywords_Link] UNIQUE ([Link]),
        CONSTRAINT [UQ_Keywords_Name_NameIndex] UNIQUE ([Name], [NameIndex])
    );
    PRINT 'Created Keywords table with new schema';

    -- Step 3: Create indexes
    CREATE NONCLUSTERED INDEX [IX_Keywords_UserId]
        ON [dbo].[Keywords] ([UserId]);
    PRINT 'Created index IX_Keywords_UserId';

    CREATE NONCLUSTERED INDEX [IX_Keywords_WorkspaceId]
        ON [dbo].[Keywords] ([WorkspaceId])
        WHERE [WorkspaceId] IS NOT NULL;
    PRINT 'Created index IX_Keywords_WorkspaceId';

    -- UNIQUE constraint for TargetItemId (each workspace_items can have only 1 keyword)
    CREATE UNIQUE NONCLUSTERED INDEX [UQ_Keywords_TargetItemId]
        ON [dbo].[Keywords] ([TargetItemId])
        WHERE [TargetItemId] IS NOT NULL;
    PRINT 'Created unique index UQ_Keywords_TargetItemId';

    CREATE NONCLUSTERED INDEX [IX_Keywords_NoteItemId]
        ON [dbo].[Keywords] ([NoteItemId])
        WHERE [NoteItemId] IS NOT NULL;
    PRINT 'Created index IX_Keywords_NoteItemId';

    CREATE NONCLUSTERED INDEX [IX_Keywords_PathIds]
        ON [dbo].[Keywords] ([PathIds])
        WHERE [PathIds] IS NOT NULL;
    PRINT 'Created index IX_Keywords_PathIds';

    CREATE NONCLUSTERED INDEX [IX_Keywords_Type]
        ON [dbo].[Keywords] ([Type]);
    PRINT 'Created index IX_Keywords_Type';

    COMMIT TRANSACTION;
    PRINT 'Migration completed successfully!';
    PRINT 'Note: Workspace keywords will be synced via application code on first workspace create/update';

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
