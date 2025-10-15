# 06 - Tag Sharing System

Tài liệu này mô tả chi tiết hệ thống sharing tags với đầy đủ permissions, security và use cases.

---

## 📋 Table of Contents

1. [Overview](#overview)
2. [Schema Design](#schema-design)
3. [Permission Model](#permission-model)
4. [Stored Procedures](#stored-procedures)
5. [Security & RLS](#security--rls)
6. [Use Cases](#use-cases)
7. [Performance](#performance)
8. [Best Practices](#best-practices)

---

## 🎯 Overview

### Features
- ✅ Share tags với users hoặc groups
- ✅ Granular permissions (read, write, tag, untag, reshare)
- ✅ Include/exclude children (share subtree)
- ✅ Expiration dates
- ✅ Audit trail
- ✅ Row-Level Security integration

### Use Cases
```
1. User A shares "Work.ProjectX" với User B (read-only)
2. User B shares "Recipes" với Team (read-write)  
3. Organization shares "Company Policies" với all employees
4. User unshares tag → recipients lose access
5. Temporary share với expiration date
```

---

## 📐 Schema Design

### Main Tables

```sql
-- ============================================
-- TAG SHARES: Many-to-many sharing
-- ============================================
CREATE TABLE tag_shares (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    tag_id INT NOT NULL,
    owner_id INT NOT NULL,        -- Tag owner
    shared_with_id INT NOT NULL,  -- Recipient user
    
    -- Permissions
    can_read BIT DEFAULT 1,
    can_write BIT DEFAULT 0,      -- Edit tag metadata
    can_tag BIT DEFAULT 0,        -- Tag items with this
    can_untag BIT DEFAULT 0,      -- Remove tags
    can_reshare BIT DEFAULT 0,    -- Share to others
    
    -- Scope
    include_children BIT DEFAULT 1,  -- Share subtree?
    
    -- Lifecycle
    expires_at DATETIME2 NULL,
    shared_at DATETIME2 DEFAULT GETUTCDATE(),
    revoked_at DATETIME2 NULL,
    
    -- Audit
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    updated_at DATETIME2 DEFAULT GETUTCDATE(),
    created_by INT NOT NULL,
    
    CONSTRAINT fk_tagshare_tag FOREIGN KEY (tag_id) REFERENCES tags(id) ON DELETE CASCADE,
    CONSTRAINT fk_tagshare_owner FOREIGN KEY (owner_id) REFERENCES users(id),
    CONSTRAINT fk_tagshare_recipient FOREIGN KEY (shared_with_id) REFERENCES users(id),
    CONSTRAINT fk_tagshare_creator FOREIGN KEY (created_by) REFERENCES users(id),
    
    -- Unique: Can't share same tag to same user twice (active shares)
    CONSTRAINT uq_tag_share_active UNIQUE (tag_id, shared_with_id, revoked_at)
);

CREATE INDEX ix_tagshares_recipient ON tag_shares(shared_with_id, revoked_at)
    WHERE revoked_at IS NULL;
CREATE INDEX ix_tagshares_tag ON tag_shares(tag_id, revoked_at)
    WHERE revoked_at IS NULL;
CREATE INDEX ix_tagshares_expiration ON tag_shares(expires_at)
    WHERE expires_at IS NOT NULL AND revoked_at IS NULL;

-- ============================================
-- GROUP SHARES: Share với groups
-- ============================================
CREATE TABLE tag_group_shares (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    tag_id INT NOT NULL,
    owner_id INT NOT NULL,
    group_id INT NOT NULL,         -- User group
    
    -- Same permission structure
    can_read BIT DEFAULT 1,
    can_write BIT DEFAULT 0,
    can_tag BIT DEFAULT 0,
    can_untag BIT DEFAULT 0,
    can_reshare BIT DEFAULT 0,
    include_children BIT DEFAULT 1,
    
    expires_at DATETIME2 NULL,
    shared_at DATETIME2 DEFAULT GETUTCDATE(),
    revoked_at DATETIME2 NULL,
    
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    updated_at DATETIME2 DEFAULT GETUTCDATE(),
    created_by INT NOT NULL,
    
    CONSTRAINT fk_groupshare_tag FOREIGN KEY (tag_id) REFERENCES tags(id) ON DELETE CASCADE,
    CONSTRAINT fk_groupshare_owner FOREIGN KEY (owner_id) REFERENCES users(id),
    CONSTRAINT fk_groupshare_group FOREIGN KEY (group_id) REFERENCES user_groups(id),
    CONSTRAINT fk_groupshare_creator FOREIGN KEY (created_by) REFERENCES users(id),
    
    CONSTRAINT uq_tag_group_share_active UNIQUE (tag_id, group_id, revoked_at)
);

CREATE INDEX ix_groupshares_group ON tag_group_shares(group_id, revoked_at)
    WHERE revoked_at IS NULL;
CREATE INDEX ix_groupshares_tag ON tag_group_shares(tag_id, revoked_at)
    WHERE revoked_at IS NULL;

-- ============================================
-- SHARE AUDIT LOG
-- ============================================
CREATE TABLE tag_share_audit (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    share_id BIGINT NOT NULL,
    action NVARCHAR(50) NOT NULL,  -- 'CREATED', 'UPDATED', 'REVOKED', 'EXPIRED'
    
    -- Snapshot of permissions at time of action
    permissions_snapshot NVARCHAR(MAX),  -- JSON
    
    performed_by INT NOT NULL,
    performed_at DATETIME2 DEFAULT GETUTCDATE(),
    ip_address NVARCHAR(45),
    user_agent NVARCHAR(500),
    
    CONSTRAINT fk_shareaudit_share FOREIGN KEY (share_id) REFERENCES tag_shares(id),
    CONSTRAINT fk_shareaudit_user FOREIGN KEY (performed_by) REFERENCES users(id)
);

CREATE INDEX ix_shareaudit_share ON tag_share_audit(share_id, performed_at);
CREATE INDEX ix_shareaudit_user ON tag_share_audit(performed_by, performed_at);
```

---

## 🔐 Permission Model

### Permission Levels

| Permission | Description | Implies |
|-----------|-------------|---------|
| **can_read** | View tag and its metadata | - |
| **can_write** | Edit tag name, color, description | can_read |
| **can_tag** | Apply tag to items | can_read |
| **can_untag** | Remove tag from items | can_read |
| **can_reshare** | Share tag with others | can_read |

### Permission Inheritance

```sql
-- Tag hierarchy: Work → ProjectX → Phase1
-- Share "Work" with include_children = TRUE
-- → User gets access to ProjectX and Phase1 automatically

-- Example query to check permissions:
CREATE FUNCTION dbo.fn_user_can_access_tag(
    @user_id INT,
    @tag_id INT,
    @permission NVARCHAR(20)  -- 'read', 'write', 'tag', 'untag', 'reshare'
)
RETURNS BIT
AS
BEGIN
    DECLARE @has_permission BIT = 0;
    
    -- Check direct ownership
    IF EXISTS (
        SELECT 1 FROM tags 
        WHERE id = @tag_id AND user_id = @user_id AND deleted_at IS NULL
    )
        RETURN 1;
    
    -- Check direct share
    IF EXISTS (
        SELECT 1 FROM tag_shares ts
        WHERE ts.shared_with_id = @user_id
        AND ts.tag_id = @tag_id
        AND ts.revoked_at IS NULL
        AND (ts.expires_at IS NULL OR ts.expires_at > GETUTCDATE())
        AND (
            (@permission = 'read' AND ts.can_read = 1) OR
            (@permission = 'write' AND ts.can_write = 1) OR
            (@permission = 'tag' AND ts.can_tag = 1) OR
            (@permission = 'untag' AND ts.can_untag = 1) OR
            (@permission = 'reshare' AND ts.can_reshare = 1)
        )
    )
        RETURN 1;
    
    -- Check inherited share (parent with include_children)
    IF EXISTS (
        SELECT 1 
        FROM tag_shares ts
        JOIN tag_paths tp ON tp.ancestor_id = ts.tag_id
        WHERE ts.shared_with_id = @user_id
        AND tp.descendant_id = @tag_id
        AND ts.include_children = 1
        AND ts.revoked_at IS NULL
        AND (ts.expires_at IS NULL OR ts.expires_at > GETUTCDATE())
        AND (
            (@permission = 'read' AND ts.can_read = 1) OR
            (@permission = 'write' AND ts.can_write = 1) OR
            (@permission = 'tag' AND ts.can_tag = 1) OR
            (@permission = 'untag' AND ts.can_untag = 1) OR
            (@permission = 'reshare' AND ts.can_reshare = 1)
        )
    )
        RETURN 1;
    
    -- Check group membership
    IF EXISTS (
        SELECT 1
        FROM tag_group_shares tgs
        JOIN user_group_members ugm ON ugm.group_id = tgs.group_id
        WHERE ugm.user_id = @user_id
        AND tgs.tag_id = @tag_id
        AND tgs.revoked_at IS NULL
        AND (tgs.expires_at IS NULL OR tgs.expires_at > GETUTCDATE())
        AND (
            (@permission = 'read' AND tgs.can_read = 1) OR
            (@permission = 'write' AND tgs.can_write = 1) OR
            (@permission = 'tag' AND tgs.can_tag = 1) OR
            (@permission = 'untag' AND tgs.can_untag = 1) OR
            (@permission = 'reshare' AND tgs.can_reshare = 1)
        )
    )
        RETURN 1;
    
    RETURN @has_permission;
END;
GO
```

---

## 🛠️ Stored Procedures

### 1. Share Tag with User

```sql
CREATE OR ALTER PROCEDURE sp_share_tag
    @tag_id INT,
    @owner_id INT,
    @shared_with_id INT,
    @can_read BIT = 1,
    @can_write BIT = 0,
    @can_tag BIT = 0,
    @can_untag BIT = 0,
    @can_reshare BIT = 0,
    @include_children BIT = 1,
    @expires_at DATETIME2 = NULL,
    @ip_address NVARCHAR(45) = NULL,
    @user_agent NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Validate: Owner must own the tag or have reshare permission
        IF NOT EXISTS (
            SELECT 1 FROM tags WHERE id = @tag_id AND user_id = @owner_id
        ) AND dbo.fn_user_can_access_tag(@owner_id, @tag_id, 'reshare') = 0
        BEGIN
            RAISERROR('You do not have permission to share this tag', 16, 1);
            RETURN;
        END;
        
        -- Validate: Cannot share with yourself
        IF @owner_id = @shared_with_id
        BEGIN
            RAISERROR('Cannot share tag with yourself', 16, 1);
            RETURN;
        END;
        
        -- Validate: Recipient exists
        IF NOT EXISTS (SELECT 1 FROM users WHERE id = @shared_with_id)
        BEGIN
            RAISERROR('Recipient user not found', 16, 1);
            RETURN;
        END;
        
        -- Check if already shared (active)
        DECLARE @existing_share_id BIGINT;
        SELECT @existing_share_id = id
        FROM tag_shares
        WHERE tag_id = @tag_id
        AND shared_with_id = @shared_with_id
        AND revoked_at IS NULL;
        
        IF @existing_share_id IS NOT NULL
        BEGIN
            -- Update existing share
            UPDATE tag_shares
            SET can_read = @can_read,
                can_write = @can_write,
                can_tag = @can_tag,
                can_untag = @can_untag,
                can_reshare = @can_reshare,
                include_children = @include_children,
                expires_at = @expires_at,
                updated_at = GETUTCDATE()
            WHERE id = @existing_share_id;
            
            -- Audit
            INSERT INTO tag_share_audit (share_id, action, permissions_snapshot, performed_by, ip_address, user_agent)
            VALUES (
                @existing_share_id,
                'UPDATED',
                JSON_OBJECT(
                    'can_read': @can_read,
                    'can_write': @can_write,
                    'can_tag': @can_tag,
                    'can_untag': @can_untag,
                    'can_reshare': @can_reshare,
                    'include_children': @include_children
                ),
                @owner_id,
                @ip_address,
                @user_agent
            );
            
            SELECT @existing_share_id AS share_id, 'UPDATED' AS action;
        END
        ELSE
        BEGIN
            -- Create new share
            INSERT INTO tag_shares (
                tag_id, owner_id, shared_with_id,
                can_read, can_write, can_tag, can_untag, can_reshare,
                include_children, expires_at, created_by
            )
            VALUES (
                @tag_id, @owner_id, @shared_with_id,
                @can_read, @can_write, @can_tag, @can_untag, @can_reshare,
                @include_children, @expires_at, @owner_id
            );
            
            SET @existing_share_id = SCOPE_IDENTITY();
            
            -- Audit
            INSERT INTO tag_share_audit (share_id, action, permissions_snapshot, performed_by, ip_address, user_agent)
            VALUES (
                @existing_share_id,
                'CREATED',
                JSON_OBJECT(
                    'can_read': @can_read,
                    'can_write': @can_write,
                    'can_tag': @can_tag,
                    'can_untag': @can_untag,
                    'can_reshare': @can_reshare,
                    'include_children': @include_children
                ),
                @owner_id,
                @ip_address,
                @user_agent
            );
            
            SELECT @existing_share_id AS share_id, 'CREATED' AS action;
        END;
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
```

### 2. Revoke Tag Share

```sql
CREATE OR ALTER PROCEDURE sp_revoke_tag_share
    @share_id BIGINT,
    @user_id INT,  -- User performing revoke (must be owner or have permission)
    @ip_address NVARCHAR(45) = NULL,
    @user_agent NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Validate: Share exists and not already revoked
        DECLARE @tag_id INT, @owner_id INT, @shared_with_id INT;
        
        SELECT @tag_id = tag_id, @owner_id = owner_id, @shared_with_id = shared_with_id
        FROM tag_shares
        WHERE id = @share_id AND revoked_at IS NULL;
        
        IF @tag_id IS NULL
        BEGIN
            RAISERROR('Share not found or already revoked', 16, 1);
            RETURN;
        END;
        
        -- Validate: User has permission to revoke
        IF @user_id != @owner_id AND @user_id != @shared_with_id
        BEGIN
            -- Check if user is tag owner
            IF NOT EXISTS (SELECT 1 FROM tags WHERE id = @tag_id AND user_id = @user_id)
            BEGIN
                RAISERROR('You do not have permission to revoke this share', 16, 1);
                RETURN;
            END;
        END;
        
        -- Revoke share
        UPDATE tag_shares
        SET revoked_at = GETUTCDATE(),
            updated_at = GETUTCDATE()
        WHERE id = @share_id;
        
        -- Audit
        INSERT INTO tag_share_audit (share_id, action, permissions_snapshot, performed_by, ip_address, user_agent)
        VALUES (
            @share_id,
            'REVOKED',
            NULL,
            @user_id,
            @ip_address,
            @user_agent
        );
        
        SELECT @share_id AS share_id, 'REVOKED' AS action;
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
```

### 3. Update Tag Share Permissions

```sql
CREATE OR ALTER PROCEDURE sp_update_tag_share
    @share_id BIGINT,
    @user_id INT,  -- Must be owner
    @can_read BIT = NULL,
    @can_write BIT = NULL,
    @can_tag BIT = NULL,
    @can_untag BIT = NULL,
    @can_reshare BIT = NULL,
    @include_children BIT = NULL,
    @expires_at DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Validate: Share exists
        DECLARE @owner_id INT;
        SELECT @owner_id = owner_id
        FROM tag_shares
        WHERE id = @share_id AND revoked_at IS NULL;
        
        IF @owner_id IS NULL
        BEGIN
            RAISERROR('Share not found or already revoked', 16, 1);
            RETURN;
        END;
        
        -- Validate: User is owner
        IF @user_id != @owner_id
        BEGIN
            RAISERROR('Only share owner can update permissions', 16, 1);
            RETURN;
        END;
        
        -- Update (only non-NULL values)
        UPDATE tag_shares
        SET can_read = COALESCE(@can_read, can_read),
            can_write = COALESCE(@can_write, can_write),
            can_tag = COALESCE(@can_tag, can_tag),
            can_untag = COALESCE(@can_untag, can_untag),
            can_reshare = COALESCE(@can_reshare, can_reshare),
            include_children = COALESCE(@include_children, include_children),
            expires_at = COALESCE(@expires_at, expires_at),
            updated_at = GETUTCDATE()
        WHERE id = @share_id;
        
        -- Audit
        INSERT INTO tag_share_audit (share_id, action, permissions_snapshot, performed_by)
        VALUES (
            @share_id,
            'UPDATED',
            JSON_OBJECT(
                'can_read': COALESCE(@can_read, (SELECT can_read FROM tag_shares WHERE id = @share_id)),
                'can_write': COALESCE(@can_write, (SELECT can_write FROM tag_shares WHERE id = @share_id)),
                'can_tag': COALESCE(@can_tag, (SELECT can_tag FROM tag_shares WHERE id = @share_id)),
                'can_untag': COALESCE(@can_untag, (SELECT can_untag FROM tag_shares WHERE id = @share_id)),
                'can_reshare': COALESCE(@can_reshare, (SELECT can_reshare FROM tag_shares WHERE id = @share_id))
            ),
            @user_id
        );
        
        SELECT @share_id AS share_id, 'UPDATED' AS action;
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
```

### 4. Get Tag Shares (Who has access)

```sql
CREATE OR ALTER PROCEDURE sp_get_tag_shares
    @tag_id INT,
    @user_id INT  -- Must be owner or have permission
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Validate: User can view shares
    IF NOT EXISTS (
        SELECT 1 FROM tags WHERE id = @tag_id AND user_id = @user_id
    ) AND dbo.fn_user_can_access_tag(@user_id, @tag_id, 'read') = 0
    BEGIN
        RAISERROR('You do not have permission to view shares for this tag', 16, 1);
        RETURN;
    END;
    
    -- Direct user shares
    SELECT 
        ts.id AS share_id,
        'USER' AS share_type,
        ts.shared_with_id AS recipient_id,
        u.email AS recipient_email,
        u.display_name AS recipient_name,
        ts.can_read,
        ts.can_write,
        ts.can_tag,
        ts.can_untag,
        ts.can_reshare,
        ts.include_children,
        ts.expires_at,
        ts.shared_at,
        ts.created_by,
        creator.email AS created_by_email
    FROM tag_shares ts
    JOIN users u ON u.id = ts.shared_with_id
    JOIN users creator ON creator.id = ts.created_by
    WHERE ts.tag_id = @tag_id
    AND ts.revoked_at IS NULL
    
    UNION ALL
    
    -- Group shares
    SELECT 
        tgs.id AS share_id,
        'GROUP' AS share_type,
        tgs.group_id AS recipient_id,
        ug.name AS recipient_email,
        ug.description AS recipient_name,
        tgs.can_read,
        tgs.can_write,
        tgs.can_tag,
        tgs.can_untag,
        tgs.can_reshare,
        tgs.include_children,
        tgs.expires_at,
        tgs.shared_at,
        tgs.created_by,
        creator.email AS created_by_email
    FROM tag_group_shares tgs
    JOIN user_groups ug ON ug.id = tgs.group_id
    JOIN users creator ON creator.id = tgs.created_by
    WHERE tgs.tag_id = @tag_id
    AND tgs.revoked_at IS NULL
    
    ORDER BY shared_at DESC;
END;
GO
```

### 5. Get Shared Tags (Tags shared with me)

```sql
CREATE OR ALTER PROCEDURE sp_get_shared_tags
    @user_id INT,
    @include_expired BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Direct shares
    SELECT 
        t.id AS tag_id,
        t.name AS tag_name,
        t.path,
        t.color,
        'USER' AS share_type,
        ts.owner_id,
        owner.email AS owner_email,
        owner.display_name AS owner_name,
        ts.can_read,
        ts.can_write,
        ts.can_tag,
        ts.can_untag,
        ts.can_reshare,
        ts.include_children,
        ts.expires_at,
        ts.shared_at,
        CASE 
            WHEN ts.expires_at IS NOT NULL AND ts.expires_at <= GETUTCDATE() THEN 1
            ELSE 0
        END AS is_expired
    FROM tag_shares ts
    JOIN tags t ON t.id = ts.tag_id
    JOIN users owner ON owner.id = ts.owner_id
    WHERE ts.shared_with_id = @user_id
    AND ts.revoked_at IS NULL
    AND t.deleted_at IS NULL
    AND (@include_expired = 1 OR ts.expires_at IS NULL OR ts.expires_at > GETUTCDATE())
    
    UNION ALL
    
    -- Group shares
    SELECT 
        t.id AS tag_id,
        t.name AS tag_name,
        t.path,
        t.color,
        'GROUP' AS share_type,
        tgs.owner_id,
        owner.email AS owner_email,
        owner.display_name AS owner_name,
        tgs.can_read,
        tgs.can_write,
        tgs.can_tag,
        tgs.can_untag,
        tgs.can_reshare,
        tgs.include_children,
        tgs.expires_at,
        tgs.shared_at,
        CASE 
            WHEN tgs.expires_at IS NOT NULL AND tgs.expires_at <= GETUTCDATE() THEN 1
            ELSE 0
        END AS is_expired
    FROM tag_group_shares tgs
    JOIN user_group_members ugm ON ugm.group_id = tgs.group_id
    JOIN tags t ON t.id = tgs.tag_id
    JOIN users owner ON owner.id = tgs.owner_id
    WHERE ugm.user_id = @user_id
    AND tgs.revoked_at IS NULL
    AND t.deleted_at IS NULL
    AND (@include_expired = 1 OR tgs.expires_at IS NULL OR tgs.expires_at > GETUTCDATE())
    
    ORDER BY shared_at DESC;
END;
GO
```

### 6. Share Tag with Group

```sql
CREATE OR ALTER PROCEDURE sp_share_tag_with_group
    @tag_id INT,
    @owner_id INT,
    @group_id INT,
    @can_read BIT = 1,
    @can_write BIT = 0,
    @can_tag BIT = 0,
    @can_untag BIT = 0,
    @can_reshare BIT = 0,
    @include_children BIT = 1,
    @expires_at DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Validate: Owner must own the tag
        IF NOT EXISTS (SELECT 1 FROM tags WHERE id = @tag_id AND user_id = @owner_id)
        BEGIN
            RAISERROR('You do not own this tag', 16, 1);
            RETURN;
        END;
        
        -- Validate: Group exists
        IF NOT EXISTS (SELECT 1 FROM user_groups WHERE id = @group_id)
        BEGIN
            RAISERROR('Group not found', 16, 1);
            RETURN;
        END;
        
        -- Check if already shared
        DECLARE @existing_share_id BIGINT;
        SELECT @existing_share_id = id
        FROM tag_group_shares
        WHERE tag_id = @tag_id
        AND group_id = @group_id
        AND revoked_at IS NULL;
        
        IF @existing_share_id IS NOT NULL
        BEGIN
            -- Update
            UPDATE tag_group_shares
            SET can_read = @can_read,
                can_write = @can_write,
                can_tag = @can_tag,
                can_untag = @can_untag,
                can_reshare = @can_reshare,
                include_children = @include_children,
                expires_at = @expires_at,
                updated_at = GETUTCDATE()
            WHERE id = @existing_share_id;
            
            SELECT @existing_share_id AS share_id, 'UPDATED' AS action;
        END
        ELSE
        BEGIN
            -- Create
            INSERT INTO tag_group_shares (
                tag_id, owner_id, group_id,
                can_read, can_write, can_tag, can_untag, can_reshare,
                include_children, expires_at, created_by
            )
            VALUES (
                @tag_id, @owner_id, @group_id,
                @can_read, @can_write, @can_tag, @can_untag, @can_reshare,
                @include_children, @expires_at, @owner_id
            );
            
            SELECT SCOPE_IDENTITY() AS share_id, 'CREATED' AS action;
        END;
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
```

### 7. Cleanup Expired Shares (Background Job)

```sql
CREATE OR ALTER PROCEDURE sp_cleanup_expired_shares
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @expired_count INT = 0;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Mark expired shares as revoked
        UPDATE tag_shares
        SET revoked_at = GETUTCDATE(),
            updated_at = GETUTCDATE()
        WHERE expires_at IS NOT NULL
        AND expires_at <= GETUTCDATE()
        AND revoked_at IS NULL;
        
        SET @expired_count = @@ROWCOUNT;
        
        -- Audit
        INSERT INTO tag_share_audit (share_id, action, permissions_snapshot, performed_by)
        SELECT 
            id,
            'EXPIRED',
            NULL,
            owner_id
        FROM tag_shares
        WHERE revoked_at = (SELECT MAX(updated_at) FROM tag_shares WHERE revoked_at IS NOT NULL);
        
        -- Group shares
        UPDATE tag_group_shares
        SET revoked_at = GETUTCDATE(),
            updated_at = GETUTCDATE()
        WHERE expires_at IS NOT NULL
        AND expires_at <= GETUTCDATE()
        AND revoked_at IS NULL;
        
        SET @expired_count = @expired_count + @@ROWCOUNT;
        
        SELECT @expired_count AS expired_shares_count;
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
```

---

## 🔒 Security & Row-Level Security

### RLS Policy for Tags

```sql
-- Enable RLS on tags table
ALTER TABLE tags ENABLE ROW_LEVEL SECURITY;

-- Policy: Users can see their own tags + shared tags
CREATE SECURITY POLICY tags_rls_policy
ADD FILTER PREDICATE dbo.fn_user_can_access_tag_rls(user_id, id, 'read')
ON tags
WITH (STATE = ON);

-- Function for RLS
CREATE FUNCTION dbo.fn_user_can_access_tag_rls(
    @tag_owner_id INT,
    @tag_id INT,
    @permission NVARCHAR(20)
)
RETURNS TABLE
WITH SCHEMABINDING
AS
RETURN
(
    SELECT 1 AS can_access
    WHERE 
        -- User is owner
        @tag_owner_id = CAST(SESSION_CONTEXT(N'user_id') AS INT)
        OR
        -- User has share access
        EXISTS (
            SELECT 1 FROM dbo.tag_shares ts
            WHERE ts.tag_id = @tag_id
            AND ts.shared_with_id = CAST(SESSION_CONTEXT(N'user_id') AS INT)
            AND ts.revoked_at IS NULL
            AND (ts.expires_at IS NULL OR ts.expires_at > GETUTCDATE())
        )
        OR
        -- User has group share access
        EXISTS (
            SELECT 1 FROM dbo.tag_group_shares tgs
            JOIN dbo.user_group_members ugm ON ugm.group_id = tgs.group_id
            WHERE tgs.tag_id = @tag_id
            AND ugm.user_id = CAST(SESSION_CONTEXT(N'user_id') AS INT)
            AND tgs.revoked_at IS NULL
            AND (tgs.expires_at IS NULL OR tgs.expires_at > GETUTCDATE())
        )
);
GO
```

### Set Session Context (Call at login)

```sql
-- After user authentication
EXEC sp_set_session_context @key = N'user_id', @value = 123, @read_only = 1;
```

---

## 💡 Use Cases & Examples

### Example 1: Share Read-Only Tag

```sql
-- User 1 shares "Work.ProjectX" with User 2 (read-only)
EXEC sp_share_tag
    @tag_id = 42,
    @owner_id = 1,
    @shared_with_id = 2,
    @can_read = 1,
    @can_write = 0,
    @can_tag = 0,
    @can_untag = 0,
    @can_reshare = 0,
    @include_children = 1;  -- Include subtree
```

### Example 2: Share with Full Permissions

```sql
-- User 1 shares "Recipes" with User 3 (full access)
EXEC sp_share_tag
    @tag_id = 88,
    @owner_id = 1,
    @shared_with_id = 3,
    @can_read = 1,
    @can_write = 1,
    @can_tag = 1,
    @can_untag = 1,
    @can_reshare = 1,
    @include_children = 1;
```

### Example 3: Temporary Share (24 hours)

```sql
-- Share with expiration
EXEC sp_share_tag
    @tag_id = 99,
    @owner_id = 1,
    @shared_with_id = 4,
    @can_read = 1,
    @can_tag = 1,
    @include_children = 0,  -- Only this tag
    @expires_at = DATEADD(HOUR, 24, GETUTCDATE());
```

### Example 4: Share with Team

```sql
-- Share with entire team/group
EXEC sp_share_tag_with_group
    @tag_id = 100,
    @owner_id = 1,
    @group_id = 5,  -- "Marketing Team"
    @can_read = 1,
    @can_tag = 1,
    @include_children = 1;
```

### Example 5: Query Shared Tags

```sql
-- Get all tags shared with me
EXEC sp_get_shared_tags @user_id = 2;

-- Get who has access to my tag
EXEC sp_get_tag_shares @tag_id = 42, @user_id = 1;
```

### Example 6: Revoke Share

```sql
-- Owner revokes share
EXEC sp_revoke_tag_share @share_id = 123, @user_id = 1;

-- Recipient can also unshare themselves
EXEC sp_revoke_tag_share @share_id = 123, @user_id = 2;
```

---

## 🚀 Performance Considerations

### 1. **Index Strategy**
```sql
-- Critical indexes already defined:
-- - ix_tagshares_recipient (shared_with_id, revoked_at)
-- - ix_tagshares_tag (tag_id, revoked_at)
-- - ix_tagshares_expiration (expires_at)
-- These support fast lookups for permissions checks
```

### 2. **Caching Recommendations**
```sql
-- Cache user permissions in application layer
-- Invalidate cache on share/revoke/expiration
-- Example cache key: "user:123:tag:42:permissions"
```

### 3. **Batch Operations**
```sql
-- For bulk shares, consider bulk insert instead of multiple sp_share_tag calls
INSERT INTO tag_shares (tag_id, owner_id, shared_with_id, can_read, can_tag, created_by)
SELECT @tag_id, @owner_id, user_id, 1, 1, @owner_id
FROM user_group_members
WHERE group_id = @group_id;
```

### 4. **Monitoring**
```sql
-- Track share growth
SELECT 
    CAST(shared_at AS DATE) AS share_date,
    COUNT(*) AS new_shares,
    COUNT(DISTINCT owner_id) AS active_sharers,
    COUNT(DISTINCT shared_with_id) AS active_recipients
FROM tag_shares
WHERE shared_at >= DATEADD(DAY, -30, GETUTCDATE())
GROUP BY CAST(shared_at AS DATE)
ORDER BY share_date DESC;
```

---

## ✅ Best Practices

### 1. **Permission Design**
- ✅ Start with minimal permissions (read-only)
- ✅ Escalate permissions only when needed
- ✅ Use groups for team-wide access
- ✅ Set expiration for temporary access

### 2. **Security**
- ✅ Always validate ownership before sharing
- ✅ Log all share operations (audit trail)
- ✅ Run cleanup job daily to revoke expired shares
- ✅ Use RLS to enforce permissions at DB level

### 3. **User Experience**
- ✅ Show clear indicators for shared tags in UI
- ✅ Notify recipients when tag is shared
- ✅ Allow recipients to "unshare" themselves
- ✅ Display share hierarchy (who shared with whom)

### 4. **Performance**
- ✅ Cache permission checks in application
- ✅ Use indexed queries for share lookups
- ✅ Avoid recursive permission checks in hot paths
- ✅ Consider eventual consistency for share notifications

---

## 🧪 Testing Checklist

### Unit Tests
- [ ] Share tag with valid permissions
- [ ] Share tag without ownership (should fail)
- [ ] Share tag with self (should fail)
- [ ] Update share permissions
- [ ] Revoke share (by owner)
- [ ] Revoke share (by recipient)
- [ ] Expired shares are cleaned up
- [ ] Permission inheritance (parent → children)

### Integration Tests
- [ ] User can tag item with shared tag (can_tag = true)
- [ ] User cannot tag item with shared tag (can_tag = false)
- [ ] User can view items tagged with shared tag
- [ ] Group members inherit group share permissions
- [ ] RLS enforces correct visibility

### Performance Tests
- [ ] Permission check < 10ms (cached)
- [ ] Permission check < 100ms (uncached)
- [ ] Share creation < 50ms
- [ ] Query shared tags < 200ms (100 shares)

---

## 📚 Related Documentation

- [01-Schema.md](01-Schema.md) - Full database schema
- [02-Triggers.md](02-Triggers.md) - Trigger logic for path maintenance
- [04-Procedures.md](04-Procedures.md) - All stored procedures
- [05-Security.md](05-Security.md) - Security and RLS details

---

**End of 06-Sharing.md**
