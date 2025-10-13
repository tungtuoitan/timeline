# Stored Procedures

Complete collection of stored procedures for tag management, tagging operations, querying, and sharing functionality.

---

## Table of Contents

- [Stored Procedures](#stored-procedures)
  - [Table of Contents](#table-of-contents)
  - [1. Tag Management](#1-tag-management)
    - [1.1 usp\_i\_tag](#11-usp_i_tag)
    - [1.2 usp\_u\_tag](#12-usp_u_tag)
    - [1.3 usp\_move\_tag](#13-usp_move_tag)
    - [1.4 usp\_d\_tag](#14-usp_d_tag)
    - [1.5 usp\_restore\_tag](#15-usp_restore_tag)
  - [2. Tagging Operations](#2-tagging-operations)
    - [2.1 usp\_tag\_item](#21-usp_tag_item)
    - [2.2 usp\_untag\_item](#22-usp_untag_item)
    - [2.3 usp\_bulk\_tag\_items](#23-usp_bulk_tag_items)
    - [2.4 usp\_replace\_tag](#24-usp_replace_tag)
  - [3. Query Operations](#3-query-operations)
    - [3.1 usp\_s\_user\_tags](#31-usp_s_user_tags)
    - [3.2 usp\_s\_tag\_subtree](#32-usp_s_tag_subtree)
    - [3.3 usp\_s\_tag\_breadcrumb](#33-usp_s_tag_breadcrumb)
    - [3.4 usp\_s\_tagged\_items](#34-usp_s_tagged_items)
    - [3.5 usp\_search\_tags](#35-usp_search_tags)
    - [3.6 usp\_s\_tag\_tree](#36-usp_s_tag_tree)
  - [4. Sharing Operations](#4-sharing-operations)
    - [4.1 usp\_share\_tag](#41-usp_share_tag)
    - [4.2 usp\_revoke\_tag\_share](#42-usp_revoke_tag_share)
    - [4.3 usp\_u\_tag\_share](#43-usp_u_tag_share)
    - [4.4 usp\_s\_tag\_shares](#44-usp_s_tag_shares)
    - [4.5 usp\_s\_shared\_tags](#45-usp_s_shared_tags)
    - [4.6 usp\_share\_tag\_with\_group](#46-usp_share_tag_with_group)

---

## 1. Tag Management

### 1.1 usp_i_tag

Create a new tag for a user with automatic slug generation and validation.

```sql
CREATE OR ALTER PROCEDURE usp_i_tag
    @user_id INT,
    @name NVARCHAR(255),
    @parent_id INT = NULL,
    @slug NVARCHAR(255) = NULL,
    @color NVARCHAR(7) = NULL,
    @icon NVARCHAR(50) = NULL,
    @description NVARCHAR(MAX) = NULL,
    @is_public BIT = 0,
    @public_slug NVARCHAR(255) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    
    BEGIN TRY
        -- =============================================
        -- Validate user exists
        -- =============================================
        IF NOT EXISTS (SELECT 1 FROM users WHERE id = @user_id)
        BEGIN
            THROW 50016, 'User not found', 1;
        END;
        
        -- =============================================
        -- Validate parent belongs to same user
        -- =============================================
        IF @parent_id IS NOT NULL
        AND NOT EXISTS (
            SELECT 1 FROM tags 
            WHERE id = @parent_id 
            AND user_id = @user_id
            AND deleted_at IS NULL
        )
        BEGIN
            THROW 50006, 'Parent tag not found or access denied', 1;
        END;
        
        -- =============================================
        -- Validate name is not empty
        -- =============================================
        IF LTRIM(RTRIM(@name)) = ''
        BEGIN
            THROW 50017, 'Tag name cannot be empty', 1;
        END;
        
        -- =============================================
        -- Auto-generate slug if not provided
        -- =============================================
        IF @slug IS NULL
        BEGIN
            -- Convert to lowercase, replace spaces with hyphens
            SET @slug = LOWER(REPLACE(LTRIM(RTRIM(@name)), ' ', '-'));
            
            -- Remove special characters (keep only alphanumeric and hyphens)
            SET @slug = REPLACE(REPLACE(REPLACE(REPLACE(@slug, '/', '-'), '\', '-'), '?', ''), '&', '');
            
            -- Ensure unique slug for user
            DECLARE @counter INT = 1;
            DECLARE @base_slug NVARCHAR(255) = @slug;
            
            WHILE EXISTS (
                SELECT 1 FROM tags 
                WHERE user_id = @user_id 
                AND slug = @slug
                AND deleted_at IS NULL
            )
            BEGIN
                SET @slug = @base_slug + '-' + CAST(@counter AS NVARCHAR);
                SET @counter = @counter + 1;
                
                -- Prevent infinite loop
                IF @counter > 1000
                BEGIN
                    THROW 50018, 'Unable to generate unique slug', 1;
                END;
            END;
        END
        ELSE
        BEGIN
            -- Validate provided slug is unique
            IF EXISTS (
                SELECT 1 FROM tags 
                WHERE user_id = @user_id 
                AND slug = @slug
                AND deleted_at IS NULL
            )
            BEGIN
                THROW 50019, 'Slug already exists for this user', 1;
            END;
        END;
        
        -- =============================================
        -- Validate color format (hex color)
        -- =============================================
        IF @color IS NOT NULL 
        AND @color NOT LIKE '#[0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f]'
        BEGIN
            THROW 50020, 'Invalid color format. Use hex format: #RRGGBB', 1;
        END;
        
        -- =============================================
        -- Insert tag (triggers will maintain closure + path)
        -- =============================================
        INSERT INTO tags (
            user_id, 
            name, 
            parent_id, 
            slug, 
            color, 
            icon, 
            description, 
            is_public, 
            public_slug,
            created_by
        )
        VALUES (
            @user_id, 
            LTRIM(RTRIM(@name)), 
            @parent_id, 
            @slug, 
            @color, 
            @icon, 
            @description,
            @is_public,
            @public_slug,
            @user_id
        );
        
        DECLARE @new_tag_id INT = SCOPE_IDENTITY();
        
        -- =============================================
        -- Return created tag
        -- =============================================
        SELECT 
            id,
            user_id,
            name,
            parent_id,
            path,
            slug,
            color,
            icon,
            description,
            is_public,
            created_at
        FROM tags 
        WHERE id = @new_tag_id;
        
        COMMIT;
    END TRY
    BEGIN CATCH
        ROLLBACK;
        THROW;
    END CATCH;
END;
GO
```

**Usage:**
```sql
-- Create root tag
EXEC usp_i_tag 
    @user_id = 1, 
    @name = 'Work Projects',
    @color = '#0066CC',
    @icon = 'briefcase',
    @description = 'All work-related projects';

-- Create child tag
DECLARE @parent_id INT;
SELECT @parent_id = id FROM tags WHERE user_id = 1 AND name = 'Work Projects';

EXEC usp_i_tag 
    @user_id = 1, 
    @name = 'Client A',
    @parent_id = @parent_id,
    @color = '#0099FF';
```

---

### 1.2 usp_u_tag

Update tag metadata (name, color, icon, description).

```sql
CREATE OR ALTER PROCEDURE usp_u_tag
    @user_id INT,
    @tag_id INT,
    @name NVARCHAR(255) = NULL,
    @color NVARCHAR(7) = NULL,
    @icon NVARCHAR(50) = NULL,
    @description NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    
    BEGIN TRY
        -- =============================================
        -- Validate ownership or write permission
        -- =============================================
        IF NOT EXISTS (
            SELECT 1 FROM tags 
            WHERE id = @tag_id 
            AND user_id = @user_id
            AND deleted_at IS NULL
        )
        AND NOT EXISTS (
            SELECT 1 FROM tag_shares
            WHERE tag_id = @tag_id
            AND shared_with_id = @user_id
            AND can_write = 1
            AND revoked_at IS NULL
            AND (expires_at IS NULL OR expires_at > GETDATE())
        )
        BEGIN
            THROW 50005, 'Tag not found or access denied', 1;
        END;
        
        -- =============================================
        -- Validate name if provided
        -- =============================================
        IF @name IS NOT NULL AND LTRIM(RTRIM(@name)) = ''
        BEGIN
            THROW 50017, 'Tag name cannot be empty', 1;
        END;
        
        -- =============================================
        -- Check name uniqueness if changing name
        -- =============================================
        IF @name IS NOT NULL
        BEGIN
            DECLARE @owner_id INT;
            SELECT @owner_id = user_id FROM tags WHERE id = @tag_id;
            
            IF EXISTS (
                SELECT 1 FROM tags 
                WHERE user_id = @owner_id 
                AND name = @name 
                AND id <> @tag_id
                AND deleted_at IS NULL
            )
            BEGIN
                THROW 50021, 'Tag name already exists for this user', 1;
            END;
        END;
        
        -- =============================================
        -- Validate color format if provided
        -- =============================================
        IF @color IS NOT NULL 
        AND @color <> ''
        AND @color NOT LIKE '#[0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f]'
        BEGIN
            THROW 50020, 'Invalid color format. Use hex format: #RRGGBB', 1;
        END;
        
        -- =============================================
        -- Update only non-NULL parameters
        -- =============================================
        UPDATE tags
        SET 
            name = CASE WHEN @name IS NOT NULL THEN LTRIM(RTRIM(@name)) ELSE name END,
            color = CASE WHEN @color IS NOT NULL THEN @color ELSE color END,
            icon = CASE WHEN @icon IS NOT NULL THEN @icon ELSE icon END,
            description = CASE WHEN @description IS NOT NULL THEN @description ELSE description END,
            updated_at = GETDATE()
        WHERE id = @tag_id;
        
        -- =============================================
        -- Return updated tag
        -- =============================================
        SELECT 
            id,
            user_id,
            name,
            parent_id,
            path,
            slug,
            color,
            icon,
            description,
            updated_at
        FROM tags 
        WHERE id = @tag_id;
        
        COMMIT;
    END TRY
    BEGIN CATCH
        ROLLBACK;
        THROW;
    END CATCH;
END;
GO
```

**Usage:**
```sql
-- Update tag color and icon
EXEC usp_u_tag 
    @user_id = 1,
    @tag_id = 5,
    @color = '#FF6600',
    @icon = 'star';

-- Update only name
EXEC usp_u_tag 
    @user_id = 1,
    @tag_id = 5,
    @name = 'Important Work';
```

---

### 1.3 usp_move_tag

Move tag to a new parent (or to root if parent_id is NULL).

```sql
CREATE OR ALTER PROCEDURE usp_move_tag
    @user_id INT,
    @tag_id INT,
    @new_parent_id INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    
    BEGIN TRY
        -- =============================================
        -- Validate ownership
        -- =============================================
        IF NOT EXISTS (
            SELECT 1 FROM tags 
            WHERE id = @tag_id 
            AND user_id = @user_id
            AND deleted_at IS NULL
        )
        BEGIN
            THROW 50005, 'Tag not found or access denied', 1;
        END;
        
        -- =============================================
        -- Validate new parent belongs to same user
        -- =============================================
        IF @new_parent_id IS NOT NULL
        AND NOT EXISTS (
            SELECT 1 FROM tags 
            WHERE id = @new_parent_id 
            AND user_id = @user_id
            AND deleted_at IS NULL
        )
        BEGIN
            THROW 50006, 'Parent tag not found or access denied', 1;
        END;
        
        -- =============================================
        -- Check if already at target parent
        -- =============================================
        DECLARE @current_parent_id INT;
        SELECT @current_parent_id = parent_id FROM tags WHERE id = @tag_id;
        
        IF ISNULL(@current_parent_id, -1) = ISNULL(@new_parent_id, -1)
        BEGIN
            -- No change needed
            SELECT 
                id, name, parent_id, path, updated_at 
            FROM tags 
            WHERE id = @tag_id;
            
            COMMIT;
            RETURN;
        END;
        
        -- =============================================
        -- Validate not moving to own descendant
        -- =============================================
        IF @new_parent_id IS NOT NULL
        AND EXISTS (
            SELECT 1 FROM tag_paths
            WHERE ancestor_id = @tag_id
            AND descendant_id = @new_parent_id
        )
        BEGIN
            THROW 50002, 'Cannot move tag to its own descendant', 1;
        END;
        
        -- =============================================
        -- Check depth limit (prevent too deep trees)
        -- =============================================
        IF @new_parent_id IS NOT NULL
        BEGIN
            DECLARE @new_depth INT;
            
            SELECT @new_depth = MAX(depth) + 1
            FROM tag_paths
            WHERE descendant_id = @new_parent_id;
            
            -- Get subtree depth of moving tag
            DECLARE @subtree_depth INT;
            SELECT @subtree_depth = MAX(depth)
            FROM tag_paths
            WHERE ancestor_id = @tag_id;
            
            -- Check total depth (current: max 20 levels)
            IF (@new_depth + @subtree_depth) > 20
            BEGIN
                THROW 50022, 'Maximum tag depth (20 levels) would be exceeded', 1;
            END;
        END;
        
        -- =============================================
        -- Update parent_id (triggers will handle closure table and paths)
        -- =============================================
        UPDATE tags
        SET 
            parent_id = @new_parent_id,
            updated_at = GETDATE()
        WHERE id = @tag_id;
        
        -- =============================================
        -- Return updated tag
        -- =============================================
        SELECT 
            id,
            user_id,
            name,
            parent_id,
            path,
            slug,
            color,
            icon,
            updated_at
        FROM tags 
        WHERE id = @tag_id;
        
        COMMIT;
    END TRY
    BEGIN CATCH
        ROLLBACK;
        THROW;
    END CATCH;
END;
GO
```

**Usage:**
```sql
-- Move tag to new parent
EXEC usp_move_tag 
    @user_id = 1,
    @tag_id = 5,
    @new_parent_id = 10;

-- Move tag to root (remove parent)
EXEC usp_move_tag 
    @user_id = 1,
    @tag_id = 5,
    @new_parent_id = NULL;
```

---

### 1.4 usp_d_tag

Soft delete a tag and optionally its descendants.

```sql
CREATE OR ALTER PROCEDURE usp_d_tag
    @user_id INT,
    @tag_id INT,
    @cascade BIT = 1  -- If 1, delete descendants; if 0, re-parent children to parent
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    
    BEGIN TRY
        -- =============================================
        -- Validate ownership
        -- =============================================
        IF NOT EXISTS (
            SELECT 1 FROM tags 
            WHERE id = @tag_id 
            AND user_id = @user_id
            AND deleted_at IS NULL
        )
        BEGIN
            THROW 50005, 'Tag not found or access denied', 1;
        END;
        
        -- =============================================
        -- Get parent for potential re-parenting
        -- =============================================
        DECLARE @parent_id INT;
        SELECT @parent_id = parent_id FROM tags WHERE id = @tag_id;
        
        IF @cascade = 0
        BEGIN
            -- =============================================
            -- Re-parent direct children to grandparent
            -- =============================================
            UPDATE tags
            SET 
                parent_id = @parent_id,
                updated_at = GETDATE()
            WHERE parent_id = @tag_id
            AND deleted_at IS NULL;
        END;
        
        -- =============================================
        -- Soft delete tag (and descendants if cascade)
        -- =============================================
        IF @cascade = 1
        BEGIN
            -- Delete tag and all descendants
            UPDATE t
            SET t.deleted_at = GETDATE()
            FROM tags t
            INNER JOIN tag_paths p ON t.id = p.descendant_id
            WHERE p.ancestor_id = @tag_id
            AND t.deleted_at IS NULL;
        END
        ELSE
        BEGIN
            -- Delete only this tag
            UPDATE tags
            SET deleted_at = GETDATE()
            WHERE id = @tag_id;
        END;
        
        -- =============================================
        -- Return summary
        -- =============================================
        DECLARE @deleted_count INT;
        SELECT @deleted_count = @@ROWCOUNT;
        
        SELECT 
            deleted_count = @deleted_count,
            cascade_applied = @cascade,
            message = CASE 
                WHEN @cascade = 1 THEN 'Tag and ' + CAST(@deleted_count - 1 AS NVARCHAR) + ' descendants deleted'
                ELSE 'Tag deleted, children re-parented'
            END;
        
        COMMIT;
    END TRY
    BEGIN CATCH
        ROLLBACK;
        THROW;
    END CATCH;
END;
GO
```

**Usage:**
```sql
-- Delete tag and all descendants
EXEC usp_d_tag 
    @user_id = 1,
    @tag_id = 5,
    @cascade = 1;

-- Delete tag but keep children (re-parent to grandparent)
EXEC usp_d_tag 
    @user_id = 1,
    @tag_id = 5,
    @cascade = 0;
```

---

### 1.5 usp_restore_tag

Restore a soft-deleted tag.

```sql
CREATE OR ALTER PROCEDURE usp_restore_tag
    @user_id INT,
    @tag_id INT,
    @restore_descendants BIT = 1
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    
    BEGIN TRY
        -- =============================================
        -- Validate ownership and tag is deleted
        -- =============================================
        IF NOT EXISTS (
            SELECT 1 FROM tags 
            WHERE id = @tag_id 
            AND user_id = @user_id
            AND deleted_at IS NOT NULL
        )
        BEGIN
            THROW 50023, 'Tag not found, not owned by user, or not deleted', 1;
        END;
        
        -- =============================================
        -- Check if parent is still available
        -- =============================================
        DECLARE @parent_id INT;
        SELECT @parent_id = parent_id FROM tags WHERE id = @tag_id;
        
        IF @parent_id IS NOT NULL
        AND NOT EXISTS (
            SELECT 1 FROM tags 
            WHERE id = @parent_id 
            AND deleted_at IS NULL
        )
        BEGIN
            -- Parent is deleted, set to root
            UPDATE tags SET parent_id = NULL WHERE id = @tag_id;
        END;
        
        -- =============================================
        -- Restore tag (and descendants if requested)
        -- =============================================
        IF @restore_descendants = 1
        BEGIN
            UPDATE t
            SET t.deleted_at = NULL
            FROM tags t
            INNER JOIN tag_paths p ON t.id = p.descendant_id
            WHERE p.ancestor_id = @tag_id
            AND t.user_id = @user_id;
        END
        ELSE
        BEGIN
            UPDATE tags
            SET deleted_at = NULL
            WHERE id = @tag_id;
        END;
        
        DECLARE @restored_count INT = @@ROWCOUNT;
        
        SELECT 
            restored_count = @restored_count,
            message = 'Tag restored successfully';
        
        COMMIT;
    END TRY
    BEGIN CATCH
        ROLLBACK;
        THROW;
    END CATCH;
END;
GO
```

---

## 2. Tagging Operations

### 2.1 usp_tag_item

Apply a tag to an item.

```sql
CREATE OR ALTER PROCEDURE usp_tag_item
    @user_id INT,
    @tag_id INT,
    @taggable_id BIGINT,
    @taggable_type NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    
    BEGIN TRY
        -- =============================================
        -- Validate tag ownership or permission
        -- =============================================
        DECLARE @tag_owner_id INT;
        SELECT @tag_owner_id = user_id 
        FROM tags 
        WHERE id = @tag_id AND deleted_at IS NULL;
        
        IF @tag_owner_id IS NULL
        BEGIN
            THROW 50005, 'Tag not found', 1;
        END;
        
        IF @tag_owner_id <> @user_id
        AND NOT EXISTS (
            SELECT 1 FROM tag_shares
            WHERE tag_id = @tag_id
            AND shared_with_id = @user_id
            AND can_tag = 1
            AND revoked_at IS NULL
            AND (expires_at IS NULL OR expires_at > GETDATE())
        )
        BEGIN
            THROW 50012, 'Permission denied: cannot tag with this tag', 1;
        END;
        
        -- =============================================
        -- Validate entity type
        -- =============================================
        IF NOT EXISTS (
            SELECT 1 FROM entity_types 
            WHERE type_name = @taggable_type 
            AND is_active = 1
        )
        BEGIN
            THROW 50007, 'Invalid entity type: ' + @taggable_type, 1;
        END;
        
        -- =============================================
        -- Check if already tagged
        -- =============================================
        IF EXISTS (
            SELECT 1 FROM taggables
            WHERE user_id = @tag_owner_id
            AND tag_id = @tag_id
            AND taggable_id = @taggable_id
            AND taggable_type = @taggable_type
        )
        BEGIN
            -- Already tagged, return success
            SELECT 
                tag_id = @tag_id,
                taggable_id = @taggable_id,
                taggable_type = @taggable_type,
                message = 'Item already tagged (no change)';
            
            COMMIT;
            RETURN;
        END;
        
        -- =============================================
        -- Insert tagging (use tag owner's user_id)
        -- =============================================
        INSERT INTO taggables (
            user_id, 
            tag_id, 
            taggable_id, 
            taggable_type, 
            created_by
        )
        VALUES (
            @tag_owner_id, 
            @tag_id, 
            @taggable_id, 
            @taggable_type, 
            @user_id
        );
        
        SELECT 
            id = SCOPE_IDENTITY(),
            tag_id = @tag_id,
            taggable_id = @taggable_id,
            taggable_type = @taggable_type,
            created_by = @user_id,
            message = 'Item tagged successfully';
        
        COMMIT;
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER() = 2627 -- Unique constraint violation
        BEGIN
            ROLLBACK;
            SELECT message = 'Item already tagged with this tag';
        END
        ELSE
        BEGIN
            ROLLBACK;
            THROW;
        END;
    END CATCH;
END;
GO
```

**Usage:**
```sql
-- Tag a note
EXEC usp_tag_item 
    @user_id = 1,
    @tag_id = 5,
    @taggable_id = 123,
    @taggable_type = 'Note';

-- Tag a file
EXEC usp_tag_item 
    @user_id = 1,
    @tag_id = 5,
    @taggable_id = 456,
    @taggable_type = 'File';
```

---

### 2.2 usp_untag_item

Remove a tag from an item.

```sql
CREATE OR ALTER PROCEDURE usp_untag_item
    @user_id INT,
    @tag_id INT,
    @taggable_id BIGINT,
    @taggable_type NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    
    BEGIN TRY
        -- =============================================
        -- Validate ownership or permission
        -- =============================================
        DECLARE @tag_owner_id INT;
        
        IF NOT EXISTS (
            SELECT 1 FROM taggables t
            INNER JOIN tags tg ON t.tag_id = tg.id
            WHERE t.tag_id = @tag_id
            AND t.taggable_id = @taggable_id
            AND t.taggable_type = @taggable_type
            AND (
                tg.user_id = @user_id 
                OR EXISTS (
                    SELECT 1 FROM tag_shares s
                    WHERE s.tag_id = @tag_id
                    AND s.shared_with_id = @user_id
                    AND s.can_untag = 1
                    AND s.revoked_at IS NULL
                    AND (s.expires_at IS NULL OR s.expires_at > GETDATE())
                )
                OR t.created_by = @user_id  -- Allow creator to untag
            )
        )
        BEGIN
            THROW 50013, 'Permission denied or tagging not found', 1;
        END;
        
        -- =============================================
        -- Delete tagging
        -- =============================================
        DELETE FROM taggables
        WHERE tag_id = @tag_id
        AND taggable_id = @taggable_id
        AND taggable_type = @taggable_type;
        
        IF @@ROWCOUNT = 0
        BEGIN
            THROW 50024, 'Tagging not found', 1;
        END;
        
        SELECT 
            tag_id = @tag_id,
            taggable_id = @taggable_id,
            taggable_type = @taggable_type,
            message = 'Tag removed successfully';
        
        COMMIT;
    END TRY
    BEGIN CATCH
        ROLLBACK;
        THROW;
    END CATCH;
END;
GO
```

---

### 2.3 usp_bulk_tag_items

Tag multiple items at once.

```sql
CREATE OR ALTER PROCEDURE usp_bulk_tag_items
    @user_id INT,
    @tag_id INT,
    @items NVARCHAR(MAX)  -- JSON array: [{"id": 123, "type": "Note"}, ...]
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    
    BEGIN TRY
        -- =============================================
        -- Validate tag access
        -- =============================================
        DECLARE @tag_owner_id INT;
        SELECT @tag_owner_id = user_id 
        FROM tags 
        WHERE id = @tag_id AND deleted_at IS NULL;
        
        IF @tag_owner_id IS NULL
        BEGIN
            THROW 50005, 'Tag not found', 1;
        END;
        
        IF @tag_owner_id <> @user_id
        AND NOT EXISTS (
            SELECT 1 FROM tag_shares
            WHERE tag_id = @tag_id
            AND shared_with_id = @user_id
            AND can_tag = 1
            AND revoked_at IS NULL
        )
        BEGIN
            THROW 50012, 'Permission denied: cannot tag with this tag', 1;
        END;
        
        -- =============================================
        -- Parse JSON and insert tags
        -- =============================================
        INSERT INTO taggables (user_id, tag_id, taggable_id, taggable_type, created_by)
        SELECT 
            @tag_owner_id,
            @tag_id,
            CAST(JSON_VALUE(value, '$.id') AS BIGINT),
            JSON_VALUE(value, '$.type'),
            @user_id
        FROM OPENJSON(@items)
        WHERE NOT EXISTS (
            -- Skip if already tagged
            SELECT 1 FROM taggables
            WHERE user_id = @tag_owner_id
            AND tag_id = @tag_id
            AND taggable_id = CAST(JSON_VALUE(value, '$.id') AS BIGINT)
            AND taggable_type = JSON_VALUE(value, '$.type')
        );
        
        DECLARE @tagged_count INT = @@ROWCOUNT;
        
        SELECT 
            tagged_count = @tagged_count,
            message = CAST(@tagged_count AS NVARCHAR) + ' items tagged successfully';
        
        COMMIT;
    END TRY
    BEGIN CATCH
        ROLLBACK;
        THROW;
    END CATCH;
END;
GO
```

**Usage:**
```sql
-- Tag multiple items
EXEC usp_bulk_tag_items 
    @user_id = 1,
    @tag_id = 5,
    @items = '[
        {"id": 123, "type": "Note"},
        {"id": 124, "type": "Note"},
        {"id": 456, "type": "File"}
    ]';
```

---

### 2.4 usp_replace_tag

Replace one tag with another on all items.

```sql
CREATE OR ALTER PROCEDURE usp_replace_tag
    @user_id INT,
    @old_tag_id INT,
    @new_tag_id INT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    
    BEGIN TRY
        -- =============================================
        -- Validate ownership of both tags
        -- =============================================
        IF NOT EXISTS (
            SELECT 1 FROM tags 
            WHERE id = @old_tag_id 
            AND user_id = @user_id
            AND deleted_at IS NULL
        )
        OR NOT EXISTS (
            SELECT 1 FROM tags 
            WHERE id = @new_tag_id 
            AND user_id = @user_id
            AND deleted_at IS NULL
        )
        BEGIN
            THROW 50005, 'One or both tags not found or access denied', 1;
        END;
        
        -- =============================================
        -- Update taggables
        -- =============================================
        UPDATE t
        SET t.tag_id = @new_tag_id
        FROM taggables t
        WHERE t.tag_id = @old_tag_id
        AND t.user_id = @user_id
        AND NOT EXISTS (
            -- Skip if new tag already applied
            SELECT 1 FROM taggables existing
            WHERE existing.user_id = t.user_id
            AND existing.tag_id = @new_tag_id
            AND existing.taggable_id = t.taggable_id
            AND existing.taggable_type = t.taggable_type
        );
        
        DECLARE @replaced_count INT = @@ROWCOUNT;
        
        -- =============================================
        -- Delete duplicates (items that had both tags)
        -- =============================================
        DELETE FROM taggables
        WHERE tag_id = @old_tag_id
        AND user_id = @user_id;
        
        SELECT 
            replaced_count = @replaced_count,
            message = 'Tag replaced on ' + CAST(@replaced_count AS NVARCHAR) + ' items';
        
        COMMIT;
    END TRY
    BEGIN CATCH
        ROLLBACK;
        THROW;
    END CATCH;
END;
GO
```

---

## 3. Query Operations

### 3.1 usp_s_user_tags

Get all tags for a user in tree format.

```sql
CREATE OR ALTER PROCEDURE usp_s_user_tags
    @user_id INT,
    @include_deleted BIT = 0,
    @include_shared BIT = 1,
    @parent_id INT = NULL  -- NULL = all, specific ID = children only
AS
BEGIN
    SET NOCOUNT ON;
    
    -- =============================================
    -- Own tags
    -- =============================================
    SELECT 
        t.id,
        t.user_id,
        t.name,
        t.parent_id,
        t.path,
        t.slug,
        t.color,
        t.icon,
        t.description,
        'owner' AS access_type,
        CAST(1 AS BIT) AS can_read,
        CAST(1 AS BIT) AS can_write,
        CAST(1 AS BIT) AS can_tag,
        CAST(1 AS BIT) AS can_untag,
        CAST(1 AS BIT) AS can_delete,
        COUNT(tg.id) AS usage_count,
        dbo.fn_get_tag_children_count(t.id) AS children_count,
        t.created_at,
        t.updated_at,
        t.deleted_at
    FROM tags t
    LEFT JOIN taggables tg ON tg.tag_id = t.id
    WHERE t.user_id = @user_id
    AND (@include_deleted = 1 OR t.deleted_at IS NULL)
    AND (@parent_id IS NULL OR t.parent_id = @parent_id OR (@parent_id = 0 AND t.parent_id IS NULL))
    GROUP BY t.id, t.user_id, t.name, t.parent_id, t.path, t.slug, t.color, 
             t.icon, t.description, t.created_at, t.updated_at, t.deleted_at
    
    UNION ALL
    
    -- =============================================
    -- Shared tags
    -- =============================================
    SELECT 
        t.id,
        t.user_id,
        t.name,
        t.parent_id,
        t.path,
        t.slug,
        t.color,
        t.icon,
        t.description,
        'shared' AS access_type,
        s.can_read,
        s.can_write,
        s.can_tag,
        s.can_untag,
        CAST(0 AS BIT) AS can_delete,
        COUNT(tg.id) AS usage_count,
        dbo.fn_get_tag_children_count(t.id) AS children_count,
        t.created_at,
        t.updated_at,
        NULL AS deleted_at
    FROM tags t
    INNER JOIN tag_shares s ON t.id = s.tag_id
    LEFT JOIN taggables tg ON tg.tag_id = t.id
    WHERE @include_shared = 1
    AND s.shared_with_id = @user_id
    AND s.revoked_at IS NULL
    AND (s.expires_at IS NULL OR s.expires_at > GETDATE())
    AND t.deleted_at IS NULL
    AND (@parent_id IS NULL OR t.parent_id = @parent_id OR (@parent_id = 0 AND t.parent_id IS NULL))
    GROUP BY t.id, t.user_id, t.name, t.parent_id, t.path, t.slug, t.color, 
             t.icon, t.description, s.can_read, s.can_write, s.can_tag, 
             s.can_untag, t.created_at, t.updated_at
    
    ORDER BY access_type, path;
END;
GO
```

**Usage:**
```sql
-- Get all tags
EXEC usp_s_user_tags @user_id = 1;

-- Get only root tags
EXEC usp_s_user_tags @user_id = 1, @parent_id = 0;

-- Get children of specific tag
EXEC usp_s_user_tags @user_id = 1, @parent_id = 5;

-- Include deleted tags
EXEC usp_s_user_tags @user_id = 1, @include_deleted = 1;
```

---

### 3.2 usp_s_tag_subtree

Get tag and all its descendants with depth information.

```sql
CREATE OR ALTER PROCEDURE usp_s_tag_subtree
    @user_id INT,
    @root_tag_id INT,
    @max_depth INT = NULL  -- NULL = unlimited
AS
BEGIN
    SET NOCOUNT ON;
    
    -- =============================================
    -- Validate access
    -- =============================================
    IF NOT EXISTS (
        SELECT 1 FROM tags 
        WHERE id = @root_tag_id 
        AND (
            user_id = @user_id 
            OR EXISTS (
                SELECT 1 FROM tag_shares
                WHERE tag_id = @root_tag_id
                AND shared_with_id = @user_id
                AND can_read = 1
                AND revoked_at IS NULL
            )
        )
        AND deleted_at IS NULL
    )
    BEGIN
        THROW 50005, 'Tag not found or access denied', 1;
    END;
    
    -- =============================================
    -- Get subtree
    -- =============================================
    SELECT 
        t.id,
        t.user_id,
        t.name,
        t.parent_id,
        t.path,
        t.slug,
        t.color,
        t.icon,
        t.description,
        p.depth,
        COUNT(tg.id) AS item_count,
        dbo.fn_get_tag_children_count(t.id) AS children_count,
        t.created_at,
        t.updated_at
    FROM tags t
    INNER JOIN tag_paths p ON t.id = p.descendant_id
    LEFT JOIN taggables tg ON tg.tag_id = t.id
    WHERE p.ancestor_id = @root_tag_id
    AND t.deleted_at IS NULL
    AND (@max_depth IS NULL OR p.depth <= @max_depth)
    GROUP BY t.id, t.user_id, t.name, t.parent_id, t.path, t.slug, 
             t.color, t.icon, t.description, p.depth, 
             t.created_at, t.updated_at
    ORDER BY p.depth, t.name;
END;
GO
```

**Usage:**
```sql
-- Get full subtree
EXEC usp_s_tag_subtree 
    @user_id = 1,
    @root_tag_id = 5;

-- Get only 2 levels deep
EXEC usp_s_tag_subtree 
    @user_id = 1,
    @root_tag_id = 5,
    @max_depth = 2;
```

---

### 3.3 usp_s_tag_breadcrumb

Get breadcrumb path (all ancestors) for a tag.

```sql
CREATE OR ALTER PROCEDURE usp_s_tag_breadcrumb
    @tag_id INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        t.id,
        t.name,
        t.slug,
        t.color,
        t.icon,
        p.depth
    FROM tags t
    INNER JOIN tag_paths p ON t.id = p.ancestor_id
    WHERE p.descendant_id = @tag_id
    AND t.deleted_at IS NULL
    ORDER BY p.depth;
END;
GO
```

**Usage:**
```sql
EXEC usp_s_tag_breadcrumb @tag_id = 15;
-- Returns: Root (depth 0) → Parent (depth 1) → Grandparent (depth 2) → Current Tag (depth 3)
```

---

### 3.4 usp_s_tagged_items

Find all items tagged with specific tag(s).

```sql
CREATE OR ALTER PROCEDURE usp_s_tagged_items
    @user_id INT,
    @tag_id INT = NULL,
    @tag_ids NVARCHAR(MAX) = NULL,  -- JSON array: [1, 5, 10]
    @include_subtree BIT = 1,
    @entity_type NVARCHAR(50) = NULL,
    @limit INT = 100,
    @offset INT = 0
AS
BEGIN
    SET NOCOUNT ON;
    
    -- =============================================
    -- Build tag ID list
    -- =============================================
    DECLARE @tag_list TABLE (tag_id INT);
    
    IF @tag_id IS NOT NULL
    BEGIN
        INSERT INTO @tag_list (tag_id) VALUES (@tag_id);
    END
    ELSE IF @tag_ids IS NOT NULL
    BEGIN
        INSERT INTO @tag_list (tag_id)
        SELECT CAST(value AS INT)
        FROM OPENJSON(@tag_ids);
    END
    ELSE
    BEGIN
        THROW 50025, 'Either tag_id or tag_ids must be provided', 1;
    END;
    
    -- =============================================
    -- Expand to subtree if requested
    -- =============================================
    IF @include_subtree = 1
    BEGIN
        INSERT INTO @tag_list (tag_id)
        SELECT DISTINCT p.descendant_id
        FROM tag_paths p
        INNER JOIN @tag_list t ON p.ancestor_id = t.tag_id
        WHERE p.depth > 0  -- Exclude self-references (already in list)
        AND NOT EXISTS (
            SELECT 1 FROM @tag_list existing
            WHERE existing.tag_id = p.descendant_id
        );
    END;
    
    -- =============================================
    -- Query tagged items
    -- =============================================
    SELECT 
        tg.taggable_id,
        tg.taggable_type,
        tg.created_at AS tagged_at,
        tg.created_by AS tagged_by,
        t.id AS tag_id,
        t.name AS tag_name,
        t.path AS tag_path,
        t.color AS tag_color,
        u.username AS tagged_by_username
    FROM taggables tg
    INNER JOIN @tag_list tl ON tg.tag_id = tl.tag_id
    INNER JOIN tags t ON tg.tag_id = t.id
    LEFT JOIN users u ON tg.created_by = u.id
    WHERE (@entity_type IS NULL OR tg.taggable_type = @entity_type)
    AND (
        t.user_id = @user_id
        OR EXISTS (
            SELECT 1 FROM tag_shares s
            WHERE s.tag_id = t.id
            AND s.shared_with_id = @user_id
            AND s.can_read = 1
            AND s.revoked_at IS NULL
        )
    )
    ORDER BY tg.created_at DESC
    OFFSET @offset ROWS
    FETCH NEXT @limit ROWS ONLY;
END;
GO
```

**Usage:**
```sql
-- Get items tagged with tag 5 and its children
EXEC usp_s_tagged_items 
    @user_id = 1,
    @tag_id = 5,
    @include_subtree = 1;

-- Get items tagged with multiple tags
EXEC usp_s_tagged_items 
    @user_id = 1,
    @tag_ids = '[5, 10, 15]',
    @include_subtree = 0;

-- Get only Notes tagged with tag 5
EXEC usp_s_tagged_items 
    @user_id = 1,
    @tag_id = 5,
    @entity_type = 'Note';

-- Pagination
EXEC usp_s_tagged_items 
    @user_id = 1,
    @tag_id = 5,
    @limit = 20,
    @offset = 0;
```

---

### 3.5 usp_search_tags

Search tags by name, path, or slug with autocomplete support.

```sql
CREATE OR ALTER PROCEDURE usp_search_tags
    @user_id INT,
    @query NVARCHAR(255),
    @limit INT = 20,
    @include_shared BIT = 1
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT TOP (@limit)
        t.id,
        t.user_id,
        t.name,
        t.path,
        t.slug,
        t.color,
        t.icon,
        t.description,
        CASE 
            WHEN t.user_id = @user_id THEN 'owner'
            ELSE 'shared'
        END AS access_type,
        COUNT(tg.id) AS usage_count,
        dbo.fn_get_tag_children_count(t.id) AS children_count,
        -- Relevance score
        CASE
            WHEN LOWER(t.name) = LOWER(@query) THEN 100
            WHEN LOWER(t.name) LIKE LOWER(@query) + '%' THEN 90
            WHEN LOWER(t.slug) = LOWER(@query) THEN 80
            WHEN LOWER(t.name) LIKE '%' + LOWER(@query) + '%' THEN 70
            WHEN LOWER(t.path) LIKE '%' + LOWER(@query) + '%' THEN 60
            ELSE 50
        END AS relevance_score
    FROM tags t
    LEFT JOIN taggables tg ON tg.tag_id = t.id
    WHERE t.deleted_at IS NULL
    AND (
        (t.user_id = @user_id)
        OR (
            @include_shared = 1
            AND EXISTS (
                SELECT 1 FROM tag_shares s
                WHERE s.tag_id = t.id
                AND s.shared_with_id = @user_id
                AND s.can_read = 1
                AND s.revoked_at IS NULL
            )
        )
    )
    AND (
        t.name LIKE '%' + @query + '%'
        OR t.path LIKE '%' + @query + '%'
        OR t.slug LIKE '%' + @query + '%'
    )
    GROUP BY t.id, t.user_id, t.name, t.path, t.slug, t.color, t.icon, t.description
    ORDER BY 
        -- Sort by relevance, then usage
        CASE
            WHEN LOWER(t.name) = LOWER(@query) THEN 100
            WHEN LOWER(t.name) LIKE LOWER(@query) + '%' THEN 90
            WHEN LOWER(t.slug) = LOWER(@query) THEN 80
            WHEN LOWER(t.name) LIKE '%' + LOWER(@query) + '%' THEN 70
            WHEN LOWER(t.path) LIKE '%' + LOWER(@query) + '%' THEN 60
            ELSE 50
        END DESC,
        COUNT(tg.id) DESC,
        t.name;
END;
GO
```

**Usage:**
```sql
-- Search for tags
EXEC usp_search_tags 
    @user_id = 1,
    @query = 'project',
    @limit = 10;

-- Search only own tags
EXEC usp_search_tags 
    @user_id = 1,
    @query = 'work',
    @include_shared = 0;
```

---

### 3.6 usp_s_tag_tree

Get complete tag tree in hierarchical format (for UI tree components).

```sql
CREATE OR ALTER PROCEDURE usp_s_tag_tree
    @user_id INT,
    @include_shared BIT = 1
AS
BEGIN
    SET NOCOUNT ON;
    
    -- =============================================
    -- Get all accessible tags with hierarchy info
    -- =============================================
    WITH TagHierarchy AS (
        -- Root tags
        SELECT 
            t.id,
            t.user_id,
            t.name,
            t.parent_id,
            t.path,
            t.slug,
            t.color,
            t.icon,
            CASE WHEN t.user_id = @user_id THEN 'owner' ELSE 'shared' END AS access_type,
            0 AS level,
            CAST(t.name AS NVARCHAR(4000)) AS sort_path
        FROM tags t
        WHERE t.deleted_at IS NULL
        AND t.parent_id IS NULL
        AND (
            t.user_id = @user_id
            OR (
                @include_shared = 1
                AND EXISTS (
                    SELECT 1 FROM tag_shares s
                    WHERE s.tag_id = t.id
                    AND s.shared_with_id = @user_id
                    AND s.can_read = 1
                    AND s.revoked_at IS NULL
                )
            )
        )
        
        UNION ALL
        
        -- Child tags
        SELECT 
            t.id,
            t.user_id,
            t.name,
            t.parent_id,
            t.path,
            t.slug,
            t.color,
            t.icon,
            CASE WHEN t.user_id = @user_id THEN 'owner' ELSE 'shared' END,
            th.level + 1,
            CAST(th.sort_path + '|' + t.name AS NVARCHAR(4000))
        FROM tags t
        INNER JOIN TagHierarchy th ON t.parent_id = th.id
        WHERE t.deleted_at IS NULL
    )
    SELECT 
        th.id,
        th.user_id,
        th.name,
        th.parent_id,
        th.path,
        th.slug,
        th.color,
        th.icon,
        th.access_type,
        th.level,
        COUNT(tg.id) AS usage_count,
        dbo.fn_get_tag_children_count(th.id) AS children_count
    FROM TagHierarchy th
    LEFT JOIN taggables tg ON tg.tag_id = th.id
    GROUP BY th.id, th.user_id, th.name, th.parent_id, th.path, th.slug, 
             th.color, th.icon, th.access_type, th.level, th.sort_path
    ORDER BY th.sort_path;
END;
GO
```

**Usage:**
```sql
-- Get full tree
EXEC usp_s_tag_tree @user_id = 1;

-- Get only own tags tree
EXEC usp_s_tag_tree @user_id = 1, @include_shared = 0;
```

---

## 4. Sharing Operations

### 4.1 usp_share_tag

Share a tag with another user with granular permissions.

```sql
CREATE OR ALTER PROCEDURE usp_share_tag
    @owner_id INT,
    @tag_id INT,
    @recipient_id INT,
    @can_read BIT = 1,
    @can_write BIT = 0,
    @can_tag BIT = 0,
    @can_untag BIT = 0,
    @can_reshare BIT = 0,
    @include_children BIT = 1,
    @expires_at DATETIME = NULL,
    @note NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    
    BEGIN TRY
        -- =============================================
        -- Validate owner owns the tag
        -- =============================================
        IF NOT EXISTS (
            SELECT 1 FROM tags 
            WHERE id = @tag_id 
            AND user_id = @owner_id
            AND deleted_at IS NULL
        )
        BEGIN
            THROW 50010, 'Tag not found or access denied', 1;
        END;
        
        -- =============================================
        -- Validate recipient exists
        -- =============================================
        IF NOT EXISTS (SELECT 1 FROM users WHERE id = @recipient_id)
        BEGIN
            THROW 50026, 'Recipient user not found', 1;
        END;
        
        -- =============================================
        -- Prevent self-sharing
        -- =============================================
        IF @owner_id = @recipient_id
        BEGIN
            THROW 50011, 'Cannot share tag with yourself', 1;
        END;
        
        -- =============================================
        -- Validate expiration date
        -- =============================================
        IF @expires_at IS NOT NULL AND @expires_at <= GETDATE()
        BEGIN
            THROW 50027, 'Expiration date must be in the future', 1;
        END;
        
        -- =============================================
        -- Insert or update share
        -- =============================================
        MERGE INTO tag_shares AS target
        USING (
            SELECT 
                @tag_id AS tag_id,
                @owner_id AS owner_id,
                @recipient_id AS shared_with_id
        ) AS source
        ON target.tag_id = source.tag_id
        AND target.owner_id = source.owner_id
        AND target.shared_with_id = source.shared_with_id
        WHEN MATCHED THEN
            UPDATE SET
                can_read = @can_read,
                can_write = @can_write,
                can_tag = @can_tag,
                can_untag = @can_untag,
                can_reshare = @can_reshare,
                include_children = @include_children,
                expires_at = @expires_at,
                revoked_at = NULL,
                shared_by = @owner_id
        WHEN NOT MATCHED THEN
            INSERT (
                tag_id, owner_id, shared_with_id, 
                can_read, can_write, can_tag, can_untag, can_reshare, 
                include_children, expires_at, shared_by
            )
            VALUES (
                @tag_id, @owner_id, @recipient_id,
                @can_read, @can_write, @can_tag, @can_untag, @can_reshare,
                @include_children, @expires_at, @owner_id
            );
        
        DECLARE @share_id BIGINT;
        SELECT @share_id = id 
        FROM tag_shares
        WHERE tag_id = @tag_id 
        AND owner_id = @owner_id 
        AND shared_with_id = @recipient_id;
        
        -- =============================================
        -- Audit log
        -- =============================================
        INSERT INTO tag_share_audit (share_id, action, performed_by, details)
        VALUES (
            @share_id,
            'share_created', 
            @owner_id,
            (SELECT 
                tag_id = @tag_id, 
                recipient_id = @recipient_id,
                note = @note
             FOR JSON PATH, WITHOUT_ARRAY_WRAPPER)
        );
        
        -- =============================================
        -- Return share details
        -- =============================================
        SELECT 
            s.id,
            s.tag_id,
            t.name AS tag_name,
            s.shared_with_id,
            u.username AS shared_with_username,
            s.can_read,
            s.can_write,
            s.can_tag,
            s.can_untag,
            s.can_reshare,
            s.include_children,
            s.shared_at,
            s.expires_at,
            message = 'Tag shared successfully'
        FROM tag_shares s
        INNER JOIN tags t ON s.tag_id = t.id
        INNER JOIN users u ON s.shared_with_id = u.id
        WHERE s.id = @share_id;
        
        COMMIT;
    END TRY
    BEGIN CATCH
        ROLLBACK;
        THROW;
    END CATCH;
END;
GO
```

**Usage:**
```sql
-- Share tag with read-only access
EXEC usp_share_tag 
    @owner_id = 1,
    @tag_id = 5,
    @recipient_id = 2,
    @can_read = 1;

-- Share tag with full access
EXEC usp_share_tag 
    @owner_id = 1,
    @tag_id = 5,
    @recipient_id = 2,
    @can_read = 1,
    @can_write = 1,
    @can_tag = 1,
    @can_untag = 1;

-- Share tag with expiration
EXEC usp_share_tag 
    @owner_id = 1,
    @tag_id = 5,
    @recipient_id = 2,
    @can_read = 1,
    @can_tag = 1,
    @expires_at = '2025-12-31 23:59:59';

-- Share only this tag, not children
EXEC usp_share_tag 
    @owner_id = 1,
    @tag_id = 5,
    @recipient_id = 2,
    @can_read = 1,
    @include_children = 0;
```

---

### 4.2 usp_revoke_tag_share

Revoke a tag share.

```sql
CREATE OR ALTER PROCEDURE usp_revoke_tag_share
    @owner_id INT,
    @tag_id INT,
    @recipient_id INT = NULL  -- NULL = revoke all shares for this tag
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    
    BEGIN TRY
        -- =============================================
        -- Validate ownership
        -- =============================================
        IF NOT EXISTS (
            SELECT 1 FROM tags 
            WHERE id = @tag_id 
            AND user_id = @owner_id
        )
        BEGIN
            THROW 50005, 'Tag not found or access denied', 1;
        END;
        
        -- =============================================
        -- Revoke share(s)
        -- =============================================
        UPDATE tag_shares
        SET revoked_at = GETDATE()
        WHERE tag_id = @tag_id
        AND owner_id = @owner_id
        AND (@recipient_id IS NULL OR shared_with_id = @recipient_id)
        AND revoked_at IS NULL;
        
        DECLARE @revoked_count INT = @@ROWCOUNT;
        
        IF @revoked_count = 0
        BEGIN
            THROW 50014, 'Share not found or already revoked', 1;
        END;
        
        -- =============================================
        -- Audit log
        -- =============================================
        INSERT INTO tag_share_audit (action, performed_by, details)
        VALUES (
            'share_revoked', 
            @owner_id, 
            (SELECT 
                tag_id = @tag_id, 
                recipient_id = @recipient_id,
                revoked_count = @revoked_count
             FOR JSON PATH, WITHOUT_ARRAY_WRAPPER)
        );
        
        SELECT 
            revoked_count = @revoked_count,
            message = CASE 
                WHEN @revoked_count = 1 THEN 'Share revoked successfully'
                ELSE CAST(@revoked_count AS NVARCHAR) + ' shares revoked successfully'
            END;
        
        COMMIT;
    END TRY
    BEGIN CATCH
        ROLLBACK;
        THROW;
    END CATCH;
END;
GO
```

**Usage:**
```sql
-- Revoke share for specific user
EXEC usp_revoke_tag_share 
    @owner_id = 1,
    @tag_id = 5,
    @recipient_id = 2;

-- Revoke all shares for a tag
EXEC usp_revoke_tag_share 
    @owner_id = 1,
    @tag_id = 5,
    @recipient_id = NULL;
```

---

### 4.3 usp_u_tag_share

Update permissions of an existing share.

```sql
CREATE OR ALTER PROCEDURE usp_u_tag_share
    @owner_id INT,
    @tag_id INT,
    @recipient_id INT,
    @can_read BIT = NULL,
    @can_write BIT = NULL,
    @can_tag BIT = NULL,
    @can_untag BIT = NULL,
    @can_reshare BIT = NULL,
    @include_children BIT = NULL,
    @expires_at DATETIME = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    
    BEGIN TRY
        -- =============================================
        -- Validate share exists
        -- =============================================
        IF NOT EXISTS (
            SELECT 1 FROM tag_shares
            WHERE tag_id = @tag_id
            AND owner_id = @owner_id
            AND shared_with_id = @recipient_id
            AND revoked_at IS NULL
        )
        BEGIN
            THROW 50028, 'Share not found or already revoked', 1;
        END;
        
        -- =============================================
        -- Update only provided parameters
        -- =============================================
        UPDATE tag_shares
        SET 
            can_read = ISNULL(@can_read, can_read),
            can_write = ISNULL(@can_write, can_write),
            can_tag = ISNULL(@can_tag, can_tag),
            can_untag = ISNULL(@can_untag, can_untag),
            can_reshare = ISNULL(@can_reshare, can_reshare),
            include_children = ISNULL(@include_children, include_children),
            expires_at = CASE 
                WHEN @expires_at IS NOT NULL THEN @expires_at 
                ELSE expires_at 
            END
        WHERE tag_id = @tag_id
        AND owner_id = @owner_id
        AND shared_with_id = @recipient_id;
        
        -- =============================================
        -- Audit log
        -- =============================================
        INSERT INTO tag_share_audit (action, performed_by, details)
        VALUES (
            'share_updated', 
            @owner_id,
            (SELECT 
                tag_id = @tag_id, 
                recipient_id = @recipient_id
             FOR JSON PATH, WITHOUT_ARRAY_WRAPPER)
        );
        
        -- =============================================
        -- Return updated share
        -- =============================================
        SELECT 
            s.*,
            t.name AS tag_name,
            u.username AS shared_with_username
        FROM tag_shares s
        INNER JOIN tags t ON s.tag_id = t.id
        INNER JOIN users u ON s.shared_with_id = u.id
        WHERE s.tag_id = @tag_id
        AND s.owner_id = @owner_id
        AND s.shared_with_id = @recipient_id;
        
        COMMIT;
    END TRY
    BEGIN CATCH
        ROLLBACK;
        THROW;
    END CATCH;
END;
GO
```

**Usage:**
```sql
-- Grant write permission
EXEC usp_u_tag_share 
    @owner_id = 1,
    @tag_id = 5,
    @recipient_id = 2,
    @can_write = 1;

-- Extend expiration
EXEC usp_u_tag_share 
    @owner_id = 1,
    @tag_id = 5,
    @recipient_id = 2,
    @expires_at = '2026-12-31';
```

---

### 4.4 usp_s_tag_shares

List all shares for a tag.

```sql
CREATE OR ALTER PROCEDURE usp_s_tag_shares
    @owner_id INT,
    @tag_id INT,
    @include_revoked BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    
    -- =============================================
    -- Validate ownership
    -- =============================================
    IF NOT EXISTS (
        SELECT 1 FROM tags 
        WHERE id = @tag_id 
        AND user_id = @owner_id
    )
    BEGIN
        THROW 50005, 'Tag not found or access denied', 1;
    END;
    
    -- =============================================
    -- Get shares
    -- =============================================
    SELECT 
        s.id,
        s.tag_id,
        t.name AS tag_name,
        t.path AS tag_path,
        s.shared_with_id,
        u.username AS shared_with_username,
        u.email AS shared_with_email,
        s.can_read,
        s.can_write,
        s.can_tag,
        s.can_untag,
        s.can_reshare,
        s.include_children,
        s.shared_at,
        s.shared_by,
        sharer.username AS shared_by_username,
        s.expires_at,
        s.revoked_at,
        CASE 
            WHEN s.revoked_at IS NOT NULL THEN 'revoked'
            WHEN s.expires_at IS NOT NULL AND s.expires_at < GETDATE() THEN 'expired'
            ELSE 'active'
        END AS status
    FROM tag_shares s
    INNER JOIN tags t ON s.tag_id = t.id
    INNER JOIN users u ON s.shared_with_id = u.id
    LEFT JOIN users sharer ON s.shared_by = sharer.id
    WHERE s.tag_id = @tag_id
    AND s.owner_id = @owner_id
    AND (@include_revoked = 1 OR s.revoked_at IS NULL)
    ORDER BY 
        CASE 
            WHEN s.revoked_at IS NOT NULL THEN 2
            WHEN s.expires_at IS NOT NULL AND s.expires_at < GETDATE() THEN 1
            ELSE 0
        END,
        s.shared_at DESC;
END;
GO
```

**Usage:**
```sql
-- Get active shares
EXEC usp_s_tag_shares 
    @owner_id = 1,
    @tag_id = 5;

-- Include revoked shares
EXEC usp_s_tag_shares 
    @owner_id = 1,
    @tag_id = 5,
    @include_revoked = 1;
```

---

### 4.5 usp_s_shared_tags

Get all tags shared WITH current user.

```sql
CREATE OR ALTER PROCEDURE usp_s_shared_tags
    @user_id INT,
    @status NVARCHAR(20) = 'active'  -- 'active', 'expired', 'all'
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        t.id,
        t.user_id AS owner_id,
        owner.username AS owner_username,
        t.name,
        t.path,
        t.slug,
        t.color,
        t.icon,
        t.description,
        s.can_read,
        s.can_write,
        s.can_tag,
        s.can_untag,
        s.can_reshare,
        s.include_children,
        s.shared_at,
        s.expires_at,
        COUNT(tg.id) AS usage_count,
        CASE 
            WHEN s.expires_at IS NOT NULL AND s.expires_at < GETDATE() THEN 'expired'
            ELSE 'active'
        END AS status
    FROM tag_shares s
    INNER JOIN tags t ON s.tag_id = t.id
    INNER JOIN users owner ON t.user_id = owner.id
    LEFT JOIN taggables tg ON tg.tag_id = t.id
    WHERE s.shared_with_id = @user_id
    AND s.revoked_at IS NULL
    AND t.deleted_at IS NULL
    AND (
        @status = 'all'
        OR (@status = 'active' AND (s.expires_at IS NULL OR s.expires_at >= GETDATE()))
        OR (@status = 'expired' AND s.expires_at < GETDATE())
    )
    GROUP BY t.id, t.user_id, owner.username, t.name, t.path, t.slug, 
             t.color, t.icon, t.description, s.can_read, s.can_write, 
             s.can_tag, s.can_untag, s.can_reshare, s.include_children,
             s.shared_at, s.expires_at
    ORDER BY s.shared_at DESC;
END;
GO
```

**Usage:**
```sql
-- Get all active shared tags
EXEC usp_s_shared_tags @user_id = 2;

-- Get expired shares
EXEC usp_s_shared_tags @user_id = 2, @status = 'expired';

-- Get all shares (active + expired)
EXEC usp_s_shared_tags @user_id = 2, @status = 'all';
```

---

### 4.6 usp_share_tag_with_group

Share tag with all members of a group.

```sql
CREATE OR ALTER PROCEDURE usp_share_tag_with_group
    @owner_id INT,
    @tag_id INT,
    @group_id INT,
    @can_read BIT = 1,
    @can_write BIT = 0,
    @can_tag BIT = 0,
    @can_untag BIT = 0,
    @can_reshare BIT = 0,
    @include_children BIT = 1,
    @expires_at DATETIME = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    
    BEGIN TRY
        -- =============================================
        -- Validate tag ownership
        -- =============================================
        IF NOT EXISTS (
            SELECT 1 FROM tags 
            WHERE id = @tag_id 
            AND user_id = @owner_id
            AND deleted_at IS NULL
        )
        BEGIN
            THROW 50010, 'Tag not found or access denied', 1;
        END;
        
        -- =============================================
        -- Validate group exists
        -- =============================================
        IF NOT EXISTS (SELECT 1 FROM groups WHERE id = @group_id)
        BEGIN
            THROW 50029, 'Group not found', 1;
        END;
        
        -- =============================================
        -- Record group-level share
        -- =============================================
        MERGE INTO tag_share_groups AS target
        USING (
            SELECT @tag_id AS tag_id, @owner_id AS owner_id, @group_id AS group_id
        ) AS source
        ON target.tag_id = source.tag_id
        AND target.owner_id = source.owner_id
        AND target.group_id = source.group_id
        WHEN MATCHED THEN
            UPDATE SET
                can_read = @can_read,
                can_write = @can_write,
                can_tag = @can_tag,
                can_untag = @can_untag,
                can_reshare = @can_reshare,
                include_children = @include_children,
                revoked_at = NULL,
                shared_by = @owner_id
        WHEN NOT MATCHED THEN
            INSERT (
                tag_id, owner_id, group_id,
                can_read, can_write, can_tag, can_untag, can_reshare,
                include_children, shared_by
            )
            VALUES (
                @tag_id, @owner_id, @group_id,
                @can_read, @can_write, @can_tag, @can_untag, @can_reshare,
                @include_children, @owner_id
            );
        
        -- =============================================
        -- Share with all group members
        -- =============================================
        DECLARE @member_id INT;
        DECLARE member_cursor CURSOR FOR
            SELECT user_id 
            FROM group_members 
            WHERE group_id = @group_id
            AND user_id <> @owner_id;  -- Don't share with self
        
        OPEN member_cursor;
        FETCH NEXT FROM member_cursor INTO @member_id;
        
        WHILE @@FETCH_STATUS = 0
        BEGIN
            -- Use usp_share_tag for each member
            EXEC usp_share_tag 
                @owner_id = @owner_id,
                @tag_id = @tag_id,
                @recipient_id = @member_id,
                @can_read = @can_read,
                @can_write = @can_write,
                @can_tag = @can_tag,
                @can_untag = @can_untag,
                @can_reshare = @can_reshare,
                @include_children = @include_children,
                @expires_at = @expires_at;
            
            FETCH NEXT FROM member_cursor INTO @member_id;
        END;
        
        CLOSE member_cursor;
        DEALLOCATE member_cursor;
        
        -- =============================================
        -- Return summary
        -- =============================================
        SELECT 
            tag_id = @tag_id,
            group_id = @group_id,
            members_count = (SELECT COUNT(*) FROM group_members WHERE group_id = @group_id AND user_id <> @owner_id),
            message = 'Tag shared with group successfully';
        
        COMMIT;
    END TRY
    BEGIN CATCH
        IF CURSOR_STATUS('local', 'member_cursor') >= 0
        BEGIN
            CLOSE member_cursor;
            DEALLOCATE member_cursor;
        END;
        
        ROLLBACK;
        THROW;
    END CATCH;
END;
GO
```

**Usage:**
```sql
-- Share tag with team
EXEC usp_share_tag_with_group 
    @owner_id = 1,
    