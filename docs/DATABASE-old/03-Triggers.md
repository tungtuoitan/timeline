# Database Triggers

All triggers for maintaining data integrity and automation in the Tag-Tree system.

---

## Table of Contents

1. [Closure Table Maintenance](#1-closure-table-maintenance)
2. [Materialized Path Maintenance](#2-materialized-path-maintenance)
3. [Validation Triggers](#3-validation-triggers)
4. [Cascade Operations](#4-cascade-operations)

---

## 1. Closure Table Maintenance

### 1.1 trg_maintain_tag_closure

Automatically maintains the closure table when tags are inserted or updated.

```sql
CREATE OR ALTER TRIGGER trg_maintain_tag_closure
ON tags
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    -- =============================================
    -- Prevent circular reference
    -- =============================================
    IF EXISTS (
        SELECT 1 
        FROM inserted i
        WHERE i.parent_id IS NOT NULL
        AND i.id IN (
            SELECT ancestor_id 
            FROM tag_paths 
            WHERE descendant_id = i.parent_id
        )
    )
    BEGIN
        THROW 50001, 'Circular reference detected: Cannot set parent to own descendant', 1;
        RETURN;
    END;
    
    -- =============================================
    -- Prevent cross-user parent assignment
    -- =============================================
    IF EXISTS (
        SELECT 1
        FROM inserted i
        INNER JOIN tags p ON i.parent_id = p.id
        WHERE i.user_id <> p.user_id
    )
    BEGIN
        THROW 50003, 'Cannot set parent tag from different user', 1;
        RETURN;
    END;
    
    -- =============================================
    -- Handle INSERT: Add new paths
    -- =============================================
    IF NOT EXISTS (SELECT 1 FROM deleted)
    BEGIN
        -- Add self-reference (depth 0)
        INSERT INTO tag_paths (ancestor_id, descendant_id, depth)
        SELECT id, id, 0
        FROM inserted;
        
        -- Add paths from all ancestors of parent to new tag
        INSERT INTO tag_paths (ancestor_id, descendant_id, depth)
        SELECT 
            p.ancestor_id, 
            i.id, 
            p.depth + 1
        FROM inserted i
        INNER JOIN tag_paths p ON p.descendant_id = i.parent_id
        WHERE i.parent_id IS NOT NULL;
    END
    
    -- =============================================
    -- Handle UPDATE: parent_id changed
    -- =============================================
    ELSE IF EXISTS (
        SELECT 1 
        FROM inserted i
        INNER JOIN deleted d ON i.id = d.id
        WHERE ISNULL(i.parent_id, -1) <> ISNULL(d.parent_id, -1)
    )
    BEGIN
        -- Step 1: Delete old paths (except self-reference and subtree paths)
        DELETE p
        FROM tag_paths p
        INNER JOIN inserted i ON p.descendant_id = i.id
        WHERE p.ancestor_id <> p.descendant_id
        AND NOT EXISTS (
            -- Keep paths within subtree
            SELECT 1 FROM tag_paths sub
            WHERE sub.ancestor_id = i.id
            AND sub.descendant_id = p.descendant_id
        );
        
        -- Step 2: For moved tag and all its descendants, add new paths
        -- Get all descendants of moved tag
        DECLARE @descendants TABLE (descendant_id INT, depth_from_moved INT);
        
        INSERT INTO @descendants
        SELECT descendant_id, depth
        FROM tag_paths
        WHERE ancestor_id IN (SELECT id FROM inserted);
        
        -- Add paths from new ancestors to all descendants
        INSERT INTO tag_paths (ancestor_id, descendant_id, depth)
        SELECT 
            p.ancestor_id, 
            d.descendant_id, 
            p.depth + 1 + d.depth_from_moved
        FROM inserted i
        INNER JOIN tag_paths p ON p.descendant_id = i.parent_id
        CROSS JOIN @descendants d
        WHERE i.parent_id IS NOT NULL
        AND NOT EXISTS (
            SELECT 1 FROM tag_paths existing
            WHERE existing.ancestor_id = p.ancestor_id
            AND existing.descendant_id = d.descendant_id
        );
    END;
END;
GO
```

**What it does:**
1. **INSERT**: Adds self-reference + copies all ancestor paths
2. **UPDATE (move)**: Removes old paths, adds new paths for entire subtree
3. **Validation**: Prevents circular references and cross-user assignments

**Performance:**
- INSERT: O(depth) - adds ~5-10 rows typically
- UPDATE: O(old_ancestors × subtree_size) - can be slow for large subtrees

---

## 2. Materialized Path Maintenance

### 2.1 trg_maintain_tag_path

Automatically updates the materialized path column when tags are inserted or moved.

```sql
CREATE OR ALTER TRIGGER trg_maintain_tag_path
ON tags
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    -- =============================================
    -- Recursive CTE to update paths
    -- =============================================
    WITH TagHierarchy AS (
        -- Base case: Updated tags
        SELECT 
            i.id, 
            i.user_id,
            i.parent_id, 
            i.name,
            CASE 
                WHEN i.parent_id IS NULL THEN i.name
                ELSE p.path + '.' + i.name
            END AS new_path
        FROM inserted i
        LEFT JOIN tags p ON i.parent_id = p.id 
            AND i.user_id = p.user_id  -- Same user only
        
        UNION ALL
        
        -- Recursive case: All descendants
        SELECT 
            t.id, 
            t.user_id,
            t.parent_id, 
            t.name,
            th.new_path + '.' + t.name
        FROM tags t
        INNER JOIN TagHierarchy th ON t.parent_id = th.id 
            AND t.user_id = th.user_id  -- Same user only
    )
    UPDATE t
    SET 
        t.path = th.new_path, 
        t.updated_at = GETDATE()
    FROM tags t
    INNER JOIN TagHierarchy th ON t.id = th.id;
END;
GO
```

**What it does:**
- Recursively updates path for moved tag and all descendants
- Ensures paths reflect hierarchy: 'Work.ProjectX.Phase1'

**Example:**
```
Before move:
  Work (path: 'Work')
    └─ ProjectX (path: 'Work.ProjectX')
         └─ Phase1 (path: 'Work.ProjectX.Phase1')

Move ProjectX under Personal:

After move:
  Personal (path: 'Personal')
    └─ ProjectX (path: 'Personal.ProjectX')  ← Updated
         └─ Phase1 (path: 'Personal.ProjectX.Phase1')  ← Updated
```

---

## 3. Validation Triggers

### 3.1 trg_validate_taggable_user

Ensures tags can only be applied by authorized users.

```sql
CREATE OR ALTER TRIGGER trg_validate_taggable_user
ON taggables
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    -- =============================================
    -- Ensure tag belongs to same user OR user has permission
    -- =============================================
    IF EXISTS (
        SELECT 1
        FROM inserted i
        INNER JOIN tags t ON i.tag_id = t.id
        WHERE i.user_id <> t.user_id  -- Different user
        AND NOT EXISTS (
            -- Check if shared with permission
            SELECT 1 FROM tag_shares s
            WHERE s.tag_id = i.tag_id
            AND s.shared_with_id = i.user_id
            AND s.can_tag = 1
            AND s.revoked_at IS NULL
            AND (s.expires_at IS NULL OR s.expires_at > GETDATE())
        )
    )
    BEGIN
        THROW 50004, 'Cannot tag with another user''s tag without permission', 1;
    END;
    
    -- =============================================
    -- Log tagging action for audit
    -- =============================================
    INSERT INTO tag_share_audit (action, performed_by, details)
    SELECT 
        'tagged_item',
        i.created_by,
        (SELECT 
            tag_id = i.tag_id,
            taggable_id = i.taggable_id,
            taggable_type = i.taggable_type
         FOR JSON PATH, WITHOUT_ARRAY_WRAPPER)
    FROM inserted i;
END;
GO
```

**What it does:**
- Validates user owns tag OR has can_tag permission
- Logs all tagging operations for audit trail

---

### 3.2 trg_validate_tag_delete

Handles tag deletion (soft delete preferred).

```sql
CREATE OR ALTER TRIGGER trg_validate_tag_delete
ON tags
INSTEAD OF DELETE
AS
BEGIN
    SET NOCOUNT ON;
    
    -- =============================================
    -- Soft delete instead of hard delete
    -- =============================================
    UPDATE tags
    SET deleted_at = GETDATE()
    WHERE id IN (SELECT id FROM deleted);
    
    -- =============================================
    -- Cascade soft delete to all descendants
    -- =============================================
    UPDATE t
    SET t.deleted_at = GETDATE()
    FROM tags t
    INNER JOIN tag_paths p ON t.id = p.descendant_id
    WHERE p.ancestor_id IN (SELECT id FROM deleted)
    AND p.depth > 0  -- Not self
    AND t.deleted_at IS NULL;
    
    -- Note: Hard deletes (ON DELETE CASCADE) will handle
    -- tag_paths, taggables, and tag_shares automatically
END;
GO
```

**What it does:**
- Intercepts DELETE and converts to soft delete
- Cascades soft delete to entire subtree
- Preserves data for recovery

---

## 4. Cascade Operations

### 4.1 trg_cascade_tag_share_revoke

Auto-revokes child shares when parent is revoked.

```sql
CREATE OR ALTER TRIGGER trg_cascade_tag_share_revoke
ON tag_shares
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    -- =============================================
    -- When share is revoked, revoke all child tag shares
    -- =============================================
    IF EXISTS (
        SELECT 1 FROM inserted 
        WHERE revoked_at IS NOT NULL
    )
    BEGIN
        UPDATE s
        SET s.revoked_at = i.revoked_at
        FROM tag_shares s
        INNER JOIN inserted i ON s.tag_id IN (
            -- Find all descendant tags
            SELECT p.descendant_id
            FROM tag_paths p
            WHERE p.ancestor_id = i.tag_id
            AND p.depth > 0  -- Children only
        )
        WHERE s.owner_id = i.owner_id
        AND s.shared_with_id = i.shared_with_id
        AND s.revoked_at IS NULL
        AND i.include_children = 1;  -- Only if parent share includes children
        
        -- Log cascade revocation
        INSERT INTO tag_share_audit (action, performed_by, details)
        SELECT 
            'cascade_revoked',
            (SELECT TOP 1 performed_by FROM tag_share_audit 
             WHERE share_id = i.id ORDER BY performed_at DESC),
            (SELECT parent_share_id = i.id FOR JSON PATH, WITHOUT_ARRAY_WRAPPER)
        FROM inserted i
        WHERE i.revoked_at IS NOT NULL;
    END;
END;
GO
```

**What it does:**
- When parent tag share is revoked, auto-revoke all child tag shares
- Only applies if parent share had include_children = 1
- Logs cascade operations

---

## 5. Audit Triggers

### 5.1 trg_audit_tag_changes

Comprehensive audit log for tag changes.

```sql
CREATE OR ALTER TRIGGER trg_audit_tag_changes
ON tags
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @action NVARCHAR(50);
    DECLARE @user_id INT;
    
    -- Determine action
    IF EXISTS (SELECT 1 FROM inserted) AND EXISTS (SELECT 1 FROM deleted)
        SET @action = 'updated';
    ELSE IF EXISTS (SELECT 1 FROM inserted)
        SET @action = 'created';
    ELSE
        SET @action = 'deleted';
    
    -- Get user (from inserted or deleted)
    SELECT TOP 1 @user_id = COALESCE(i.created_by, d.user_id)
    FROM inserted i
    FULL OUTER JOIN deleted d ON 1=1;
    
    -- Log change
    INSERT INTO tag_share_audit (action, performed_by, details)
    SELECT 
        @action,
        @user_id,
        (SELECT 
            tag_id = COALESCE(i.id, d.id),
            tag_name = COALESCE(i.name, d.name),
            old_parent = d.parent_id,
            new_parent = i.parent_id
         FOR JSON PATH, WITHOUT_ARRAY_WRAPPER)
    FROM inserted i
    FULL OUTER JOIN deleted d ON i.id = d.id;
END;
GO
```

---

## 6. Trigger Installation Order

**Important:** Install triggers in this order to avoid dependency issues:

```sql
-- 1. Validation triggers (no dependencies)
-- Run: trg_validate_tag_delete
-- Run: trg_validate_taggable_user

-- 2. Maintenance triggers
-- Run: trg_maintain_tag_closure
-- Run: trg_maintain_tag_path

-- 3. Cascade triggers (depend on sharing tables)
-- Run: trg_cascade_tag_share_revoke

-- 4. Audit triggers (last)
-- Run: trg_audit_tag_changes
```

---

## 7. Trigger Testing

### Test Circular Reference Prevention

```sql
-- Setup
INSERT INTO tags (user_id, name) VALUES (1, 'A');  -- id 1
INSERT INTO tags (user_id, name, parent_id) VALUES (1, 'B', 1);  -- id 2
INSERT INTO tags (user_id, name, parent_id) VALUES (1, 'C', 2);  -- id 3

-- Test: Try to make A child of C (circular)
BEGIN TRY
    UPDATE tags SET parent_id = 3 WHERE id = 1;
    PRINT 'ERROR: Circular reference not prevented!';
END TRY
BEGIN CATCH
    PRINT 'SUCCESS: Circular reference prevented: ' + ERROR_MESSAGE();
END CATCH;
```

Expected: Error 50001

### Test Cross-User Prevention

```sql
-- Setup
INSERT INTO tags (user_id, name) VALUES (1, 'User1Tag');  -- id 1
INSERT INTO tags (user_id, name) VALUES (2, 'User2Tag');  -- id 2

-- Test: Try to make User2's tag child of User1's tag
BEGIN TRY
    UPDATE tags SET parent_id = 1 WHERE id = 2;
    PRINT 'ERROR: Cross-user parent not prevented!';
END TRY
BEGIN CATCH
    PRINT 'SUCCESS: Cross-user parent prevented: ' + ERROR_MESSAGE();
END CATCH;
```

Expected: Error 50003

### Test Path Updates

```sql
-- Setup
INSERT INTO tags (user_id, name) VALUES (1, 'Work');  -- id 1
INSERT INTO tags (user_id, name, parent_id) VALUES (1, 'ProjectX', 1);  -- id 2
INSERT INTO tags (user_id, name, parent_id) VALUES (1, 'Phase1', 2);  -- id 3

-- Check paths
SELECT id, name, path FROM tags WHERE user_id = 1;
-- Expected:
-- 1, Work, Work
-- 2, ProjectX, Work.ProjectX
-- 3, Phase1, Work.ProjectX.Phase1

-- Move ProjectX under root
UPDATE tags SET parent_id = NULL WHERE id = 2;

-- Check paths again
SELECT id, name, path FROM tags WHERE user_id = 1;
-- Expected:
-- 1, Work, Work
-- 2, ProjectX, ProjectX  ← Changed
-- 3, Phase1, ProjectX.Phase1  ← Changed
```

### Test Closure Table

```sql
-- Using same setup as above

-- Check closure table after initial inserts
SELECT * FROM tag_paths WHERE ancestor_id = 1 ORDER BY depth;
-- Expected:
-- 1, 1, 0  (Work → Work)
-- 1, 2, 1  (Work → ProjectX)
-- 1, 3, 2  (Work → Phase1)

-- After moving ProjectX to root
SELECT * FROM tag_paths WHERE ancestor_id = 1 ORDER BY depth;
-- Expected:
-- 1, 1, 0  (Work → Work only)

SELECT * FROM tag_paths WHERE ancestor_id = 2 ORDER BY depth;
-- Expected:
-- 2, 2, 0  (ProjectX → ProjectX)
-- 2, 3, 1  (ProjectX → Phase1)
```

---

## 8. Trigger Performance

### Monitoring

```sql
-- Find slow triggers
SELECT 
    OBJECT_NAME(object_id) AS trigger_name,
    total_elapsed_time / execution_count AS avg_time_ms,
    execution_count
FROM sys.dm_exec_trigger_stats
WHERE OBJECT_NAME(object_id) LIKE 'trg_%'
ORDER BY avg_time_ms DESC;
```

### Optimization Tips

1. **Avoid cursor-based logic** - Use set-based operations
2. **Minimize recursive CTEs** - Current design already optimal
3. **Index well** - Ensure tag_paths has proper indexes
4. **Consider async** - For large tree moves, queue background job

---

## 9. Troubleshooting

### Trigger Not Firing

```sql
-- Check if trigger is enabled
SELECT name, is_disabled 
FROM sys.triggers 
WHERE name LIKE 'trg_%';

-- Enable if disabled
ALTER TABLE tags ENABLE TRIGGER trg_maintain_tag_closure;
```

### Trigger Errors

```sql
-- View trigger definition
EXEC sp_helptext 'trg_maintain_tag_closure';

-- Drop and recreate
DROP TRIGGER IF EXISTS trg_maintain_tag_closure;
GO
-- Then run CREATE script
```

### Closure Table Out of Sync

```sql
-- Rebuild closure table from scratch
TRUNCATE TABLE tag_paths;

-- Reinsert all paths (will trigger maintenance)
UPDATE tags SET updated_at = GETDATE();
```

---

**Next:** See [04-Procedures.md](04-Procedures.md) for stored procedures.
