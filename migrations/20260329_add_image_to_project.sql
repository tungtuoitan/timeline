IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[pro].[project]')
      AND name = 'image'
)
BEGIN
    ALTER TABLE [pro].[project]
    ADD [image] NVARCHAR(MAX) NULL;
END
GO
