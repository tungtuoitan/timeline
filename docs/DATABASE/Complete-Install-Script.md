# Complete Installation Script

Single script to install entire Tag-Tree system.

---

## Prerequisites

- SQL Server 2016 or later
- CREATE DATABASE permission
- SA or db_owner role

---

## Complete Installation Script

```sql
-- ============================================
-- TAG-TREE SYSTEM - COMPLETE INSTALLATION
-- SQL Server 2016+
-- Version: 1.0
-- ============================================

USE master;
GO

-- Create database (if needed)
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'SuperApp-dev')
BEGIN
    CREATE DATABASE SuperApp-dev;
END;
GO

USE SuperApp-dev;
GO

-- ============================================
-- STEP 1: DROP EXISTING OBJECTS (Optional - for clean install)
-- ============================================
/*
DROP SECURITY POLICY IF EXISTS tags_security_policy;
DROP TRIGGER IF EXISTS trg_audit_tag_changes;
DROP TRIGGER IF EXISTS trg_cascade_tag_share_revoke;
DROP TRIGGER IF EXISTS trg_validate_tag_delete;
DROP TRIGGER IF EXISTS trg_validate_taggable_user;
DROP TRIGGER IF EXISTS trg_maintain_tag_path;
DROP TRIGGER IF EXISTS trg_maintain_tag_closure;
DROP PROCEDURE IF EXISTS sp_apply_tag_template;
DROP PROCEDURE IF EXISTS sp_get_tag_shares;
DROP PROCEDURE IF EXISTS sp_revoke_tag_share;
DROP PROCEDURE IF EXISTS sp_share_tag;
DROP PROCEDURE IF EXISTS sp_search_tags;
DROP PROCEDURE IF EXISTS sp_get_tagged_items;
DROP PROCEDURE IF EXISTS sp_get_tag_breadcrumb;
DROP PROCEDURE IF EXISTS sp_get_tag_subtree;
DROP PROCEDURE IF EXISTS sp_get_user_tags;
DROP PROCEDURE IF EXISTS sp_untag_item;
DROP PROCEDURE IF EXISTS sp_tag_item;
DROP PROCEDURE IF EXISTS sp_delete_tag;
DROP PROCEDURE IF EXISTS sp_move_tag;
DROP PROCEDURE IF EXISTS sp_update_tag;
DROP PROCEDURE IF EXISTS sp_create_tag;
DROP FUNCTION IF EXISTS fn_get_tag_statistics;
DROP FUNCTION IF EXISTS fn_filter_tags_by_user;
DROP FUNCTION IF EXISTS fn_is_tag_ancestor;
DROP FUNCTION IF EXISTS fn_get_tag_descendants_count;
DROP FUNCTION IF EXISTS fn_get_tag_children_count;
DROP FUNCTION IF EXISTS fn_get_tag_depth;
DROP FUNCTION IF EXISTS fn_can_access_tag;
DROP TABLE IF EXISTS tag_share_audit;
DROP TABLE IF EXISTS tag_template_items;
DROP TABLE IF EXISTS tag_templates;
DROP TABLE IF EXISTS tag_rules;
DROP TABLE IF EXISTS tag_share_groups;
DROP TABLE IF EXISTS tag_shares;
DROP TABLE IF EXISTS group_members;
DROP TABLE IF EXISTS groups;
DROP TABLE IF EXISTS taggables;
DROP TABLE IF EXISTS tag_paths;
DROP TABLE IF EXISTS tags;
DROP TABLE IF EXISTS entity_types;
DROP TABLE IF EXISTS users;
*/

-- ============================================
-- STEP 2: CREATE TABLES
-- ============================================

-- Users (if not exists)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'users')
BEGIN
    CREATE TABLE users (
        id INT IDENTITY(1,1) PRIMARY KEY,
        username NVARCHAR(100) NOT NULL UNIQUE,
        email NVARCHAR(255) NOT NULL UNIQUE,
        created_at DATETIME DEFAULT GETDATE()
    );
    
    -- Insert demo users
    INSERT INTO users (username, email) VALUES 
        ('admin', 'admin@example.com'),
        ('user1', 'user1@example.com'),
        ('user2', 'user2@example.com');
END;
GO

-- Entity types
CREATE TABLE entity_types (
    type_name NVARCHAR(50) PRIMARY KEY,
    table_name NVARCHAR(100) NOT NULL,
    is_active BIT DEFAULT 1,
    created_at DATETIME DEFAULT GETDATE()
);
GO

-- Tags
CREATE TABLE tags (
    id INT IDENTITY(1,1) PRIMARY KEY,
    user_id INT NOT NULL,
    name NVARCHAR(255) NOT NULL,
    parent_id INT NULL,
    path NVARCHAR(4000) NULL,
    slug NVARCHAR(255) NULL,
    color NVARCHAR(7) NULL,
    icon NVARCHAR(50) NULL,
    description NVARCHAR(MAX) NULL,
    is_public BIT DEFAULT 0,
    public_slug NVARCHAR(255) NULL,
    public_description NVARCHAR(MAX) NULL,
    created_at DATETIME DEFAULT GETDATE(),
    updated_at DATETIME DEFAULT GETDATE(),
    created_by INT NULL,
    deleted_at DATETIME NULL,
    CONSTRAINT fk_tags_parent FOREIGN KEY (parent_id) REFERENCES tags(id),
    CONSTRAINT fk_tags_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    CONSTRAINT uq_tags_user_name UNIQUE (user_id, name),
    CONSTRAINT uq_tags_user_slug UNIQUE (user_id, slug),
    CONSTRAINT uq_tags_public_slug UNIQUE (public_slug)
);
GO

-- Tag paths (closure table)
CREATE TABLE tag_paths (
    ancestor_id INT NOT NULL,
    descendant_id INT NOT NULL,
    depth INT NOT NULL,
    PRIMARY KEY (ancestor_id, descendant_id),
    CONSTRAINT fk_paths_ancestor FOREIGN KEY (ancestor_id) REFERENCES tags(id) ON DELETE CASCADE,
    CONSTRAINT fk_paths_descendant FOREIGN KEY (descendant_id) REFERENCES tags(id)
);
GO

-- Taggables
CREATE TABLE taggables (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    user_id INT NOT NULL,
    tag_id INT NOT NULL,
    taggable_id BIGINT NOT NULL,
    taggable_type NVARCHAR(50) NOT NULL,
    created_at DATETIME DEFAULT GETDATE(),
    created_by INT NULL,
    CONSTRAINT fk_taggables_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    CONSTRAINT fk_taggables_tag FOREIGN KEY (tag_id) REFERENCES tags(id) ON DELETE CASCADE,
    CONSTRAINT fk_taggables_type FOREIGN KEY (taggable_type) REFERENCES entity_types(type_name),
    CONSTRAINT uq_taggables UNIQUE (user_id, tag_id, taggable_id, taggable_type)
);
GO

-- Groups
CREATE TABLE groups (
    id INT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(255) NOT NULL,
    type NVARCHAR(50) NULL,
    description NVARCHAR(MAX) NULL,
    created_by INT NOT NULL,
    created_at DATETIME DEFAULT GETDATE(),
    CONSTRAINT fk_group_creator FOREIGN KEY (created_by) REFERENCES users(id)
);
GO

-- Group members
CREATE TABLE group_members (
    group_id INT NOT NULL,
    user_id INT NOT NULL,
    role NVARCHAR(50) DEFAULT 'member',
    joined_at DATETIME DEFAULT GETDATE(),
    PRIMARY KEY (group_id, user_id),
    CONSTRAINT fk_member_group FOREIGN KEY (group_id) REFERENCES groups(id) ON DELETE CASCADE,
    CONSTRAINT fk_member_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
);
GO

-- Tag shares
CREATE TABLE tag_shares (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    tag_id INT NOT NULL,
    owner_id INT NOT NULL,
    shared_with_id INT NOT NULL,
    can_read BIT DEFAULT 1,
    can_write BIT DEFAULT 0,
    can_tag BIT DEFAULT 0,
    can_untag BIT DEFAULT 0,
    can_reshare BIT DEFAULT 0,
    include_children BIT DEFAULT 1,
    shared_at DATETIME DEFAULT GETDATE(),
    shared_by INT NOT NULL,
    expires_at DATETIME NULL,
    revoked_at DATETIME NULL,
    CONSTRAINT fk_share_tag FOREIGN KEY (tag_id) REFERENCES tags(id) ON DELETE CASCADE,
    CONSTRAINT fk_share_owner FOREIGN KEY (owner_id) REFERENCES users(id),
    CONSTRAINT fk_share_recipient FOREIGN KEY (shared_with_id) REFERENCES users(id),
    CONSTRAINT fk_share_by FOREIGN KEY (shared_by) REFERENCES users(id),
    CONSTRAINT uq_tag_share UNIQUE (tag_id, owner_id, shared_with_id)
);
GO

-- Tag share groups
CREATE TABLE tag_share_groups (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    tag_id INT NOT NULL,
    owner_id INT NOT NULL,
    group_id INT NOT NULL,
    can_read BIT DEFAULT 1,
    can_write BIT DEFAULT 0,
    can_tag BIT DEFAULT 0,
    can_untag BIT DEFAULT 0,
    can_reshare BIT DEFAULT 0,
    include_children BIT DEFAULT 1,
    shared_at DATETIME DEFAULT GETDATE(),
    shared_by INT NOT NULL,
    revoked_at DATETIME NULL,
    CONSTRAINT fk_share_group_tag FOREIGN KEY (tag_id) REFERENCES tags(id) ON DELETE CASCADE,
    CONSTRAINT fk_share_group_owner FOREIGN KEY (owner_id) REFERENCES users(id),
    CONSTRAINT fk_share_group FOREIGN KEY (group_id) REFERENCES groups(id),
    CONSTRAINT uq_tag_share_group UNIQUE (tag_id, owner_id, group_id)
);
GO

-- Tag share audit
CREATE TABLE tag_share_audit (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    share_id BIGINT NULL,
    action NVARCHAR(50) NOT NULL,
    performed_by INT NOT NULL,
    performed_at DATETIME DEFAULT GETDATE(),
    details NVARCHAR(MAX) NULL,
    CONSTRAINT fk_audit_user FOREIGN KEY (performed_by) REFERENCES users(id)
);
GO

-- Tag templates
CREATE TABLE tag_templates (
    id INT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(255) NOT NULL,
    description NVARCHAR(MAX) NULL,
    category NVARCHAR(100) NULL,
    is_public BIT DEFAULT 0,
    created_by INT NOT NULL,
    use_count INT DEFAULT 0,
    rating DECIMAL(3,2) NULL,
    created_at DATETIME DEFAULT GETDATE(),
    updated_at DATETIME DEFAULT GETDATE(),
    CONSTRAINT fk_template_creator FOREIGN KEY (created_by) REFERENCES users(id)
);
GO

-- Tag template items
CREATE TABLE tag_template_items (
    id INT IDENTITY(1,1) PRIMARY KEY,
    template_id INT NOT NULL,
    name NVARCHAR(255) NOT NULL,
    parent_item_id INT NULL,
    color NVARCHAR(7) NULL,
    icon NVARCHAR(50) NULL,
    description NVARCHAR(MAX) NULL,
    sort_order INT DEFAULT 0,
    CONSTRAINT fk_template_item FOREIGN KEY (template_id) REFERENCES tag_templates(id) ON DELETE CASCADE,
    CONSTRAINT fk_template_parent FOREIGN KEY (parent_item_id) REFERENCES tag_template_items(id)
);
GO

-- Tag rules
CREATE TABLE tag_rules (
    id INT IDENTITY(1,1) PRIMARY KEY,
    user_id INT NOT NULL,
    tag_id INT NOT NULL,
    rule_type NVARCHAR(50) NOT NULL,
    config NVARCHAR(MAX) NULL,
    is_active BIT DEFAULT 1,
    priority INT DEFAULT 0,
    created_at DATETIME DEFAULT GETDATE(),
    CONSTRAINT fk_rules_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    CONSTRAINT fk_rules_tag FOREIGN KEY (tag_id) REFERENCES tags(id) ON DELETE CASCADE
);
GO

-- ============================================
-- STEP 3: CREATE INDEXES
-- ============================================

-- Tags indexes
CREATE NONCLUSTERED INDEX idx_tags_user ON tags(user_id, deleted_at) WHERE deleted_at IS NULL;
CREATE NONCLUSTERED INDEX idx_tags_user_parent ON tags(user_id, parent_id);
CREATE NONCLUSTERED INDEX idx_tags_path ON tags(path) WHERE deleted_at IS NULL;
CREATE NONCLUSTERED INDEX idx_tags_slug ON tags(user_id, slug) WHERE slug IS NOT NULL;
CREATE NONCLUSTERED INDEX idx_tags_public ON tags(is_public, public_slug) WHERE is_public = 1 AND deleted_at IS NULL;

-- Tag paths indexes
CREATE NONCLUSTERED INDEX idx_paths_ancestor ON tag_paths(ancestor_id, depth);
CREATE NONCLUSTERED INDEX idx_paths_descendant ON tag_paths(descendant_id, depth);

-- Taggables indexes
CREATE NONCLUSTERED INDEX idx_taggables_entity ON taggables(taggable_type, taggable_id);
CREATE NONCLUSTERED INDEX idx_taggables_tag ON taggables(tag_id);
CREATE NONCLUSTERED INDEX idx_taggables_user ON taggables(user_id);
CREATE NONCLUSTERED INDEX idx_taggables_user_entity ON taggables(user_id, taggable_type, taggable_id);

-- Sharing indexes
CREATE NONCLUSTERED INDEX idx_shares_recipient ON tag_shares(shared_with_id, revoked_at) WHERE revoked_at IS NULL;
CREATE NONCLUSTERED INDEX idx_shares_tag ON tag_shares(tag_id, revoked_at) WHERE revoked_at IS NULL;
CREATE NONCLUSTERED INDEX idx_shares_owner ON tag_shares(owner_id);
CREATE NONCLUSTERED INDEX idx_share_groups_group ON tag_share_groups(group_id, revoked_at) WHERE revoked_at IS NULL;

-- Template indexes
CREATE NONCLUSTERED INDEX idx_templates_public ON tag_templates(is_public, category) WHERE is_public = 1;
CREATE NONCLUSTERED INDEX idx_template_items_parent ON tag_template_items(parent_item_id, sort_order);

-- Audit indexes
CREATE NONCLUSTERED INDEX idx_audit_performed ON tag_share_audit(performed_by, performed_at DESC);
CREATE NONCLUSTERED INDEX idx_audit_share ON tag_share_audit(share_id, performed_at DESC);
GO

-- ============================================
-- STEP 4: INSERT INITIAL DATA
-- ============================================

-- Entity types
INSERT INTO entity_types (type_name, table_name) VALUES 
    ('Note', 'notes'),
    ('File', 'files'),
    ('Task', 'tasks');
GO

-- ============================================
-- STEP 5: PRINT SUCCESS MESSAGE
-- ============================================

PRINT '============================================';
PRINT 'Tag-Tree System Installed Successfully!';
PRINT '============================================';
PRINT 'Database: ' + DB_NAME();
PRINT 'Tables created: 15';
PRINT 'Indexes created: 18';
PRINT 'Demo users: 3';
PRINT '';
PRINT 'Next steps:';
PRINT '1. Run triggers (03-Triggers.md)';
PRINT '2. Run procedures (04-Procedures.md)';
PRINT '3. Run functions (05-Functions.md)';
PRINT '============================================';
GO
```

---

## Verification Queries

After installation, run these to verify:

```sql
-- Check all tables exist
SELECT name FROM sys.tables ORDER BY name;

-- Check indexes
SELECT 
    OBJECT_NAME(object_id) AS table_name,
    name AS index_name,
    type_desc
FROM sys.indexes
WHERE OBJECT_NAME(object_id) LIKE 'tag%' OR OBJECT_NAME(object_id) = 'taggables'
ORDER BY table_name, name;

-- Check foreign keys
SELECT 
    OBJECT_NAME(parent_object_id) AS table_name,
    name AS fk_name,
    OBJECT_NAME(referenced_object_id) AS referenced_table
FROM sys.foreign_keys
ORDER BY table_name;

-- Check users
SELECT * FROM users;

-- Check entity types
SELECT * FROM entity_types;
```

---

## Quick Start Demo

```sql
-- Create a tag for user 1
EXEC sp_create_tag 
    @user_id = 1, 
    @name = 'Work',
    @color = '#0066CC';

EXEC sp_create_tag 
    @user_id = 1, 
    @name = 'Personal',
    @color = '#FF6600';

-- Create child tag
DECLARE @work_id INT;
SELECT @work_id = id FROM tags WHERE user_id = 1 AND name = 'Work';

EXEC sp_create_tag 
    @user_id = 1, 
    @name = 'ProjectX',
    @parent_id = @work_id,
    @color = '#0099FF';

-- View user's tags
EXEC sp_get_user_tags @user_id = 1;

-- Tag an item
EXEC sp_tag_item 
    @user_id = 1, 
    @tag_id = 1, 
    @taggable_id = 123, 
    @taggable_type = 'Note';

-- Query tagged items
EXEC sp_get_tagged_items 
    @user_id = 1, 
    @tag_id = 1, 
    @include_subtree = 1;
```

---

## Uninstall Script

```sql
USE SuperApp-dev;
GO

-- Drop all objects
DROP SECURITY POLICY IF EXISTS tags_security_policy;
DROP TRIGGER IF EXISTS trg_audit_tag_changes;
DROP TRIGGER IF EXISTS trg_cascade_tag_share_revoke;
DROP TRIGGER IF EXISTS trg_validate_tag_delete;
DROP TRIGGER IF EXISTS trg_validate_taggable_user;
DROP TRIGGER IF EXISTS trg_maintain_tag_path;
DROP TRIGGER IF EXISTS trg_maintain_tag_closure;

-- Drop procedures (run all DROP PROCEDURE statements)
-- Drop functions (run all DROP FUNCTION statements)

-- Drop tables in correct order
DROP TABLE IF EXISTS tag_share_audit;
DROP TABLE IF EXISTS tag_template_items;
DROP TABLE IF EXISTS tag_templates;
DROP TABLE IF EXISTS tag_rules;
DROP TABLE IF EXISTS tag_share_groups;
DROP TABLE IF EXISTS tag_shares;
DROP TABLE IF EXISTS group_members;
DROP TABLE IF EXISTS groups;
DROP TABLE IF EXISTS taggables;
DROP TABLE IF EXISTS tag_paths;
DROP TABLE IF EXISTS tags;
DROP TABLE IF EXISTS entity_types;

-- Optional: Drop database
-- USE master;
-- DROP DATABASE SuperApp-dev;
```

---

**See other files for triggers, procedures, and functions code.**
EOF
cat /home/claude/tag-tree-docs/06-Complete-Install-Script.md
Output

# Complete Installation Script

Single script to install entire Tag-Tree system.

---

## Prerequisites

- SQL Server 2016 or later
- CREATE DATABASE permission
- SA or db_owner role

---

## Complete Installation Script

```sql
-- ============================================
-- TAG-TREE SYSTEM - COMPLETE INSTALLATION
-- SQL Server 2016+
-- Version: 1.0
-- ============================================

USE master;
GO

-- Create database (if needed)
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'SuperApp-dev')
BEGIN
    CREATE DATABASE SuperApp-dev;
END;
GO

USE SuperApp-dev;
GO

-- ============================================
-- STEP 1: DROP EXISTING OBJECTS (Optional - for clean install)
-- ============================================
/*
DROP SECURITY POLICY IF EXISTS tags_security_policy;
DROP TRIGGER IF EXISTS trg_audit_tag_changes;
DROP TRIGGER IF EXISTS trg_cascade_tag_share_revoke;
DROP TRIGGER IF EXISTS trg_validate_tag_delete;
DROP TRIGGER IF EXISTS trg_validate_taggable_user;
DROP TRIGGER IF EXISTS trg_maintain_tag_path;
DROP TRIGGER IF EXISTS trg_maintain_tag_closure;
DROP PROCEDURE IF EXISTS sp_apply_tag_template;
DROP PROCEDURE IF EXISTS sp_get_tag_shares;
DROP PROCEDURE IF EXISTS sp_revoke_tag_share;
DROP PROCEDURE IF EXISTS sp_share_tag;
DROP PROCEDURE IF EXISTS sp_search_tags;
DROP PROCEDURE IF EXISTS sp_get_tagged_items;
DROP PROCEDURE IF EXISTS sp_get_tag_breadcrumb;
DROP PROCEDURE IF EXISTS sp_get_tag_subtree;
DROP PROCEDURE IF EXISTS sp_get_user_tags;
DROP PROCEDURE IF EXISTS sp_untag_item;
DROP PROCEDURE IF EXISTS sp_tag_item;
DROP PROCEDURE IF EXISTS sp_delete_tag;
DROP PROCEDURE IF EXISTS sp_move_tag;
DROP PROCEDURE IF EXISTS sp_update_tag;
DROP PROCEDURE IF EXISTS sp_create_tag;
DROP FUNCTION IF EXISTS fn_get_tag_statistics;
DROP FUNCTION IF EXISTS fn_filter_tags_by_user;
DROP FUNCTION IF EXISTS fn_is_tag_ancestor;
DROP FUNCTION IF EXISTS fn_get_tag_descendants_count;
DROP FUNCTION IF EXISTS fn_get_tag_children_count;
DROP FUNCTION IF EXISTS fn_get_tag_depth;
DROP FUNCTION IF EXISTS fn_can_access_tag;
DROP TABLE IF EXISTS tag_share_audit;
DROP TABLE IF EXISTS tag_template_items;
DROP TABLE IF EXISTS tag_templates;
DROP TABLE IF EXISTS tag_rules;
DROP TABLE IF EXISTS tag_share_groups;
DROP TABLE IF EXISTS tag_shares;
DROP TABLE IF EXISTS group_members;
DROP TABLE IF EXISTS groups;
DROP TABLE IF EXISTS taggables;
DROP TABLE IF EXISTS tag_paths;
DROP TABLE IF EXISTS tags;
DROP TABLE IF EXISTS entity_types;
DROP TABLE IF EXISTS users;
*/

-- ============================================
-- STEP 2: CREATE TABLES
-- ============================================

-- Users (if not exists)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'users')
BEGIN
    CREATE TABLE users (
        id INT IDENTITY(1,1) PRIMARY KEY,
        username NVARCHAR(100) NOT NULL UNIQUE,
        email NVARCHAR(255) NOT NULL UNIQUE,
        created_at DATETIME DEFAULT GETDATE()
    );
    
    -- Insert demo users
    INSERT INTO users (username, email) VALUES 
        ('admin', 'admin@example.com'),
        ('user1', 'user1@example.com'),
        ('user2', 'user2@example.com');
END;
GO

-- Entity types
CREATE TABLE entity_types (
    type_name NVARCHAR(50) PRIMARY KEY,
    table_name NVARCHAR(100) NOT NULL,
    is_active BIT DEFAULT 1,
    created_at DATETIME DEFAULT GETDATE()
);
GO

-- Tags
CREATE TABLE tags (
    id INT IDENTITY(1,1) PRIMARY KEY,
    user_id INT NOT NULL,
    name NVARCHAR(255) NOT NULL,
    parent_id INT NULL,
    path NVARCHAR(4000) NULL,
    slug NVARCHAR(255) NULL,
    color NVARCHAR(7) NULL,
    icon NVARCHAR(50) NULL,
    description NVARCHAR(MAX) NULL,
    is_public BIT DEFAULT 0,
    public_slug NVARCHAR(255) NULL,
    public_description NVARCHAR(MAX) NULL,
    created_at DATETIME DEFAULT GETDATE(),
    updated_at DATETIME DEFAULT GETDATE(),
    created_by INT NULL,
    deleted_at DATETIME NULL,
    CONSTRAINT fk_tags_parent FOREIGN KEY (parent_id) REFERENCES tags(id),
    CONSTRAINT fk_tags_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    CONSTRAINT uq_tags_user_name UNIQUE (user_id, name),
    CONSTRAINT uq_tags_user_slug UNIQUE (user_id, slug),
    CONSTRAINT uq_tags_public_slug UNIQUE (public_slug)
);
GO

-- Tag paths (closure table)
CREATE TABLE tag_paths (
    ancestor_id INT NOT NULL,
    descendant_id INT NOT NULL,
    depth INT NOT NULL,
    PRIMARY KEY (ancestor_id, descendant_id),
    CONSTRAINT fk_paths_ancestor FOREIGN KEY (ancestor_id) REFERENCES tags(id) ON DELETE CASCADE,
    CONSTRAINT fk_paths_descendant FOREIGN KEY (descendant_id) REFERENCES tags(id)
);
GO

-- Taggables
CREATE TABLE taggables (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    user_id INT NOT NULL,
    tag_id INT NOT NULL,
    taggable_id BIGINT NOT NULL,
    taggable_type NVARCHAR(50) NOT NULL,
    created_at DATETIME DEFAULT GETDATE(),
    created_by INT NULL,
    CONSTRAINT fk_taggables_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    CONSTRAINT fk_taggables_tag FOREIGN KEY (tag_id) REFERENCES tags(id) ON DELETE CASCADE,
    CONSTRAINT fk_taggables_type FOREIGN KEY (taggable_type) REFERENCES entity_types(type_name),
    CONSTRAINT uq_taggables UNIQUE (user_id, tag_id, taggable_id, taggable_type)
);
GO

-- Groups
CREATE TABLE groups (
    id INT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(255) NOT NULL,
    type NVARCHAR(50) NULL,
    description NVARCHAR(MAX) NULL,
    created_by INT NOT NULL,
    created_at DATETIME DEFAULT GETDATE(),
    CONSTRAINT fk_group_creator FOREIGN KEY (created_by) REFERENCES users(id)
);
GO

-- Group members
CREATE TABLE group_members (
    group_id INT NOT NULL,
    user_id INT NOT NULL,
    role NVARCHAR(50) DEFAULT 'member',
    joined_at DATETIME DEFAULT GETDATE(),
    PRIMARY KEY (group_id, user_id),
    CONSTRAINT fk_member_group FOREIGN KEY (group_id) REFERENCES groups(id) ON DELETE CASCADE,
    CONSTRAINT fk_member_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
);
GO

-- Tag shares
CREATE TABLE tag_shares (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    tag_id INT NOT NULL,
    owner_id INT NOT NULL,
    shared_with_id INT NOT NULL,
    can_read BIT DEFAULT 1,
    can_write BIT DEFAULT 0,
    can_tag BIT DEFAULT 0,
    can_untag BIT DEFAULT 0,
    can_reshare BIT DEFAULT 0,
    include_children BIT DEFAULT 1,
    shared_at DATETIME DEFAULT GETDATE(),
    shared_by INT NOT NULL,
    expires_at DATETIME NULL,
    revoked_at DATETIME NULL,
    CONSTRAINT fk_share_tag FOREIGN KEY (tag_id) REFERENCES tags(id) ON DELETE CASCADE,
    CONSTRAINT fk_share_owner FOREIGN KEY (owner_id) REFERENCES users(id),
    CONSTRAINT fk_share_recipient FOREIGN KEY (shared_with_id) REFERENCES users(id),
    CONSTRAINT fk_share_by FOREIGN KEY (shared_by) REFERENCES users(id),
    CONSTRAINT uq_tag_share UNIQUE (tag_id, owner_id, shared_with_id)
);
GO

-- Tag share groups
CREATE TABLE tag_share_groups (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    tag_id INT NOT NULL,
    owner_id INT NOT NULL,
    group_id INT NOT NULL,
    can_read BIT DEFAULT 1,
    can_write BIT DEFAULT 0,
    can_tag BIT DEFAULT 0,
    can_untag BIT DEFAULT 0,
    can_reshare BIT DEFAULT 0,
    include_children BIT DEFAULT 1,
    shared_at DATETIME DEFAULT GETDATE(),
    shared_by INT NOT NULL,
    revoked_at DATETIME NULL,
    CONSTRAINT fk_share_group_tag FOREIGN KEY (tag_id) REFERENCES tags(id) ON DELETE CASCADE,
    CONSTRAINT fk_share_group_owner FOREIGN KEY (owner_id) REFERENCES users(id),
    CONSTRAINT fk_share_group FOREIGN KEY (group_id) REFERENCES groups(id),
    CONSTRAINT uq_tag_share_group UNIQUE (tag_id, owner_id, group_id)
);
GO

-- Tag share audit
CREATE TABLE tag_share_audit (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    share_id BIGINT NULL,
    action NVARCHAR(50) NOT NULL,
    performed_by INT NOT NULL,
    performed_at DATETIME DEFAULT GETDATE(),
    details NVARCHAR(MAX) NULL,
    CONSTRAINT fk_audit_user FOREIGN KEY (performed_by) REFERENCES users(id)
);
GO

-- Tag templates
CREATE TABLE tag_templates (
    id INT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(255) NOT NULL,
    description NVARCHAR(MAX) NULL,
    category NVARCHAR(100) NULL,
    is_public BIT DEFAULT 0,
    created_by INT NOT NULL,
    use_count INT DEFAULT 0,
    rating DECIMAL(3,2) NULL,
    created_at DATETIME DEFAULT GETDATE(),
    updated_at DATETIME DEFAULT GETDATE(),
    CONSTRAINT fk_template_creator FOREIGN KEY (created_by) REFERENCES users(id)
);
GO

-- Tag template items
CREATE TABLE tag_template_items (
    id INT IDENTITY(1,1) PRIMARY KEY,
    template_id INT NOT NULL,
    name NVARCHAR(255) NOT NULL,
    parent_item_id INT NULL,
    color NVARCHAR(7) NULL,
    icon NVARCHAR(50) NULL,
    description NVARCHAR(MAX) NULL,
    sort_order INT DEFAULT 0,
    CONSTRAINT fk_template_item FOREIGN KEY (template_id) REFERENCES tag_templates(id) ON DELETE CASCADE,
    CONSTRAINT fk_template_parent FOREIGN KEY (parent_item_id) REFERENCES tag_template_items(id)
);
GO

-- Tag rules
CREATE TABLE tag_rules (
    id INT IDENTITY(1,1) PRIMARY KEY,
    user_id INT NOT NULL,
    tag_id INT NOT NULL,
    rule_type NVARCHAR(50) NOT NULL,
    config NVARCHAR(MAX) NULL,
    is_active BIT DEFAULT 1,
    priority INT DEFAULT 0,
    created_at DATETIME DEFAULT GETDATE(),
    CONSTRAINT fk_rules_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    CONSTRAINT fk_rules_tag FOREIGN KEY (tag_id) REFERENCES tags(id) ON DELETE CASCADE
);
GO

-- ============================================
-- STEP 3: CREATE INDEXES
-- ============================================

-- Tags indexes
CREATE NONCLUSTERED INDEX idx_tags_user ON tags(user_id, deleted_at) WHERE deleted_at IS NULL;
CREATE NONCLUSTERED INDEX idx_tags_user_parent ON tags(user_id, parent_id);
CREATE NONCLUSTERED INDEX idx_tags_path ON tags(path) WHERE deleted_at IS NULL;
CREATE NONCLUSTERED INDEX idx_tags_slug ON tags(user_id, slug) WHERE slug IS NOT NULL;
CREATE NONCLUSTERED INDEX idx_tags_public ON tags(is_public, public_slug) WHERE is_public = 1 AND deleted_at IS NULL;

-- Tag paths indexes
CREATE NONCLUSTERED INDEX idx_paths_ancestor ON tag_paths(ancestor_id, depth);
CREATE NONCLUSTERED INDEX idx_paths_descendant ON tag_paths(descendant_id, depth);

-- Taggables indexes
CREATE NONCLUSTERED INDEX idx_taggables_entity ON taggables(taggable_type, taggable_id);
CREATE NONCLUSTERED INDEX idx_taggables_tag ON taggables(tag_id);
CREATE NONCLUSTERED INDEX idx_taggables_user ON taggables(user_id);
CREATE NONCLUSTERED INDEX idx_taggables_user_entity ON taggables(user_id, taggable_type, taggable_id);

-- Sharing indexes
CREATE NONCLUSTERED INDEX idx_shares_recipient ON tag_shares(shared_with_id, revoked_at) WHERE revoked_at IS NULL;
CREATE NONCLUSTERED INDEX idx_shares_tag ON tag_shares(tag_id, revoked_at) WHERE revoked_at IS NULL;
CREATE NONCLUSTERED INDEX idx_shares_owner ON tag_shares(owner_id);
CREATE NONCLUSTERED INDEX idx_share_groups_group ON tag_share_groups(group_id, revoked_at) WHERE revoked_at IS NULL;

-- Template indexes
CREATE NONCLUSTERED INDEX idx_templates_public ON tag_templates(is_public, category) WHERE is_public = 1;
CREATE NONCLUSTERED INDEX idx_template_items_parent ON tag_template_items(parent_item_id, sort_order);

-- Audit indexes
CREATE NONCLUSTERED INDEX idx_audit_performed ON tag_share_audit(performed_by, performed_at DESC);
CREATE NONCLUSTERED INDEX idx_audit_share ON tag_share_audit(share_id, performed_at DESC);
GO

-- ============================================
-- STEP 4: INSERT INITIAL DATA
-- ============================================

-- Entity types
INSERT INTO entity_types (type_name, table_name) VALUES 
    ('Note', 'notes'),
    ('File', 'files'),
    ('Task', 'tasks');
GO

-- ============================================
-- STEP 5: PRINT SUCCESS MESSAGE
-- ============================================

PRINT '============================================';
PRINT 'Tag-Tree System Installed Successfully!';
PRINT '============================================';
PRINT 'Database: ' + DB_NAME();
PRINT 'Tables created: 15';
PRINT 'Indexes created: 18';
PRINT 'Demo users: 3';
PRINT '';
PRINT 'Next steps:';
PRINT '1. Run triggers (03-Triggers.md)';
PRINT '2. Run procedures (04-Procedures.md)';
PRINT '3. Run functions (05-Functions.md)';
PRINT '============================================';
GO
```

---

## Verification Queries

After installation, run these to verify:

```sql
-- Check all tables exist
SELECT name FROM sys.tables ORDER BY name;

-- Check indexes
SELECT 
    OBJECT_NAME(object_id) AS table_name,
    name AS index_name,
    type_desc
FROM sys.indexes
WHERE OBJECT_NAME(object_id) LIKE 'tag%' OR OBJECT_NAME(object_id) = 'taggables'
ORDER BY table_name, name;

-- Check foreign keys
SELECT 
    OBJECT_NAME(parent_object_id) AS table_name,
    name AS fk_name,
    OBJECT_NAME(referenced_object_id) AS referenced_table
FROM sys.foreign_keys
ORDER BY table_name;

-- Check users
SELECT * FROM users;

-- Check entity types
SELECT * FROM entity_types;
```

---

## Quick Start Demo

```sql
-- Create a tag for user 1
EXEC sp_create_tag 
    @user_id = 1, 
    @name = 'Work',
    @color = '#0066CC';

EXEC sp_create_tag 
    @user_id = 1, 
    @name = 'Personal',
    @color = '#FF6600';

-- Create child tag
DECLARE @work_id INT;
SELECT @work_id = id FROM tags WHERE user_id = 1 AND name = 'Work';

EXEC sp_create_tag 
    @user_id = 1, 
    @name = 'ProjectX',
    @parent_id = @work_id,
    @color = '#0099FF';

-- View user's tags
EXEC sp_get_user_tags @user_id = 1;

-- Tag an item
EXEC sp_tag_item 
    @user_id = 1, 
    @tag_id = 1, 
    @taggable_id = 123, 
    @taggable_type = 'Note';

-- Query tagged items
EXEC sp_get_tagged_items 
    @user_id = 1, 
    @tag_id = 1, 
    @include_subtree = 1;
```

---

## Uninstall Script

```sql
USE SuperApp-dev;
GO

-- Drop all objects
DROP SECURITY POLICY IF EXISTS tags_security_policy;
DROP TRIGGER IF EXISTS trg_audit_tag_changes;
DROP TRIGGER IF EXISTS trg_cascade_tag_share_revoke;
DROP TRIGGER IF EXISTS trg_validate_tag_delete;
DROP TRIGGER IF EXISTS trg_validate_taggable_user;
DROP TRIGGER IF EXISTS trg_maintain_tag_path;
DROP TRIGGER IF EXISTS trg_maintain_tag_closure;

-- Drop procedures (run all DROP PROCEDURE statements)
-- Drop functions (run all DROP FUNCTION statements)

-- Drop tables in correct order
DROP TABLE IF EXISTS tag_share_audit;
DROP TABLE IF EXISTS tag_template_items;
DROP TABLE IF EXISTS tag_templates;
DROP TABLE IF EXISTS tag_rules;
DROP TABLE IF EXISTS tag_share_groups;
DROP TABLE IF EXISTS tag_shares;
DROP TABLE IF EXISTS group_members;
DROP TABLE IF EXISTS groups;
DROP TABLE IF EXISTS taggables;
DROP TABLE IF EXISTS tag_paths;
DROP TABLE IF EXISTS tags;
DROP TABLE IF EXISTS entity_types;

-- Optional: Drop database
-- USE master;
-- DROP DATABASE SuperApp-dev;
```

---

**See other files for triggers, procedures, and functions code.**