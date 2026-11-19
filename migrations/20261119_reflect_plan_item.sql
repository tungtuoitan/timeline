-- Migration: Create Reflect plan table (pro schema)
-- Date: 20261119
-- Description: pro.reflect_plan_item - one row per task planned for a day; anything not yet a column lives in extra (JSON text)

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = 'pro' AND TABLE_NAME = 'reflect_plan_item'
)
BEGIN
    CREATE TABLE [pro].[reflect_plan_item] (
        id          NVARCHAR(36)  NOT NULL,
        user_id     INT           NOT NULL,
        plan_date   DATE          NOT NULL,
        title       NVARCHAR(400) NOT NULL CONSTRAINT DF_pro_reflect_plan_item_title  DEFAULT N'',
        [status]    NVARCHAR(32)  NOT NULL CONSTRAINT DF_pro_reflect_plan_item_status DEFAULT N'planned',
        sort_order  INT           NOT NULL CONSTRAINT DF_pro_reflect_plan_item_sort   DEFAULT 0,
        note        NVARCHAR(MAX) NOT NULL CONSTRAINT DF_pro_reflect_plan_item_note   DEFAULT N'',
        extra       NVARCHAR(MAX) NOT NULL CONSTRAINT DF_pro_reflect_plan_item_extra  DEFAULT N'{}',
        created_at  DATETIME2     NOT NULL CONSTRAINT DF_pro_reflect_plan_item_created_at DEFAULT SYSUTCDATETIME(),
        updated_at  DATETIME2     NOT NULL CONSTRAINT DF_pro_reflect_plan_item_updated_at DEFAULT SYSUTCDATETIME(),
        deleted_at  DATETIME2     NULL,
        CONSTRAINT PK_pro_reflect_plan_item PRIMARY KEY (id),
        CONSTRAINT CK_pro_reflect_plan_item_extra_json CHECK (ISJSON(extra) = 1)
    );
    PRINT 'Created table: pro.reflect_plan_item';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_pro_reflect_plan_item_user_date' AND object_id = OBJECT_ID('pro.reflect_plan_item'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_pro_reflect_plan_item_user_date
        ON [pro].[reflect_plan_item] (user_id, plan_date, sort_order);
    PRINT 'Created index: IX_pro_reflect_plan_item_user_date';
END
GO

PRINT 'Migration 20261119_reflect_plan_item completed successfully.';
GO
