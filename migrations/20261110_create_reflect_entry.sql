-- Migration: Create Reflect journal table (pro schema)
-- Date: 20261110
-- Description: pro.reflect_entry - one row per Reflect line (type / I think / Reality), id made by the client (GUID text)

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = 'pro' AND TABLE_NAME = 'reflect_entry'
)
BEGIN
    CREATE TABLE [pro].[reflect_entry] (
        id          NVARCHAR(36)  NOT NULL,
        user_id     INT           NOT NULL,
        [type]      NVARCHAR(64)  NOT NULL CONSTRAINT DF_pro_reflect_entry_type  DEFAULT N'',
        think       NVARCHAR(MAX) NOT NULL CONSTRAINT DF_pro_reflect_entry_think DEFAULT N'',
        [real]      NVARCHAR(MAX) NOT NULL CONSTRAINT DF_pro_reflect_entry_real  DEFAULT N'',
        created_at  DATETIME2     NOT NULL CONSTRAINT DF_pro_reflect_entry_created_at DEFAULT SYSUTCDATETIME(),
        updated_at  DATETIME2     NOT NULL CONSTRAINT DF_pro_reflect_entry_updated_at DEFAULT SYSUTCDATETIME(),
        deleted_at  DATETIME2     NULL,
        CONSTRAINT PK_pro_reflect_entry PRIMARY KEY (id)
    );
    PRINT 'Created table: pro.reflect_entry';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_pro_reflect_entry_user_created' AND object_id = OBJECT_ID('pro.reflect_entry'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_pro_reflect_entry_user_created
        ON [pro].[reflect_entry] (user_id, created_at);
    PRINT 'Created index: IX_pro_reflect_entry_user_created';
END
GO

PRINT 'Migration 20261110_create_reflect_entry completed successfully.';
GO
