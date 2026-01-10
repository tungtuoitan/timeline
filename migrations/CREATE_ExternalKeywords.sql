-- =============================================
-- Migration: Create Keywords table
-- Description: Stores external links referenced in markdown notes
-- Author: Claude Code
-- Date: 2026-01-03
-- =============================================

-- Create Keywords table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Keywords]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Keywords] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [UserId] INT NOT NULL,
        [Name] NVARCHAR(255) NOT NULL,
        [Link] NVARCHAR(2000) NOT NULL,
        [Description] NVARCHAR(MAX) NULL,
        [CreatedAt] DATETIME2(7) NULL DEFAULT (GETUTCDATE()),
        [UpdatedAt] DATETIME2(7) NULL,
        [DeletedAt] DATETIME2(7) NULL,

        CONSTRAINT [PK_Keywords] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_Keywords_Users] FOREIGN KEY ([UserId])
            REFERENCES [urm].[Users] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [UQ_Keywords_Link_UserId] UNIQUE ([Link], [UserId])
    );

    -- Create index on UserId for faster queries
    CREATE NONCLUSTERED INDEX [IX_Keywords_UserId]
        ON [dbo].[Keywords] ([UserId]);

    -- Create index on Link for faster lookups
    CREATE NONCLUSTERED INDEX [IX_Keywords_Link]
        ON [dbo].[Keywords] ([Link]);

    PRINT 'Table Keywords created successfully';
END
ELSE
BEGIN
    PRINT 'Table Keywords already exists';
END
GO


