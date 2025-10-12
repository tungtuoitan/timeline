# Database Schema

Complete SQL Server schema for the Tag-Tree system with multi-user support.

---

## Table of Contents

1. [Core Tables](#1-core-tables)
2. [Sharing Tables](#2-sharing-tables)
3. [Template Tables](#3-template-tables)
4. [Support Tables](#4-support-tables)
5. [Indexes](#5-indexes)
6. [Constraints Summary](#6-constraints-summary)

---

## 1. Core Tables

### 1.1 tags

Primary table storing the tag hierarchy.

```sql
CREATE TABLE tags (
    -- Identity
    id INT IDENTITY(1,1) PRIMARY KEY,
    user_id INT NOT NULL,
    
    -- Tag data
    name NVARCHAR(255) NOT NULL,
    parent_id INT NULL,
    
    -- Hierarchy helpers
    path NVARCHAR(4000) NULL,  -- Materialized path: 'Work.ProjectX.Phase1'
    
    -- Metadata
    slug NVARCHAR(255) NULL,   -- URL-friendly: 'work-project-x'
    color NVARCHAR(7) NULL,    -- Hex color: '#FF5733'
    icon NVARCHAR(50) NULL,    -- Icon name: 'folder', 'star'
    description NVARCHAR(MAX) NULL,
    
    -- Public sharing (optional feature)
    is_public BIT DEFAULT 0,
    public_slug NVARCHAR(255) NULL,
    public_description NVARCHAR(MAX) NULL,
    
    -- Audit
    created_at DATETIME DEFAULT GETDATE(),
    updated_at DATETIME DEFAULT GETDATE(),
    created_by INT NULL,
    
    -- Soft delete
    deleted_at DATETIME NULL,
    
    -- Constraints
    CONSTRAINT fk_tags_parent FOREIGN KEY (parent_id) 
        REFERENCES tags(id),
    CONSTRAINT fk_tags_user FOREIGN KEY (user_id) 
        REFERENCES users(id) ON DELETE CASCADE,
    CONSTRAINT uq_tags_user_name UNIQUE (user_id, name),
    CONSTRAINT uq_tags_user_slug UNIQUE (user_id, slug),
    CONSTRAINT uq_tags_public_slug UNIQUE (public_slug)
);
```

**Column Details:**

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| id | INT | No | Primary key, auto-increment |
| user_id | INT | No | Owner of the tag |
| name | NVARCHAR(255) | No | Display name (unique per user) |
| parent_id | INT | Yes | Parent tag (NULL = root) |
| path | NVARCHAR(4000) | Yes | Full path (auto-maintained by trigger) |
| slug | NVARCHAR(255) | Yes | URL-safe identifier |
| color | NVARCHAR(7) | Yes | UI color in hex format |
| icon | NVARCHAR(50) | Yes | Icon identifier |
| description | NVARCHAR(MAX) | Yes | Long description |
| is_public | BIT | No | Whether tag is in public marketplace |
| public_slug | NVARCHAR(255) | Yes | Global unique slug for public tags |
| created_at | DATETIME | No | Creation timestamp |
| updated_at | DATETIME | No | Last modification timestamp |
| deleted_at | DATETIME | Yes | Soft delete timestamp |

**Indexes:** See [Section 5](#5-indexes)

---

### 1.2 tag_paths

Closure table storing all ancestor-descendant relationships.

```sql
CREATE TABLE tag_paths (
    ancestor_id INT NOT NULL,
    descendant_id INT NOT NULL,
    depth INT NOT NULL,
    
    PRIMARY KEY (ancestor_id, descendant_id),
    
    CONSTRAINT fk_paths_ancestor FOREIGN KEY (ancestor_id) 
        REFERENCES tags(id) ON DELETE CASCADE,
    CONSTRAINT fk_paths_descendant FOREIGN KEY (descendant_id) 
        REFERENCES tags(id)
);
```

**Column Details:**

| Column | Type | Description |
|--------|------|-------------|
| ancestor_id | INT | Ancestor tag (including self) |
| descendant_id | INT | Descendant tag (including self) |
| depth | INT | Distance (0 = self-reference) |

**Example Data:**

For tree: Work → ProjectX → Phase1

| ancestor_id | descendant_id | depth | Meaning |
|-------------|---------------|-------|---------|
| 1 (Work) | 1 (Work) | 0 | Self |
| 1 (Work) | 2 (ProjectX) | 1 | Child |
| 1 (Work) | 3 (Phase1) | 2 | Grandchild |
| 2 (ProjectX) | 2 (ProjectX) | 0 | Self |
| 2 (ProjectX) | 3 (Phase1) | 1 | Child |
| 3 (Phase1) | 3 (Phase1) | 0 | Self |

**Maintained by:** Triggers (see [03-Triggers.md](03-Triggers.md))

---

### 1.3 taggables

Polymorphic junction table linking tags to any entity type.

```sql
CREATE TABLE taggables (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    user_id INT NOT NULL,
    tag_id INT NOT NULL,
    taggable_id BIGINT NOT NULL,
    taggable_type NVARCHAR(50) NOT NULL,
    
    -- Audit
    created_at DATETIME DEFAULT GETDATE(),
    created_by INT NULL,
    
    CONSTRAINT fk_taggables_user FOREIGN KEY (user_id) 
        REFERENCES users(id) ON DELETE CASCADE,
    CONSTRAINT fk_taggables_tag FOREIGN KEY (tag_id) 
        REFERENCES tags(id) ON DELETE CASCADE,
    CONSTRAINT fk_taggables_type FOREIGN KEY (taggable_type) 
        REFERENCES entity_types(type_name),
    CONSTRAINT uq_taggables UNIQUE (user_id, tag_id, taggable_id, taggable_type)
);
```

**Column Details:**

| Column | Type | Description |
|--------|------|-------------|
| id | BIGINT | Primary key |
| user_id | INT | Owner (must match tag owner or have permission) |
| tag_id | INT | Tag being applied |
| taggable_id | BIGINT | Entity ID being tagged |
| taggable_type | NVARCHAR(50) | Entity type ('Note', 'File', 'Task') |
| created_at | DATETIME | When tagged |
| created_by | INT | User who applied the tag |

**Usage Example:**

```sql
-- Tag note #123 with tag #5
INSERT INTO taggables (user_id, tag_id, taggable_id, taggable_type, created_by)
VALUES (1, 5, 123, 'Note', 1);
```

---

## 2. Sharing Tables

### 2.1 tag_shares

User-to-user tag sharing.

```sql
CREATE TABLE tag_shares (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    tag_id INT NOT NULL,
    owner_id INT NOT NULL,
    shared_with_id INT NOT NULL,
    
    -- Permissions
    can_read BIT DEFAULT 1,
    can_write BIT DEFAULT 0,
    can_tag BIT DEFAULT 0,
    can_untag BIT DEFAULT 0,
    can_reshare BIT DEFAULT 0,
    
    -- Scope
    include_children BIT DEFAULT 1,
    
    -- Lifecycle
    shared_at DATETIME DEFAULT GETDATE(),
    shared_by INT NOT NULL,
    expires_at DATETIME NULL,
    revoked_at DATETIME NULL,
    
    CONSTRAINT fk_share_tag FOREIGN KEY (tag_id) 
        REFERENCES tags(id) ON DELETE CASCADE,
    CONSTRAINT fk_share_owner FOREIGN KEY (owner_id) 
        REFERENCES users(id),
    CONSTRAINT fk_share_recipient FOREIGN KEY (shared_with_id) 
        REFERENCES users(id),
    CONSTRAINT fk_share_by FOREIGN KEY (shared_by) 
        REFERENCES users(id),
    CONSTRAINT uq_tag_share UNIQUE (tag_id, owner_id, shared_with_id)
);
```

**Permission Flags:**

| Flag | Description |
|------|-------------|
| can_read | View tag name, color, description |
| can_write | Edit tag metadata |
| can_tag | Apply tag to items |
| can_untag | Remove tag from items |
| can_reshare | Share tag with other users |

**include_children:**
- TRUE: Share includes entire subtree
- FALSE: Only the specific tag

---

### 2.2 tag_share_groups

Share tags with groups/teams.

```sql
CREATE TABLE tag_share_groups (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    tag_id INT NOT NULL,
    owner_id INT NOT NULL,
    group_id INT NOT NULL,
    
    -- Same permissions as tag_shares
    can_read BIT DEFAULT 1,
    can_write BIT DEFAULT 0,
    can_tag BIT DEFAULT 0,
    can_untag BIT DEFAULT 0,
    can_reshare BIT DEFAULT 0,
    include_children BIT DEFAULT 1,
    
    shared_at DATETIME DEFAULT GETDATE(),
    shared_by INT NOT NULL,
    revoked_at DATETIME NULL,
    
    CONSTRAINT fk_share_group_tag FOREIGN KEY (tag_id) 
        REFERENCES tags(id) ON DELETE CASCADE,
    CONSTRAINT fk_share_group_owner FOREIGN KEY (owner_id) 
        REFERENCES users(id),
    CONSTRAINT fk_share_group FOREIGN KEY (group_id) 
        REFERENCES groups(id),
    CONSTRAINT uq_tag_share_group UNIQUE (tag_id, owner_id, group_id)
);
```

---

### 2.3 groups

Teams or organizations.

```sql
CREATE TABLE groups (
    id INT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(255) NOT NULL,
    type NVARCHAR(50) NULL,  -- 'team', 'organization', 'project'
    description NVARCHAR(MAX) NULL,
    created_by INT NOT NULL,
    created_at DATETIME DEFAULT GETDATE(),
    
    CONSTRAINT fk_group_creator FOREIGN KEY (created_by) 
        REFERENCES users(id)
);
```

---

### 2.4 group_members

Group membership.

```sql
CREATE TABLE group_members (
    group_id INT NOT NULL,
    user_id INT NOT NULL,
    role NVARCHAR(50) DEFAULT 'member',  -- 'admin', 'member', 'viewer'
    joined_at DATETIME DEFAULT GETDATE(),
    
    PRIMARY KEY (group_id, user_id),
    
    CONSTRAINT fk_member_group FOREIGN KEY (group_id) 
        REFERENCES groups(id) ON DELETE CASCADE,
    CONSTRAINT fk_member_user FOREIGN KEY (user_id) 
        REFERENCES users(id) ON DELETE CASCADE
);
```

---

### 2.5 tag_share_audit

Audit log for all sharing operations.

```sql
CREATE TABLE tag_share_audit (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    share_id BIGINT NULL,
    action NVARCHAR(50) NOT NULL,  -- 'created', 'updated', 'revoked', 'accessed'
    performed_by INT NOT NULL,
    performed_at DATETIME DEFAULT GETDATE(),
    details NVARCHAR(MAX) NULL,  -- JSON with additional context
    
    CONSTRAINT fk_audit_user FOREIGN KEY (performed_by) 
        REFERENCES users(id)
);
```

---

## 3. Template Tables

### 3.1 tag_templates

Reusable tag structures.

```sql
CREATE TABLE tag_templates (
    id INT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(255) NOT NULL,
    description NVARCHAR(MAX) NULL,
    category NVARCHAR(100) NULL,  -- 'productivity', 'development', 'finance'
    
    -- Visibility
    is_public BIT DEFAULT 0,
    created_by INT NOT NULL,
    
    -- Statistics
    use_count INT DEFAULT 0,
    rating DECIMAL(3,2) NULL,  -- Average rating 0.00-5.00
    
    created_at DATETIME DEFAULT GETDATE(),
    updated_at DATETIME DEFAULT GETDATE(),
    
    CONSTRAINT fk_template_creator FOREIGN KEY (created_by) 
        REFERENCES users(id)
);
```

---

### 3.2 tag_template_items

Structure of template (hierarchy).

```sql
CREATE TABLE tag_template_items (
    id INT IDENTITY(1,1) PRIMARY KEY,
    template_id INT NOT NULL,
    name NVARCHAR(255) NOT NULL,
    parent_item_id INT NULL,
    color NVARCHAR(7) NULL,
    icon NVARCHAR(50) NULL,
    description NVARCHAR(MAX) NULL,
    sort_order INT DEFAULT 0,
    
    CONSTRAINT fk_template_item FOREIGN KEY (template_id) 
        REFERENCES tag_templates(id) ON DELETE CASCADE,
    CONSTRAINT fk_template_parent FOREIGN KEY (parent_item_id) 
        REFERENCES tag_template_items(id)
);
```

**Example Template:**

Template: "Software Development"
```
tag_template_items:
id | template_id | name       | parent_item_id
1  | 1          | Frontend   | NULL
2  | 1          | Backend    | NULL
3  | 1          | React      | 1
4  | 1          | Angular    | 1
5  | 1          | Node.js    | 2
6  | 1          | Python     | 2
```

---

## 4. Support Tables

### 4.1 entity_types

Whitelist of taggable entity types.

```sql
CREATE TABLE entity_types (
    type_name NVARCHAR(50) PRIMARY KEY,
    table_name NVARCHAR(100) NOT NULL,
    is_active BIT DEFAULT 1,
    created_at DATETIME DEFAULT GETDATE()
);

-- Initial data
INSERT INTO entity_types (type_name, table_name) VALUES 
    ('Note', 'notes'),
    ('File', 'files'),
    ('Task', 'tasks');
```

**Purpose:** Type safety for polymorphic association

---

### 4.2 tag_rules

Automation rules (future feature).

```sql
CREATE TABLE tag_rules (
    id INT IDENTITY(1,1) PRIMARY KEY,
    user_id INT NOT NULL,
    tag_id INT NOT NULL,
    rule_type NVARCHAR(50) NOT NULL,  -- 'auto-assign', 'workflow', 'auto-classify'
    config NVARCHAR(MAX) NULL,        -- JSON configuration
    is_active BIT DEFAULT 1,
    priority INT DEFAULT 0,
    
    created_at DATETIME DEFAULT GETDATE(),
    
    CONSTRAINT fk_rules_user FOREIGN KEY (user_id) 
        REFERENCES users(id) ON DELETE CASCADE,
    CONSTRAINT fk_rules_tag FOREIGN KEY (tag_id) 
        REFERENCES tags(id) ON DELETE CASCADE
);
```

**Example Config (JSON):**

```json
{
  "rule_type": "auto-assign",
  "condition": {
    "parent_tag": "Work",
    "add_child": "Work.ProjectX"
  }
}
```

---

### 4.3 users

User accounts (assumed to exist in your system).

```sql
-- Reference table (may already exist in your application)
-- Shown here for completeness

CREATE TABLE users (
    id INT IDENTITY(1,1) PRIMARY KEY,
    username NVARCHAR(100) NOT NULL UNIQUE,
    email NVARCHAR(255) NOT NULL UNIQUE,
    created_at DATETIME DEFAULT GETDATE()
);
```

---

## 5. Indexes

### 5.1 tags Table

```sql
-- User isolation and soft delete filter
CREATE NONCLUSTERED INDEX idx_tags_user 
ON tags(user_id, deleted_at)
WHERE deleted_at IS NULL;

-- Parent lookups
CREATE NONCLUSTERED INDEX idx_tags_user_parent 
ON tags(user_id, parent_id);

-- Path searches (for materialized path queries)
CREATE NONCLUSTERED INDEX idx_tags_path 
ON tags(path)
WHERE deleted_at IS NULL;

-- Slug lookups
CREATE NONCLUSTERED INDEX idx_tags_slug 
ON tags(user_id, slug)
WHERE slug IS NOT NULL;

-- Public tags
CREATE NONCLUSTERED INDEX idx_tags_public 
ON tags(is_public, public_slug)
WHERE is_public = 1 AND deleted_at IS NULL;
```

### 5.2 tag_paths Table

```sql
-- Subtree queries (most common)
CREATE NONCLUSTERED INDEX idx_paths_ancestor 
ON tag_paths(ancestor_id, depth);

-- Ancestor queries (breadcrumbs)
CREATE NONCLUSTERED INDEX idx_paths_descendant 
ON tag_paths(descendant_id, depth);
```

### 5.3 taggables Table

```sql
-- Entity lookups
CREATE NONCLUSTERED INDEX idx_taggables_entity 
ON taggables(taggable_type, taggable_id);

-- Tag lookups
CREATE NONCLUSTERED INDEX idx_taggables_tag 
ON taggables(tag_id);

-- User isolation
CREATE NONCLUSTERED INDEX idx_taggables_user 
ON taggables(user_id);

-- Combined for common queries
CREATE NONCLUSTERED INDEX idx_taggables_user_entity 
ON taggables(user_id, taggable_type, taggable_id);
```

### 5.4 Sharing Tables

```sql
-- Tag shares by recipient
CREATE NONCLUSTERED INDEX idx_shares_recipient 
ON tag_shares(shared_with_id, revoked_at)
WHERE revoked_at IS NULL;

-- Tag shares by tag
CREATE NONCLUSTERED INDEX idx_shares_tag 
ON tag_shares(tag_id, revoked_at)
WHERE revoked_at IS NULL;

-- Tag shares by owner
CREATE NONCLUSTERED INDEX idx_shares_owner 
ON tag_shares(owner_id);

-- Group shares
CREATE NONCLUSTERED INDEX idx_share_groups_group 
ON tag_share_groups(group_id, revoked_at)
WHERE revoked_at IS NULL;
```

### 5.5 Template Tables

```sql
-- Public templates
CREATE NONCLUSTERED INDEX idx_templates_public 
ON tag_templates(is_public, category)
WHERE is_public = 1;

-- Template items hierarchy
CREATE NONCLUSTERED INDEX idx_template_items_parent 
ON tag_template_items(parent_item_id, sort_order);
```

### 5.6 Audit Tables

```sql
-- Audit log queries
CREATE NONCLUSTERED INDEX idx_audit_performed 
ON tag_share_audit(performed_by, performed_at DESC);

CREATE NONCLUSTERED INDEX idx_audit_share 
ON tag_share_audit(share_id, performed_at DESC);
```

---

## 6. Constraints Summary

### 6.1 Primary Keys

| Table | Primary Key |
|-------|-------------|
| tags | id |
| tag_paths | (ancestor_id, descendant_id) |
| taggables | id |
| tag_shares | id |
| tag_share_groups | id |
| groups | id |
| group_members | (group_id, user_id) |
| tag_share_audit | id |
| tag_templates | id |
| tag_template_items | id |
| entity_types | type_name |
| tag_rules | id |
| users | id |

### 6.2 Foreign Keys

**tags:**
- parent_id → tags(id)
- user_id → users(id) CASCADE DELETE

**tag_paths:**
- ancestor_id → tags(id) CASCADE DELETE
- descendant_id → tags(id) CASCADE DELETE

**taggables:**
- user_id → users(id) CASCADE DELETE
- tag_id → tags(id) CASCADE DELETE
- taggable_type → entity_types(type_name)

**tag_shares:**
- tag_id → tags(id) CASCADE DELETE
- owner_id → users(id)
- shared_with_id → users(id)
- shared_by → users(id)

**And more...** (see individual table definitions)

### 6.3 Unique Constraints

| Table | Unique Constraint |
|-------|------------------|
| tags | (user_id, name) |
| tags | (user_id, slug) |
| tags | public_slug |
| taggables | (user_id, tag_id, taggable_id, taggable_type) |
| tag_shares | (tag_id, owner_id, shared_with_id) |
| tag_share_groups | (tag_id, owner_id, group_id) |
| users | username |
| users | email |

### 6.4 Check Constraints

None explicitly defined, but can add:

```sql
-- Example: Ensure valid hex colors
ALTER TABLE tags
ADD CONSTRAINT chk_tags_color 
CHECK (color IS NULL OR color LIKE '#[0-9A-F][0-9A-F][0-9A-F][0-9A-F][0-9A-F][0-9A-F]');

-- Example: Ensure valid ratings
ALTER TABLE tag_templates
ADD CONSTRAINT chk_template_rating 
CHECK (rating IS NULL OR (rating >= 0 AND rating <= 5));
```

---

## 7. Complete Schema Script

```sql
-- ============================================
-- Tag-Tree System - Complete Schema
-- SQL Server 2016+
-- ============================================

-- Users table (if not exists)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'users')
BEGIN
    CREATE TABLE users (
        id INT IDENTITY(1,1) PRIMARY KEY,
        username NVARCHAR(100) NOT NULL UNIQUE,
        email NVARCHAR(255) NOT NULL UNIQUE,
        created_at DATETIME DEFAULT GETDATE()
    );
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

-- Core: Tags
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

-- Core: Closure table
CREATE TABLE tag_paths (
    ancestor_id INT NOT NULL,
    descendant_id INT NOT NULL,
    depth INT NOT NULL,
    PRIMARY KEY (ancestor_id, descendant_id),
    CONSTRAINT fk_paths_ancestor FOREIGN KEY (ancestor_id) REFERENCES tags(id) ON DELETE CASCADE,
    CONSTRAINT fk_paths_descendant FOREIGN KEY (descendant_id) REFERENCES tags(id)
);
GO

-- Core: Taggables
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

-- Sharing: Groups
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

-- Sharing: Group members
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

-- Sharing: Tag shares (user-to-user)
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

-- Sharing: Tag share groups
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

-- Sharing: Audit log
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

-- Templates: Tag templates
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

-- Templates: Template items
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

-- Support: Tag rules
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

-- Initial entity types
INSERT INTO entity_types (type_name, table_name) VALUES 
    ('Note', 'notes'),
    ('File', 'files'),
    ('Task', 'tasks');
GO

-- All indexes in Section 5
-- (See index creation scripts above)
```

---

**Next:** See [03-Triggers.md](03-Triggers.md) for trigger implementations.
