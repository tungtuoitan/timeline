-- ============================================================
-- Migration: Create discussion schema (disc)
-- Schema: disc (discussion) — replaces pro.task_comment + log.log standalone
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'disc')
BEGIN
    EXEC('CREATE SCHEMA [disc] AUTHORIZATION [dbo]')
END
GO

-- disc.topic
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[disc].[topic]') AND type = 'U')
BEGIN
    CREATE TABLE [disc].[topic] (
        [id]          INT            IDENTITY (1, 1) NOT NULL,
        [user_id]     INT            NOT NULL,
        [entity_type] NVARCHAR (50)  NULL,
        [entity_id]   INT            NULL,
        [name]        NVARCHAR (255) NOT NULL,
        [description] NVARCHAR (MAX) NULL,
        [created_at]  DATETIME2 (3)  CONSTRAINT [df_disc_topic_created_at] DEFAULT (sysdatetime()) NOT NULL,
        [updated_at]  DATETIME2 (3)  CONSTRAINT [df_disc_topic_updated_at] DEFAULT (sysdatetime()) NOT NULL,
        [deleted_at]  DATETIME2 (3)  NULL,
        CONSTRAINT [PK_disc_topic] PRIMARY KEY CLUSTERED ([id] ASC)
    );

    CREATE NONCLUSTERED INDEX [IX_disc_topic_entity]  ON [disc].[topic] ([entity_type], [entity_id]);
    CREATE NONCLUSTERED INDEX [IX_disc_topic_user_id] ON [disc].[topic] ([user_id]);
END
GO

-- disc.message
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[disc].[message]') AND type = 'U')
BEGIN
    CREATE TABLE [disc].[message] (
        [id]           INT            IDENTITY (1, 1) NOT NULL,
        [user_id]      INT            NOT NULL,
        [topic_id]     INT            NULL,
        [entity_type]  NVARCHAR (50)  NULL,
        [entity_id]    INT            NULL,
        [parent_id]    INT            NULL,
        [type]         NVARCHAR (50)  NULL,
        [title]        NVARCHAR (255) NULL,
        [content]      NVARCHAR (MAX) NULL,
        [track_id]     INT            NULL,
        [location]     NVARCHAR (255) NULL,
        [occur_at]     DATETIME2 (7)  NULL,
        [is_sensitive] BIT            CONSTRAINT [df_disc_message_is_sensitive] DEFAULT (0) NOT NULL,
        [created_at]   DATETIME2 (3)  CONSTRAINT [df_disc_message_created_at] DEFAULT (sysdatetime()) NOT NULL,
        [updated_at]   DATETIME2 (3)  CONSTRAINT [df_disc_message_updated_at] DEFAULT (sysdatetime()) NOT NULL,
        [deleted_at]   DATETIME2 (3)  NULL,
        CONSTRAINT [PK_disc_message] PRIMARY KEY CLUSTERED ([id] ASC),
        CONSTRAINT [FK_disc_message_topic]  FOREIGN KEY ([topic_id])  REFERENCES [disc].[topic] ([id]) ON DELETE SET NULL,
        CONSTRAINT [FK_disc_message_parent] FOREIGN KEY ([parent_id]) REFERENCES [disc].[message] ([id]),
        CONSTRAINT [FK_disc_message_track]  FOREIGN KEY ([track_id])  REFERENCES [log].[track] ([id]) ON DELETE SET NULL
    );

    CREATE NONCLUSTERED INDEX [IX_disc_message_topic_id]   ON [disc].[message] ([topic_id]);
    CREATE NONCLUSTERED INDEX [IX_disc_message_entity]     ON [disc].[message] ([entity_type], [entity_id]);
    CREATE NONCLUSTERED INDEX [IX_disc_message_parent_id]  ON [disc].[message] ([parent_id]);
    CREATE NONCLUSTERED INDEX [IX_disc_message_created_at] ON [disc].[message] ([created_at]);
END
GO
