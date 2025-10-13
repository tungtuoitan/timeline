# Quick Reference Guide

Fast lookup for common operations and queries.

---

## Table of Contents

1. [Common Procedures](#common-procedures)
2. [Common Queries](#common-queries)
3. [Maintenance Tasks](#maintenance-tasks)
4. [Troubleshooting](#troubleshooting)

---

## Common Procedures

### Create Tag

```sql
EXEC sp_create_tag 
    @user_id = 1,
    @name = 'Work',
    @parent_id = NULL,  -- NULL = root tag
    @color = '#0066CC',
    @icon = 'briefcase';
```

### Create Child Tag

```sql
-- Get parent ID first
DECLARE @parent_id INT;
SELECT @parent_id = id FROM tags 
WHERE user_id = 1 AND name = 'Work';

-- Create child
EXEC sp_create_tag 
    @user_id = 1,
    @name = 'ProjectX',
    @parent_id = @parent_id;
```

### Tag an Item

```sql
EXEC sp_tag_item 
    @user_id = 1,
    @tag_id = 5,
    @taggable_id = 123,
    @taggable_type = 'Note';
```

### Share Tag

```sql
EXEC sp_share_tag 
    @owner_id = 1,
    @tag_id = 5,
    @recipient_id = 2,
    @can_read = 1,
    @can_tag = 1,
    @include_children = 1;
```

### Move Tag

```sql
EXEC sp_move_tag 
    @user_id = 1,
    @tag_id = 5,
    @new_parent_id = 10;  -- NULL = move to root
```

---

## Common Queries

### Get All User's Tags

```sql
EXEC sp_get_user_tags 
    @user_id = 1,
    @include_shared = 1;
```

Or raw SQL:
```sql
SELECT 
    id, 
    name, 
    parent_id, 
    path, 
    color, 
    icon
FROM tags
WHERE user_id = 1
AND deleted_at IS NULL
ORDER BY path;
```

### Get Tag Subtree

```sql
EXEC sp_get_tag_subtree 
    @user_id = 1,
    @root_tag_id = 5;
```

Or raw SQL:
```sql
SELECT t.*
FROM tags t
INNER JOIN tag_paths p ON t.id = p.descendant_id
WHERE p.ancestor_id = 5
AND t.user_id = 1
AND t.deleted_at IS NULL
ORDER BY p.depth, t.name;
```

### Get All Items Tagged With X

```sql
EXEC sp_get_tagged_items 
    @user_id = 1,
    @tag_id = 5,
    @include_subtree = 1,  -- Include child tags
    @entity_type = 'Note';  -- NULL = all types
```

### Search Tags

```sql
EXEC sp_search_tags 
    @user_id = 1,
    @query = 'project',
    @limit = 20;
```

### Get Tag Breadcrumb

```sql
EXEC sp_get_tag_breadcrumb @tag_id = 5;
```

Returns: Root → Parent → Current Tag

---

## Common Checks

### Check If User Can Access Tag

```sql
SELECT dbo.fn_can_access_tag(
    @user_id = 5,
    @tag_id = 10,
    @permission = 'tag'  -- 'read', 'write', 'tag', 'untag', 'reshare'
);
-- Returns 1 (yes) or 0 (no)
```

### Get Tag Statistics

```sql
SELECT * FROM dbo.fn_get_tag_statistics(5);
```

Returns: depth, children count, descendants count, etc.

### Check Circular Reference

```sql
-- Before moving tag 5 under tag 10
IF dbo.fn_is_tag_ancestor(5, 10) = 1
BEGIN
    PRINT 'Would create circular reference!';
END;
```

---

## Maintenance Tasks

### Rebuild Closure Table

```sql
-- If closure table gets corrupted
TRUNCATE TABLE tag_paths;

-- Force trigger re-execution
UPDATE tags SET updated_at = GETDATE();
```

### Clean Soft-Deleted Tags

```sql
-- Permanently delete tags older than 30 days
DELETE FROM tags
WHERE deleted_at < DATEADD(DAY, -30, GETDATE());
```

### Rebuild Indexes

```sql
-- Rebuild fragmented indexes
ALTER INDEX ALL ON tags REBUILD;
ALTER INDEX ALL ON tag_paths REBUILD;
ALTER INDEX ALL ON taggables REBUILD;
```

### Update Statistics

```sql
UPDATE STATISTICS tags;
UPDATE STATISTICS tag_paths;
UPDATE STATISTICS taggables;
```

---

## Monitoring Queries

### Tag Count Per User

```sql
SELECT 
    user_id,
    COUNT(*) AS tag_count
FROM tags
WHERE deleted_at IS NULL
GROUP BY user_id
ORDER BY tag_count DESC;
```

### Deepest Tags

```sql
SELECT 
    t.id,
    t.user_id,
    t.name,
    t.path,
    dbo.fn_get_tag_depth(t.id) AS depth
FROM tags t
WHERE t.deleted_at IS NULL
ORDER BY depth DESC;
```

### Most Used Tags

```sql
SELECT 
    t.id,
    t.name,
    COUNT(tg.id) AS usage_count
FROM tags t
LEFT JOIN taggables tg ON tg.tag_id = t.id
WHERE t.user_id = 1
AND t.deleted_at IS NULL
GROUP BY t.id, t.name
ORDER BY usage_count DESC;
```

### Orphaned Records

```sql
-- Taggables without valid tags
SELECT * FROM taggables tg
WHERE NOT EXISTS (
    SELECT 1 FROM tags t 
    WHERE t.id = tg.tag_id
);
```

### Active Shares

```sql
SELECT 
    t.name AS tag_name,
    u1.username AS owner,
    u2.username AS shared_with,
    s.shared_at,
    s.expires_at
FROM tag_shares s
INNER JOIN tags t ON s.tag_id = t.id
INNER JOIN users u1 ON s.owner_id = u1.id
INNER JOIN users u2 ON s.shared_with_id = u2.id
WHERE s.revoked_at IS NULL
ORDER BY s.shared_at DESC;
```

---

## Troubleshooting

### Error: Circular Reference (50001)

**Cause:** Trying to set parent to own descendant

**Fix:**
```sql
-- Check ancestry before moving
SELECT * FROM tag_paths
WHERE ancestor_id = @tag_to_move
AND descendant_id = @new_parent;

-- If returns rows, it's circular
```

### Error: Tag Not Found (50005)

**Cause:** Tag deleted or wrong user_id

**Check:**
```sql
SELECT * FROM tags 
WHERE id = @tag_id;

-- Check if deleted
SELECT * FROM tags 
WHERE id = @tag_id 
AND deleted_at IS NOT NULL;
```

### Error: Cross-User Parent (50003)

**Cause:** Trying to set parent from different user

**Fix:** Tags can only have parents from same user

### Slow Queries

**Check execution plan:**
```sql
SET STATISTICS TIME ON;
SET STATISTICS IO ON;

-- Run your query
EXEC sp_get_tag_subtree @user_id = 1, @root_tag_id = 5;

SET STATISTICS TIME OFF;
SET STATISTICS IO OFF;
```

**Check missing indexes:**
```sql
SELECT 
    OBJECT_NAME(d.object_id) AS table_name,
    d.equality_columns,
    d.inequality_columns,
    d.included_columns
FROM sys.dm_db_missing_index_details d
WHERE OBJECT_NAME(d.object_id) LIKE 'tag%';
```

### Closure Table Out of Sync

**Symptoms:** Subtree queries return wrong results

**Fix:**
```sql
-- Rebuild from scratch
TRUNCATE TABLE tag_paths;

-- Re-trigger maintenance
UPDATE tags SET updated_at = GETDATE();
```

---

## Performance Tips

### Always Use Indexes

```sql
-- BAD: Full table scan
SELECT * FROM tags WHERE name = 'Work';

-- GOOD: Use user_id + name
SELECT * FROM tags 
WHERE user_id = 1 
AND name = 'Work';
```

### Use Closure Table for Subtree

```sql
-- BAD: Materialized path LIKE
SELECT * FROM tags
WHERE path LIKE 'Work.%';

-- GOOD: Closure table
SELECT t.* FROM tags t
INNER JOIN tag_paths p ON t.id = p.descendant_id
WHERE p.ancestor_id = @work_tag_id;
```

### Avoid N+1 Queries

```sql
-- BAD: Loop calling sp_get_tag_subtree
DECLARE @tag_id INT;
DECLARE tag_cursor CURSOR FOR 
    SELECT id FROM tags WHERE user_id = 1;
OPEN tag_cursor;
FETCH NEXT FROM tag_cursor INTO @tag_id;
WHILE @@FETCH_STATUS = 0
BEGIN
    EXEC sp_get_tag_subtree @user_id = 1, @root_tag_id = @tag_id;
    FETCH NEXT FROM tag_cursor INTO @tag_id;
END;

-- GOOD: Single query
SELECT t.*, p.depth
FROM tags t
INNER JOIN tag_paths p ON t.id = p.descendant_id
WHERE t.user_id = 1
ORDER BY p.ancestor_id, p.depth;
```

---

## Backup & Restore

### Backup

```sql
-- Full backup
BACKUP DATABASE SuperApp-dev
TO DISK = 'C:\Backups\SuperApp-dev.bak'
WITH FORMAT, INIT, COMPRESSION;

-- Backup specific tables
SELECT * INTO tags_backup FROM tags;
SELECT * INTO tag_paths_backup FROM tag_paths;
```

### Restore

```sql
-- Restore database
RESTORE DATABASE SuperApp-dev
FROM DISK = 'C:\Backups\SuperApp-dev.bak'
WITH REPLACE;

-- Restore specific tables
INSERT INTO tags SELECT * FROM tags_backup;
```

---

## Migration Script Template

```sql
-- Add new column
ALTER TABLE tags ADD new_column NVARCHAR(255) NULL;

-- Populate data
UPDATE tags SET new_column = 'default_value';

-- Make NOT NULL if needed
ALTER TABLE tags ALTER COLUMN new_column NVARCHAR(255) NOT NULL;

-- Add index
CREATE INDEX idx_tags_new_column ON tags(new_column);
```

---

## Quick Diagnostics

```sql
-- Database size
EXEC sp_spaceused 'tags';
EXEC sp_spaceused 'tag_paths';
EXEC sp_spaceused 'taggables';

-- Row counts
SELECT 
    'tags' AS table_name, COUNT(*) AS row_count FROM tags
UNION ALL
SELECT 'tag_paths', COUNT(*) FROM tag_paths
UNION ALL
SELECT 'taggables', COUNT(*) FROM taggables;

-- Index fragmentation
SELECT 
    OBJECT_NAME(ips.object_id) AS table_name,
    i.name AS index_name,
    ips.avg_fragmentation_in_percent
FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') ips
INNER JOIN sys.indexes i ON ips.object_id = i.object_id 
    AND ips.index_id = i.index_id
WHERE ips.avg_fragmentation_in_percent > 30
AND OBJECT_NAME(ips.object_id) LIKE 'tag%';
```

---

**For complete documentation, see [README.md](README.md)**
EOF
cat /mnt/user-data/outputs/08-Quick-Reference.md
Output

# Quick Reference Guide

Fast lookup for common operations and queries.

---

## Table of Contents

1. [Common Procedures](#common-procedures)
2. [Common Queries](#common-queries)
3. [Maintenance Tasks](#maintenance-tasks)
4. [Troubleshooting](#troubleshooting)

---

## Common Procedures

### Create Tag

```sql
EXEC sp_create_tag 
    @user_id = 1,
    @name = 'Work',
    @parent_id = NULL,  -- NULL = root tag
    @color = '#0066CC',
    @icon = 'briefcase';
```

### Create Child Tag

```sql
-- Get parent ID first
DECLARE @parent_id INT;
SELECT @parent_id = id FROM tags 
WHERE user_id = 1 AND name = 'Work';

-- Create child
EXEC sp_create_tag 
    @user_id = 1,
    @name = 'ProjectX',
    @parent_id = @parent_id;
```

### Tag an Item

```sql
EXEC sp_tag_item 
    @user_id = 1,
    @tag_id = 5,
    @taggable_id = 123,
    @taggable_type = 'Note';
```

### Share Tag

```sql
EXEC sp_share_tag 
    @owner_id = 1,
    @tag_id = 5,
    @recipient_id = 2,
    @can_read = 1,
    @can_tag = 1,
    @include_children = 1;
```

### Move Tag

```sql
EXEC sp_move_tag 
    @user_id = 1,
    @tag_id = 5,
    @new_parent_id = 10;  -- NULL = move to root
```

---

## Common Queries

### Get All User's Tags

```sql
EXEC sp_get_user_tags 
    @user_id = 1,
    @include_shared = 1;
```

Or raw SQL:
```sql
SELECT 
    id, 
    name, 
    parent_id, 
    path, 
    color, 
    icon
FROM tags
WHERE user_id = 1
AND deleted_at IS NULL
ORDER BY path;
```

### Get Tag Subtree

```sql
EXEC sp_get_tag_subtree 
    @user_id = 1,
    @root_tag_id = 5;
```

Or raw SQL:
```sql
SELECT t.*
FROM tags t
INNER JOIN tag_paths p ON t.id = p.descendant_id
WHERE p.ancestor_id = 5
AND t.user_id = 1
AND t.deleted_at IS NULL
ORDER BY p.depth, t.name;
```

### Get All Items Tagged With X

```sql
EXEC sp_get_tagged_items 
    @user_id = 1,
    @tag_id = 5,
    @include_subtree = 1,  -- Include child tags
    @entity_type = 'Note';  -- NULL = all types
```

### Search Tags

```sql
EXEC sp_search_tags 
    @user_id = 1,
    @query = 'project',
    @limit = 20;
```

### Get Tag Breadcrumb

```sql
EXEC sp_get_tag_breadcrumb @tag_id = 5;
```

Returns: Root → Parent → Current Tag

---

## Common Checks

### Check If User Can Access Tag

```sql
SELECT dbo.fn_can_access_tag(
    @user_id = 5,
    @tag_id = 10,
    @permission = 'tag'  -- 'read', 'write', 'tag', 'untag', 'reshare'
);
-- Returns 1 (yes) or 0 (no)
```

### Get Tag Statistics

```sql
SELECT * FROM dbo.fn_get_tag_statistics(5);
```

Returns: depth, children count, descendants count, etc.

### Check Circular Reference

```sql
-- Before moving tag 5 under tag 10
IF dbo.fn_is_tag_ancestor(5, 10) = 1
BEGIN
    PRINT 'Would create circular reference!';
END;
```

---

## Maintenance Tasks

### Rebuild Closure Table

```sql
-- If closure table gets corrupted
TRUNCATE TABLE tag_paths;

-- Force trigger re-execution
UPDATE tags SET updated_at = GETDATE();
```

### Clean Soft-Deleted Tags

```sql
-- Permanently delete tags older than 30 days
DELETE FROM tags
WHERE deleted_at < DATEADD(DAY, -30, GETDATE());
```

### Rebuild Indexes

```sql
-- Rebuild fragmented indexes
ALTER INDEX ALL ON tags REBUILD;
ALTER INDEX ALL ON tag_paths REBUILD;
ALTER INDEX ALL ON taggables REBUILD;
```

### Update Statistics

```sql
UPDATE STATISTICS tags;
UPDATE STATISTICS tag_paths;
UPDATE STATISTICS taggables;
```

---

## Monitoring Queries

### Tag Count Per User

```sql
SELECT 
    user_id,
    COUNT(*) AS tag_count
FROM tags
WHERE deleted_at IS NULL
GROUP BY user_id
ORDER BY tag_count DESC;
```

### Deepest Tags

```sql
SELECT 
    t.id,
    t.user_id,
    t.name,
    t.path,
    dbo.fn_get_tag_depth(t.id) AS depth
FROM tags t
WHERE t.deleted_at IS NULL
ORDER BY depth DESC;
```

### Most Used Tags

```sql
SELECT 
    t.id,
    t.name,
    COUNT(tg.id) AS usage_count
FROM tags t
LEFT JOIN taggables tg ON tg.tag_id = t.id
WHERE t.user_id = 1
AND t.deleted_at IS NULL
GROUP BY t.id, t.name
ORDER BY usage_count DESC;
```

### Orphaned Records

```sql
-- Taggables without valid tags
SELECT * FROM taggables tg
WHERE NOT EXISTS (
    SELECT 1 FROM tags t 
    WHERE t.id = tg.tag_id
);
```

### Active Shares

```sql
SELECT 
    t.name AS tag_name,
    u1.username AS owner,
    u2.username AS shared_with,
    s.shared_at,
    s.expires_at
FROM tag_shares s
INNER JOIN tags t ON s.tag_id = t.id
INNER JOIN users u1 ON s.owner_id = u1.id
INNER JOIN users u2 ON s.shared_with_id = u2.id
WHERE s.revoked_at IS NULL
ORDER BY s.shared_at DESC;
```

---

## Troubleshooting

### Error: Circular Reference (50001)

**Cause:** Trying to set parent to own descendant

**Fix:**
```sql
-- Check ancestry before moving
SELECT * FROM tag_paths
WHERE ancestor_id = @tag_to_move
AND descendant_id = @new_parent;

-- If returns rows, it's circular
```

### Error: Tag Not Found (50005)

**Cause:** Tag deleted or wrong user_id

**Check:**
```sql
SELECT * FROM tags 
WHERE id = @tag_id;

-- Check if deleted
SELECT * FROM tags 
WHERE id = @tag_id 
AND deleted_at IS NOT NULL;
```

### Error: Cross-User Parent (50003)

**Cause:** Trying to set parent from different user

**Fix:** Tags can only have parents from same user

### Slow Queries

**Check execution plan:**
```sql
SET STATISTICS TIME ON;
SET STATISTICS IO ON;

-- Run your query
EXEC sp_get_tag_subtree @user_id = 1, @root_tag_id = 5;

SET STATISTICS TIME OFF;
SET STATISTICS IO OFF;
```

**Check missing indexes:**
```sql
SELECT 
    OBJECT_NAME(d.object_id) AS table_name,
    d.equality_columns,
    d.inequality_columns,
    d.included_columns
FROM sys.dm_db_missing_index_details d
WHERE OBJECT_NAME(d.object_id) LIKE 'tag%';
```

### Closure Table Out of Sync

**Symptoms:** Subtree queries return wrong results

**Fix:**
```sql
-- Rebuild from scratch
TRUNCATE TABLE tag_paths;

-- Re-trigger maintenance
UPDATE tags SET updated_at = GETDATE();
```

---

## Performance Tips

### Always Use Indexes

```sql
-- BAD: Full table scan
SELECT * FROM tags WHERE name = 'Work';

-- GOOD: Use user_id + name
SELECT * FROM tags 
WHERE user_id = 1 
AND name = 'Work';
```

### Use Closure Table for Subtree

```sql
-- BAD: Materialized path LIKE
SELECT * FROM tags
WHERE path LIKE 'Work.%';

-- GOOD: Closure table
SELECT t.* FROM tags t
INNER JOIN tag_paths p ON t.id = p.descendant_id
WHERE p.ancestor_id = @work_tag_id;
```

### Avoid N+1 Queries

```sql
-- BAD: Loop calling sp_get_tag_subtree
DECLARE @tag_id INT;
DECLARE tag_cursor CURSOR FOR 
    SELECT id FROM tags WHERE user_id = 1;
OPEN tag_cursor;
FETCH NEXT FROM tag_cursor INTO @tag_id;
WHILE @@FETCH_STATUS = 0
BEGIN
    EXEC sp_get_tag_subtree @user_id = 1, @root_tag_id = @tag_id;
    FETCH NEXT FROM tag_cursor INTO @tag_id;
END;

-- GOOD: Single query
SELECT t.*, p.depth
FROM tags t
INNER JOIN tag_paths p ON t.id = p.descendant_id
WHERE t.user_id = 1
ORDER BY p.ancestor_id, p.depth;
```

---

## Backup & Restore

### Backup

```sql
-- Full backup
BACKUP DATABASE SuperApp-dev
TO DISK = 'C:\Backups\SuperApp-dev.bak'
WITH FORMAT, INIT, COMPRESSION;

-- Backup specific tables
SELECT * INTO tags_backup FROM tags;
SELECT * INTO tag_paths_backup FROM tag_paths;
```

### Restore

```sql
-- Restore database
RESTORE DATABASE SuperApp-dev
FROM DISK = 'C:\Backups\SuperApp-dev.bak'
WITH REPLACE;

-- Restore specific tables
INSERT INTO tags SELECT * FROM tags_backup;
```

---

## Migration Script Template

```sql
-- Add new column
ALTER TABLE tags ADD new_column NVARCHAR(255) NULL;

-- Populate data
UPDATE tags SET new_column = 'default_value';

-- Make NOT NULL if needed
ALTER TABLE tags ALTER COLUMN new_column NVARCHAR(255) NOT NULL;

-- Add index
CREATE INDEX idx_tags_new_column ON tags(new_column);
```

---

## Quick Diagnostics

```sql
-- Database size
EXEC sp_spaceused 'tags';
EXEC sp_spaceused 'tag_paths';
EXEC sp_spaceused 'taggables';

-- Row counts
SELECT 
    'tags' AS table_name, COUNT(*) AS row_count FROM tags
UNION ALL
SELECT 'tag_paths', COUNT(*) FROM tag_paths
UNION ALL
SELECT 'taggables', COUNT(*) FROM taggables;

-- Index fragmentation
SELECT 
    OBJECT_NAME(ips.object_id) AS table_name,
    i.name AS index_name,
    ips.avg_fragmentation_in_percent
FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') ips
INNER JOIN sys.indexes i ON ips.object_id = i.object_id 
    AND ips.index_id = i.index_id
WHERE ips.avg_fragmentation_in_percent > 30
AND OBJECT_NAME(ips.object_id) LIKE 'tag%';
```

---

**For complete documentation, see [README.md](README.md)**