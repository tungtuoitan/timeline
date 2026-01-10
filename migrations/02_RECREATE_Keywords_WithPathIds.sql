-- =============================================
-- Migration: Recreate Keywords table with PathIds design
-- Description: New schema with TargetItemId, NoteItemId, PathIds
-- Author: Claude Code
-- Date: 2026-01-04
-- =============================================

-- Drop existing table if exists
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Keywords]') AND type in (N'U'))
BEGIN
    DROP TABLE [dbo].[Keywords];
    PRINT 'Dropped existing Keywords table';
END
GO

-- Create Keywords table with new schema
CREATE TABLE [dbo].[Keywords] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [UserId] INT NOT NULL,
    [Name] NVARCHAR(255) NOT NULL,
    [NameIndex] INT NOT NULL DEFAULT 1,
    [Type] NVARCHAR(50) NOT NULL,

    -- Polymorphic references (only 1 should have value)
    [TargetItemId] INT NULL,        -- workspace/folder/note/file -> workspace_items.Id
    [NoteItemId] INT NULL,          -- parent note for headings -> workspace_items.Id
    [ExternalUrl] NVARCHAR(2000) NULL,  -- external links

    -- Path components
    [PathIds] NVARCHAR(1000) NULL,  -- Copy from workspace_items (NULL for heading/external)
    [HeadingPath] NVARCHAR(500) NULL,  -- 'h1-Intro/h2-Setup' (heading only)

    -- Cached Link (NO LongLink - render runtime)
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
    CONSTRAINT [FK_Keywords_TargetItem] FOREIGN KEY ([TargetItemId])
        REFERENCES [ws].[workspace_items] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Keywords_NoteItem] FOREIGN KEY ([NoteItemId])
        REFERENCES [ws].[workspace_items] ([Id]) ON DELETE NO ACTION,

    -- Type-based validation
    CONSTRAINT [CK_Keywords_External] CHECK (
        [Type] != 'external' OR
        ([ExternalUrl] IS NOT NULL AND [TargetItemId] IS NULL AND [NoteItemId] IS NULL)
    ),
    CONSTRAINT [CK_Keywords_Heading] CHECK (
        [Type] NOT IN ('h1','h2','h3','h4','h5','h6') OR
        ([NoteItemId] IS NOT NULL AND [HeadingPath] IS NOT NULL AND [TargetItemId] IS NULL)
    ),
    CONSTRAINT [CK_Keywords_Item] CHECK (
        [Type] NOT IN ('workspace','folder','note','file') OR
        ([TargetItemId] IS NOT NULL AND [NoteItemId] IS NULL AND [ExternalUrl] IS NULL)
    ),

    -- Unique constraints
    CONSTRAINT [UQ_Keywords_Link] UNIQUE ([Link]),
    CONSTRAINT [UQ_Keywords_Name_NameIndex] UNIQUE ([Name], [NameIndex])
);
GO

-- Create indexes
CREATE NONCLUSTERED INDEX [IX_Keywords_UserId]
    ON [dbo].[Keywords] ([UserId]);

-- UNIQUE constraint for TargetItemId (each workspace_items can have only 1 keyword)
CREATE UNIQUE NONCLUSTERED INDEX [UQ_Keywords_TargetItemId]
    ON [dbo].[Keywords] ([TargetItemId])
    WHERE [TargetItemId] IS NOT NULL;

CREATE NONCLUSTERED INDEX [IX_Keywords_NoteItemId]
    ON [dbo].[Keywords] ([NoteItemId])
    WHERE [NoteItemId] IS NOT NULL;

CREATE NONCLUSTERED INDEX [IX_Keywords_PathIds]
    ON [dbo].[Keywords] ([PathIds])
    WHERE [PathIds] IS NOT NULL;

CREATE NONCLUSTERED INDEX [IX_Keywords_Type]
    ON [dbo].[Keywords] ([Type]);

PRINT 'Created Keywords table with PathIds design';
GO

PRINT 'Migration completed successfully!';
