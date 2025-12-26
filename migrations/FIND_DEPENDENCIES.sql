-- =============================================
-- FIND DEPENDENCIES FOR is_original column
-- =============================================
USE [SuperApp-dev];
GO

PRINT 'Finding objects that depend on is_original column...';
PRINT '';

-- Find all dependencies
SELECT
    OBJECT_NAME(d.referencing_id) AS referencing_object,
    o.type_desc AS object_type,
    d.referencing_class_desc,
    OBJECT_SCHEMA_NAME(d.referencing_id) AS schema_name
FROM sys.sql_expression_dependencies d
INNER JOIN sys.objects o ON d.referencing_id = o.object_id
WHERE d.referenced_id = OBJECT_ID('ws.workspace_items')
  AND d.referenced_minor_id = (
      SELECT column_id
      FROM sys.columns
      WHERE object_id = OBJECT_ID('ws.workspace_items')
      AND name = 'is_original'
  );

PRINT '';
PRINT 'Checking for indexes...';

-- Check all indexes on workspace_items
SELECT
    i.name AS index_name,
    i.type_desc,
    COL_NAME(ic.object_id, ic.column_id) AS column_name
FROM sys.indexes i
INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
WHERE i.object_id = OBJECT_ID('ws.workspace_items')
  AND COL_NAME(ic.object_id, ic.column_id) = 'is_original';

PRINT '';
PRINT 'Checking for constraints...';

-- Check for constraints
SELECT
    c.name AS constraint_name,
    c.type_desc,
    OBJECT_NAME(c.parent_object_id) AS table_name
FROM sys.objects c
INNER JOIN sys.columns col ON c.parent_object_id = col.object_id
WHERE col.object_id = OBJECT_ID('ws.workspace_items')
  AND col.name = 'is_original'
  AND c.type IN ('D', 'C', 'F', 'PK', 'UQ');

PRINT '';
PRINT 'Done!';
GO
