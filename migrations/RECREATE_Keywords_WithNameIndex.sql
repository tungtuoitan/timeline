-- =============================================
-- Migration: Recreate Keywords table with nameIndex and longLink
-- Description: Drop and recreate Keywords table with new schema
-- Author: Claude Code
-- Date: 2026-01-04
-- =============================================

-- Drop table if exists
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Keywords]') AND type in (N'U'))
BEGIN
    DROP TABLE [dbo].[Keywords];
    PRINT 'Table Keywords dropped successfully';
END

-- Create Keywords table with new schema
CREATE TABLE [dbo].[Keywords] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [UserId] INT NOT NULL,
    [Name] NVARCHAR(255) NOT NULL,
    [NameIndex] INT NOT NULL DEFAULT 1,
    [Link] NVARCHAR(2000) NOT NULL,
    [LongLink] NVARCHAR(2000) NOT NULL,
    [Type] NVARCHAR(50) NOT NULL,
    [Description] NVARCHAR(MAX) NULL,
    [CreatedAt] DATETIME2(7) NULL DEFAULT (GETUTCDATE()),
    [UpdatedAt] DATETIME2(7) NULL,
    [HardDeletedAt] DATETIME2(7) NULL,

    -- Foreign key columns for fast lookup and updates
    [WorkspaceId] INT NULL,
    [FolderWorkspaceItemId] INT NULL,
    [NoteWorkspaceItemId] INT NULL,
    [FileWorkspaceItemId] INT NULL,

    CONSTRAINT [PK_Keywords] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Keywords_Users] FOREIGN KEY ([UserId])
        REFERENCES [urm].[Users] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Keywords_Workspaces] FOREIGN KEY ([WorkspaceId])
        REFERENCES [ws].[workspaces] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Keywords_FolderItems] FOREIGN KEY ([FolderWorkspaceItemId])
        REFERENCES [ws].[workspace_items] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Keywords_NoteItems] FOREIGN KEY ([NoteWorkspaceItemId])
        REFERENCES [ws].[workspace_items] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Keywords_FileItems] FOREIGN KEY ([FileWorkspaceItemId])
        REFERENCES [ws].[workspace_items] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [UQ_Keywords_Name_NameIndex] UNIQUE ([Name], [NameIndex]),
    CONSTRAINT [UQ_Keywords_Link] UNIQUE ([Link])
);

GO   -- 🔥 BẮT BUỘC
-- Create index on UserId for faster queries
CREATE NONCLUSTERED INDEX [IX_Keywords_UserId]
    ON [dbo].[Keywords] ([UserId]);

-- Create index on Link for faster lookups
CREATE NONCLUSTERED INDEX [IX_Keywords_Link]
    ON [dbo].[Keywords] ([Link]);

-- Create index on Name for faster nameIndex lookups
CREATE NONCLUSTERED INDEX [IX_Keywords_Name]
    ON [dbo].[Keywords] ([Name]);

-- Create index on Type for filtering
CREATE NONCLUSTERED INDEX [IX_Keywords_Type]
    ON [dbo].[Keywords] ([Type]);

-- Create indexes on foreign key columns for fast lookups
CREATE NONCLUSTERED INDEX [IX_Keywords_WorkspaceId]
    ON [dbo].[Keywords] ([WorkspaceId])
    WHERE [WorkspaceId] IS NOT NULL;

CREATE NONCLUSTERED INDEX [IX_Keywords_FolderWorkspaceItemId]
    ON [dbo].[Keywords] ([FolderWorkspaceItemId])
    WHERE [FolderWorkspaceItemId] IS NOT NULL;

CREATE NONCLUSTERED INDEX [IX_Keywords_NoteWorkspaceItemId]
    ON [dbo].[Keywords] ([NoteWorkspaceItemId])
    WHERE [NoteWorkspaceItemId] IS NOT NULL;

CREATE NONCLUSTERED INDEX [IX_Keywords_FileWorkspaceItemId]
    ON [dbo].[Keywords] ([FileWorkspaceItemId])
    WHERE [FileWorkspaceItemId] IS NOT NULL;

PRINT 'Table Keywords recreated successfully with nameIndex, longLink, type, and foreign key columns';
GO
