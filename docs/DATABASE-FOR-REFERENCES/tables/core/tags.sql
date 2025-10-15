-- ============================================
-- FILE: tables/core/tags.sql
-- PURPOSE: Tags table - Global tag pool (shared across workspaces)
-- SCALE: 2,000+ tags per user, 2M+ total
-- DEPENDENCIES: users.sql
-- ============================================

PRINT '📦 Creating tags table...';
GO

-- Drop table if exists (for clean reinstall)
-- WARNING: This will delete all data!
/*
IF OBJECT_ID('tags', 'U') IS NOT NULL DROP TABLE tags;
*/

-- ============================================
-- TABLE: tags
-- PURPOSE: Global tag pool (shared across workspaces)
-- SCALE: 2,000+ tags per user, 2M+ total
-- ============================================

CREATE TABLE tags (
    id INT IDENTITY(1,1) PRIMARY KEY,
    
    -- Owner
    user_id INT NOT NULL,
    
    -- Tag information
    name NVARCHAR(255) NOT NULL,
    slug NVARCHAR(255), -- URL-friendly version: "My Tag" -> "my-tag"
    
    -- Visual properties
    color NVARCHAR(7), -- Hex color: #FF5733
    icon NVARCHAR(50), -- Icon name/emoji: "folder", "📁"
    
    -- Description
    description NVARCHAR(MAX),
    
    -- Metadata
    metadata NVARCHAR(MAX), -- JSON: { custom_fields, ... }
    
    -- Statistics (denormalized for performance)
    usage_count INT DEFAULT 0, -- How many workspaces use this tag
    
    -- Timestamps
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    updated_at DATETIME2 DEFAULT GETUTCDATE(),
    deleted_at DATETIME2 NULL, -- Soft delete
    
    -- Constraints
    CONSTRAINT fk_tags_user FOREIGN KEY (user_id) 
        REFERENCES users(id) ON DELETE CASCADE,
    
    -- Unique: user can't have duplicate tag names (unless one is deleted)
    CONSTRAINT uq_tags_user_name UNIQUE (user_id, name, deleted_at)
);

-- Indexes for tags table

-- Primary lookup: Get user's tags
CREATE INDEX ix_tags_user ON tags(user_id, deleted_at) 
    INCLUDE (name, color, icon)
    WHERE deleted_at IS NULL;

-- Search by name (autocomplete, prefix search)
CREATE INDEX ix_tags_name ON tags(name, user_id, deleted_at) 
    WHERE deleted_at IS NULL;

-- Search within user's tags
CREATE INDEX ix_tags_user_name ON tags(user_id, name, deleted_at) 
    WHERE deleted_at IS NULL;

-- Find unused tags (for cleanup)
CREATE INDEX ix_tags_usage ON tags(user_id, usage_count, deleted_at) 
    WHERE deleted_at IS NULL;

-- Slug lookup (for URLs)
CREATE INDEX ix_tags_slug ON tags(user_id, slug, deleted_at) 
    WHERE deleted_at IS NULL AND slug IS NOT NULL;

-- Comments
EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description',
    @value = N'Global tag pool. Tags are reusable across multiple workspaces. Each tag belongs to a user.',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE', @level1name = N'tags';

EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description',
    @value = N'Denormalized count of workspaces using this tag. Updated by triggers.',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE', @level1name = N'tags',
    @level2type = N'COLUMN', @level2name = N'usage_count';

EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description',
    @value = N'JSON format: {"priority": "high", "category": "work", ...}',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE', @level1name = N'tags',
    @level2type = N'COLUMN', @level2name = N'metadata';

GO

-- ============================================
-- TRIGGER: Auto-generate slug when tag created
-- ============================================

CREATE OR ALTER TRIGGER tr_tags_generate_slug
ON tags
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Generate slug for new/updated tags without slug
    UPDATE t
    SET slug = LOWER(
        REPLACE(
            REPLACE(
                REPLACE(
                    REPLACE(i.name, ' ', '-'),
                    '/', '-'),
                '_', '-'),
            '--', '-')
    )
    FROM tags t
    INNER JOIN inserted i ON t.id = i.id
    WHERE t.slug IS NULL OR t.slug = '';
    
    -- Remove leading/trailing dashes
    UPDATE tags
    SET slug = LTRIM(RTRIM(slug))
    WHERE id IN (SELECT id FROM inserted);
END;
GO

-- ============================================
-- TRIGGER: Update tags.updated_at on change
-- ============================================

CREATE OR ALTER TRIGGER tr_tags_updated_at
ON tags
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE tags
    SET updated_at = GETUTCDATE()
    WHERE id IN (SELECT id FROM inserted);
END;
GO

-- ============================================
-- VIEW: Active tags with user info
-- ============================================

CREATE OR ALTER VIEW vw_active_tags AS
SELECT 
    t.id,
    t.user_id,
    u.username,
    u.display_name AS user_display_name,
    t.name,
    t.slug,
    t.color,
    t.icon,
    t.description,
    t.usage_count,
    t.created_at,
    t.updated_at
FROM tags t
INNER JOIN users u ON t.user_id = u.id
WHERE t.deleted_at IS NULL
AND u.deleted_at IS NULL;
GO

PRINT '   ✅ tags table created successfully';
GO
