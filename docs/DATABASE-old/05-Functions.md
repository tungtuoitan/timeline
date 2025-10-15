# User-Defined Functions

Utility functions for permission checking and data operations.

---

## 1. Permission Functions

### 1.1 fn_can_access_tag

Check if user has access to a tag with specific permission.

```sql
CREATE OR ALTER FUNCTION fn_can_access_tag(
    @user_id INT,
    @tag_id INT,
    @permission NVARCHAR(20)  -- 'read', 'write', 'tag', 'untag', 'reshare'
)
RETURNS BIT
AS
BEGIN
    DECLARE @can_access BIT = 0;
    
    -- Owner always has full access
    IF EXISTS (
        SELECT 1 FROM tags 
        WHERE id = @tag_id 
        AND user_id = @user_id
        AND deleted_at IS NULL
    )
    BEGIN
        SET @can_access = 1;
        RETURN @can_access;
    END;
    
    -- Check share permissions
    IF @permission = 'read'
        SELECT @can_access = COALESCE(MAX(CAST(can_read AS INT)), 0)
        FROM tag_shares
        WHERE tag_id = @tag_id
        AND shared_with_id = @user_id
        AND revoked_at IS NULL
        AND (expires_at IS NULL OR expires_at > GETDATE());
    ELSE IF @permission = 'write'
        SELECT @can_access = COALESCE(MAX(CAST(can_write AS INT)), 0)
        FROM tag_shares
        WHERE tag_id = @tag_id
        AND shared_with_id = @user_id
        AND revoked_at IS NULL
        AND (expires_at IS NULL OR expires_at > GETDATE());
    ELSE IF @permission = 'tag'
        SELECT @can_access = COALESCE(MAX(CAST(can_tag AS INT)), 0)
        FROM tag_shares
        WHERE tag_id = @tag_id
        AND shared_with_id = @user_id
        AND revoked_at IS NULL
        AND (expires_at IS NULL OR expires_at > GETDATE());
    ELSE IF @permission = 'untag'
        SELECT @can_access = COALESCE(MAX(CAST(can_untag AS INT)), 0)
        FROM tag_shares
        WHERE tag_id = @tag_id
        AND shared_with_id = @user_id
        AND revoked_at IS NULL
        AND (expires_at IS NULL OR expires_at > GETDATE());
    ELSE IF @permission = 'reshare'
        SELECT @can_access = COALESCE(MAX(CAST(can_reshare AS INT)), 0)
        FROM tag_shares
        WHERE tag_id = @tag_id
        AND shared_with_id = @user_id
        AND revoked_at IS NULL
        AND (expires_at IS NULL OR expires_at > GETDATE());
    
    RETURN @can_access;
END;
GO
```

**Usage:**
```sql
-- Check if user 5 can tag with tag 10
SELECT dbo.fn_can_access_tag(5, 10, 'tag');  -- Returns 1 or 0

-- Use in WHERE clause
SELECT * FROM tags
WHERE dbo.fn_can_access_tag(5, id, 'read') = 1;
```

---

## 2. Utility Functions

### 2.1 fn_get_tag_depth

Get depth of a tag in tree.

```sql
CREATE OR ALTER FUNCTION fn_get_tag_depth(@tag_id INT)
RETURNS INT
AS
BEGIN
    DECLARE @depth INT;
    
    SELECT @depth = MAX(depth)
    FROM tag_paths
    WHERE descendant_id = @tag_id
    AND ancestor_id != @tag_id;
    
    RETURN COALESCE(@depth, 0);
END;
GO
```

**Usage:**
```sql
SELECT id, name, dbo.fn_get_tag_depth(id) AS depth
FROM tags
WHERE user_id = 1;
```

---

### 2.2 fn_get_tag_children_count

Count direct children of a tag.

```sql
CREATE OR ALTER FUNCTION fn_get_tag_children_count(@tag_id INT)
RETURNS INT
AS
BEGIN
    DECLARE @count INT;
    
    SELECT @count = COUNT(*)
    FROM tag_paths
    WHERE ancestor_id = @tag_id
    AND depth = 1;
    
    RETURN COALESCE(@count, 0);
END;
GO
```

---

### 2.3 fn_get_tag_descendants_count

Count all descendants (subtree size).

```sql
CREATE OR ALTER FUNCTION fn_get_tag_descendants_count(@tag_id INT)
RETURNS INT
AS
BEGIN
    DECLARE @count INT;
    
    SELECT @count = COUNT(*)
    FROM tag_paths
    WHERE ancestor_id = @tag_id
    AND depth > 0;  -- Exclude self
    
    RETURN COALESCE(@count, 0);
END;
GO
```

---

### 2.4 fn_is_tag_ancestor

Check if tag A is ancestor of tag B.

```sql
CREATE OR ALTER FUNCTION fn_is_tag_ancestor(
    @ancestor_id INT,
    @descendant_id INT
)
RETURNS BIT
AS
BEGIN
    DECLARE @is_ancestor BIT = 0;
    
    IF EXISTS (
        SELECT 1 FROM tag_paths
        WHERE ancestor_id = @ancestor_id
        AND descendant_id = @descendant_id
        AND depth > 0  -- Not self
    )
        SET @is_ancestor = 1;
    
    RETURN @is_ancestor;
END;
GO
```

---

## 3. Row-Level Security Functions

### 3.1 fn_filter_tags_by_user

Filter function for RLS (Row-Level Security).

```sql
CREATE OR ALTER FUNCTION fn_filter_tags_by_user(@user_id INT)
RETURNS TABLE
WITH SCHEMABINDING
AS
RETURN (
    SELECT 1 AS result
    WHERE @user_id = CAST(SESSION_CONTEXT(N'user_id') AS INT)
);
GO
```

**Enable RLS:**
```sql
CREATE SECURITY POLICY tags_security_policy
ADD FILTER PREDICATE dbo.fn_filter_tags_by_user(user_id)
ON dbo.tags
WITH (STATE = ON);
```

**Set session context:**
```sql
-- Before queries, set current user
EXEC sp_set_session_context @key = N'user_id', @value = 1;

-- Now all queries auto-filter by user_id
SELECT * FROM tags;  -- Only returns tags for user_id = 1
```

---

## 4. Aggregate Functions (Table-Valued)

### 4.1 fn_get_tag_statistics

Get comprehensive statistics for a tag.

```sql
CREATE OR ALTER FUNCTION fn_get_tag_statistics(@tag_id INT)
RETURNS TABLE
AS
RETURN (
    SELECT 
        t.id,
        t.name,
        t.user_id,
        dbo.fn_get_tag_depth(t.id) AS depth,
        dbo.fn_get_tag_children_count(t.id) AS children_count,
        dbo.fn_get_tag_descendants_count(t.id) AS descendants_count,
        (SELECT COUNT(*) FROM taggables WHERE tag_id = t.id) AS tagged_items,
        (SELECT COUNT(*) FROM tag_shares WHERE tag_id = t.id AND revoked_at IS NULL) AS active_shares,
        t.created_at,
        t.updated_at
    FROM tags t
    WHERE t.id = @tag_id
);
GO
```

**Usage:**
```sql
SELECT * FROM dbo.fn_get_tag_statistics(5);
```

---

## 5. Usage Examples

### Check Permissions Before Action

```sql
DECLARE @user_id INT = 5;
DECLARE @tag_id INT = 10;

IF dbo.fn_can_access_tag(@user_id, @tag_id, 'write') = 1
BEGIN
    -- User can write, proceed with update
    UPDATE tags SET color = '#FF0000' WHERE id = @tag_id;
END
ELSE
BEGIN
    PRINT 'Access denied';
END;
```

### Get Tag Tree with Depth

```sql
SELECT 
    id,
    name,
    path,
    dbo.fn_get_tag_depth(id) AS depth,
    dbo.fn_get_tag_children_count(id) AS children,
    dbo.fn_get_tag_descendants_count(id) AS total_descendants
FROM tags
WHERE user_id = 1
ORDER BY path;
```

### Check Circular Reference Before Move

```sql
DECLARE @tag_id INT = 5;
DECLARE @new_parent_id INT = 10;

IF dbo.fn_is_tag_ancestor(@tag_id, @new_parent_id) = 1
BEGIN
    PRINT 'Cannot move: would create circular reference';
END
ELSE
BEGIN
    -- Safe to move
    UPDATE tags SET parent_id = @new_parent_id WHERE id = @tag_id;
END;
```

---

**Next:** See [06-Sharing.md](06-Sharing.md) for sharing system details.
EOF
cat /home/claude/tag-tree-docs/05-Functions.md
Output

# User-Defined Functions

Utility functions for permission checking and data operations.

---

## 1. Permission Functions

### 1.1 fn_can_access_tag

Check if user has access to a tag with specific permission.

```sql
CREATE OR ALTER FUNCTION fn_can_access_tag(
    @user_id INT,
    @tag_id INT,
    @permission NVARCHAR(20)  -- 'read', 'write', 'tag', 'untag', 'reshare'
)
RETURNS BIT
AS
BEGIN
    DECLARE @can_access BIT = 0;
    
    -- Owner always has full access
    IF EXISTS (
        SELECT 1 FROM tags 
        WHERE id = @tag_id 
        AND user_id = @user_id
        AND deleted_at IS NULL
    )
    BEGIN
        SET @can_access = 1;
        RETURN @can_access;
    END;
    
    -- Check share permissions
    IF @permission = 'read'
        SELECT @can_access = COALESCE(MAX(CAST(can_read AS INT)), 0)
        FROM tag_shares
        WHERE tag_id = @tag_id
        AND shared_with_id = @user_id
        AND revoked_at IS NULL
        AND (expires_at IS NULL OR expires_at > GETDATE());
    ELSE IF @permission = 'write'
        SELECT @can_access = COALESCE(MAX(CAST(can_write AS INT)), 0)
        FROM tag_shares
        WHERE tag_id = @tag_id
        AND shared_with_id = @user_id
        AND revoked_at IS NULL
        AND (expires_at IS NULL OR expires_at > GETDATE());
    ELSE IF @permission = 'tag'
        SELECT @can_access = COALESCE(MAX(CAST(can_tag AS INT)), 0)
        FROM tag_shares
        WHERE tag_id = @tag_id
        AND shared_with_id = @user_id
        AND revoked_at IS NULL
        AND (expires_at IS NULL OR expires_at > GETDATE());
    ELSE IF @permission = 'untag'
        SELECT @can_access = COALESCE(MAX(CAST(can_untag AS INT)), 0)
        FROM tag_shares
        WHERE tag_id = @tag_id
        AND shared_with_id = @user_id
        AND revoked_at IS NULL
        AND (expires_at IS NULL OR expires_at > GETDATE());
    ELSE IF @permission = 'reshare'
        SELECT @can_access = COALESCE(MAX(CAST(can_reshare AS INT)), 0)
        FROM tag_shares
        WHERE tag_id = @tag_id
        AND shared_with_id = @user_id
        AND revoked_at IS NULL
        AND (expires_at IS NULL OR expires_at > GETDATE());
    
    RETURN @can_access;
END;
GO
```

**Usage:**
```sql
-- Check if user 5 can tag with tag 10
SELECT dbo.fn_can_access_tag(5, 10, 'tag');  -- Returns 1 or 0

-- Use in WHERE clause
SELECT * FROM tags
WHERE dbo.fn_can_access_tag(5, id, 'read') = 1;
```

---

## 2. Utility Functions

### 2.1 fn_get_tag_depth

Get depth of a tag in tree.

```sql
CREATE OR ALTER FUNCTION fn_get_tag_depth(@tag_id INT)
RETURNS INT
AS
BEGIN
    DECLARE @depth INT;
    
    SELECT @depth = MAX(depth)
    FROM tag_paths
    WHERE descendant_id = @tag_id
    AND ancestor_id != @tag_id;
    
    RETURN COALESCE(@depth, 0);
END;
GO
```

**Usage:**
```sql
SELECT id, name, dbo.fn_get_tag_depth(id) AS depth
FROM tags
WHERE user_id = 1;
```

---

### 2.2 fn_get_tag_children_count

Count direct children of a tag.

```sql
CREATE OR ALTER FUNCTION fn_get_tag_children_count(@tag_id INT)
RETURNS INT
AS
BEGIN
    DECLARE @count INT;
    
    SELECT @count = COUNT(*)
    FROM tag_paths
    WHERE ancestor_id = @tag_id
    AND depth = 1;
    
    RETURN COALESCE(@count, 0);
END;
GO
```

---

### 2.3 fn_get_tag_descendants_count

Count all descendants (subtree size).

```sql
CREATE OR ALTER FUNCTION fn_get_tag_descendants_count(@tag_id INT)
RETURNS INT
AS
BEGIN
    DECLARE @count INT;
    
    SELECT @count = COUNT(*)
    FROM tag_paths
    WHERE ancestor_id = @tag_id
    AND depth > 0;  -- Exclude self
    
    RETURN COALESCE(@count, 0);
END;
GO
```

---

### 2.4 fn_is_tag_ancestor

Check if tag A is ancestor of tag B.

```sql
CREATE OR ALTER FUNCTION fn_is_tag_ancestor(
    @ancestor_id INT,
    @descendant_id INT
)
RETURNS BIT
AS
BEGIN
    DECLARE @is_ancestor BIT = 0;
    
    IF EXISTS (
        SELECT 1 FROM tag_paths
        WHERE ancestor_id = @ancestor_id
        AND descendant_id = @descendant_id
        AND depth > 0  -- Not self
    )
        SET @is_ancestor = 1;
    
    RETURN @is_ancestor;
END;
GO
```

---

## 3. Row-Level Security Functions

### 3.1 fn_filter_tags_by_user

Filter function for RLS (Row-Level Security).

```sql
CREATE OR ALTER FUNCTION fn_filter_tags_by_user(@user_id INT)
RETURNS TABLE
WITH SCHEMABINDING
AS
RETURN (
    SELECT 1 AS result
    WHERE @user_id = CAST(SESSION_CONTEXT(N'user_id') AS INT)
);
GO
```

**Enable RLS:**
```sql
CREATE SECURITY POLICY tags_security_policy
ADD FILTER PREDICATE dbo.fn_filter_tags_by_user(user_id)
ON dbo.tags
WITH (STATE = ON);
```

**Set session context:**
```sql
-- Before queries, set current user
EXEC sp_set_session_context @key = N'user_id', @value = 1;

-- Now all queries auto-filter by user_id
SELECT * FROM tags;  -- Only returns tags for user_id = 1
```

---

## 4. Aggregate Functions (Table-Valued)

### 4.1 fn_get_tag_statistics

Get comprehensive statistics for a tag.

```sql
CREATE OR ALTER FUNCTION fn_get_tag_statistics(@tag_id INT)
RETURNS TABLE
AS
RETURN (
    SELECT 
        t.id,
        t.name,
        t.user_id,
        dbo.fn_get_tag_depth(t.id) AS depth,
        dbo.fn_get_tag_children_count(t.id) AS children_count,
        dbo.fn_get_tag_descendants_count(t.id) AS descendants_count,
        (SELECT COUNT(*) FROM taggables WHERE tag_id = t.id) AS tagged_items,
        (SELECT COUNT(*) FROM tag_shares WHERE tag_id = t.id AND revoked_at IS NULL) AS active_shares,
        t.created_at,
        t.updated_at
    FROM tags t
    WHERE t.id = @tag_id
);
GO
```

**Usage:**
```sql
SELECT * FROM dbo.fn_get_tag_statistics(5);
```

---

## 5. Usage Examples

### Check Permissions Before Action

```sql
DECLARE @user_id INT = 5;
DECLARE @tag_id INT = 10;

IF dbo.fn_can_access_tag(@user_id, @tag_id, 'write') = 1
BEGIN
    -- User can write, proceed with update
    UPDATE tags SET color = '#FF0000' WHERE id = @tag_id;
END
ELSE
BEGIN
    PRINT 'Access denied';
END;
```

### Get Tag Tree with Depth

```sql
SELECT 
    id,
    name,
    path,
    dbo.fn_get_tag_depth(id) AS depth,
    dbo.fn_get_tag_children_count(id) AS children,
    dbo.fn_get_tag_descendants_count(id) AS total_descendants
FROM tags
WHERE user_id = 1
ORDER BY path;
```

### Check Circular Reference Before Move

```sql
DECLARE @tag_id INT = 5;
DECLARE @new_parent_id INT = 10;

IF dbo.fn_is_tag_ancestor(@tag_id, @new_parent_id) = 1
BEGIN
    PRINT 'Cannot move: would create circular reference';
END
ELSE
BEGIN
    -- Safe to move
    UPDATE tags SET parent_id = @new_parent_id WHERE id = @tag_id;
END;
```

---

**Next:** See [06-Sharing.md](06-Sharing.md) for sharing system details.