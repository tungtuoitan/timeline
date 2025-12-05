-- =============================================
-- CREATE DATABASE SCHEMA - Complete Setup
-- Description: Create all tables from scratch with latest schema
-- Author: Claude Code
-- Date: 2025-01-30
-- Version: 2.0 (Post-migration schema)
-- =============================================

USE SuperApp-dev;
GO

PRINT '========================================';
PRINT 'CREATING DATABASE SCHEMA';
PRINT 'Time: ' + CONVERT(VARCHAR, GETUTCDATE(), 120);
PRINT '========================================';
GO

-- =============================================
-- PHASE 1: CORE TABLES
-- =============================================
PRINT '';
PRINT 'PHASE 1: Creating core tables...';

-- Table: users
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'users')
BEGIN
    CREATE TABLE users (
        user_id INT IDENTITY(1,1) PRIMARY KEY,
        email NVARCHAR(255) NOT NULL UNIQUE,
        name NVARCHAR(255) NOT NULL,
        avatar_url NVARCHAR(500),
        auth_provider NVARCHAR(50) NOT NULL, -- 'google', 'email'
        auth_provider_id NVARCHAR(255), -- ID from OAuth provider
        email_verified BIT DEFAULT 0,
        is_active BIT DEFAULT 1,
        created_at DATETIME2 DEFAULT GETUTCDATE(),
        updated_at DATETIME2,
        deleted_at DATETIME2,

        INDEX IX_users_email (email) WHERE deleted_at IS NULL,
        INDEX IX_users_auth (auth_provider, auth_provider_id) WHERE deleted_at IS NULL
    );
    PRINT '  ✓ users table created';
END;
GO

-- Table: user_profiles
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'user_profiles')
BEGIN
    CREATE TABLE user_profiles (
        profile_id INT IDENTITY(1,1) PRIMARY KEY,
        user_id INT NOT NULL,
        bio NVARCHAR(1000),
        phone NVARCHAR(50),
        timezone NVARCHAR(50) DEFAULT 'UTC',
        language NVARCHAR(10) DEFAULT 'en',
        settings NVARCHAR(MAX), -- JSON
        created_at DATETIME2 DEFAULT GETUTCDATE(),
        updated_at DATETIME2,

        CONSTRAINT FK_user_profiles_user FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE,
        INDEX IX_user_profiles_user (user_id)
    );
    PRINT '  ✓ user_profiles table created';
END;
GO

-- Table: entity_types (lookup table)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'entity_types')
BEGIN
    CREATE TABLE entity_types (
        type_id INT IDENTITY(1,1) PRIMARY KEY,
        type_name NVARCHAR(50) NOT NULL UNIQUE, -- 'workspace', 'folder', 'note', 'file'
        description NVARCHAR(255),
        is_active BIT DEFAULT 1,
        created_at DATETIME2 DEFAULT GETUTCDATE()
    );
    PRINT '  ✓ entity_types table created';

    -- Insert default entity types
    INSERT INTO entity_types (type_name, description) VALUES
        ('workspace', 'Workspace/Project'),
        ('folder', 'Folder/Tag container'),
        ('note', 'Note/Document'),
        ('file', 'File attachment');
    PRINT '  ✓ Default entity types inserted';
END;
GO

-- Table: folders (renamed from old tags)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'folders')
BEGIN
    CREATE TABLE folders (
        folder_id INT IDENTITY(1,1) PRIMARY KEY,
        user_id INT NOT NULL,
        name NVARCHAR(255) NOT NULL,
        slug NVARCHAR(255),
        color NVARCHAR(7) DEFAULT '#3B82F6',
        icon NVARCHAR(50),
        description NVARCHAR(1000),
        metadata NVARCHAR(MAX), -- JSON
        usage_count INT DEFAULT 0,
        created_at DATETIME2 DEFAULT GETUTCDATE(),
        updated_at DATETIME2,
        deleted_at DATETIME2,

        CONSTRAINT FK_folders_user FOREIGN KEY (user_id) REFERENCES users(user_id),
        CONSTRAINT UQ_folders_user_slug UNIQUE (user_id, slug),
        INDEX IX_folders_user (user_id) WHERE deleted_at IS NULL,
        INDEX IX_folders_name (name),
        INDEX IX_folders_usage (usage_count)
    );
    PRINT '  ✓ folders table created';
END;
GO

-- Table: tags_new (hashtags only)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'tags_new')
BEGIN
    CREATE TABLE tags_new (
        tag_id INT IDENTITY(1,1) PRIMARY KEY,
        user_id INT NOT NULL,
        name NVARCHAR(255) NOT NULL,
        slug NVARCHAR(255),
        color NVARCHAR(7) DEFAULT '#6B7280',
        usage_count INT DEFAULT 0,
        created_at DATETIME2 DEFAULT GETUTCDATE(),
        updated_at DATETIME2,
        deleted_at DATETIME2,

        CONSTRAINT FK_tags_new_user FOREIGN KEY (user_id) REFERENCES users(user_id),
        CONSTRAINT UQ_tags_new_user_slug UNIQUE (user_id, slug),
        INDEX IX_tags_new_user (user_id, deleted_at) WHERE deleted_at IS NULL,
        INDEX IX_tags_new_usage (user_id, usage_count DESC) WHERE deleted_at IS NULL
    );
    PRINT '  ✓ tags_new table created';
END;
GO

-- Table: entity_tags (polymorphic tagging)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'entity_tags')
BEGIN
    CREATE TABLE entity_tags (
        entity_tag_id BIGINT IDENTITY(1,1) PRIMARY KEY,
        tag_id INT NOT NULL,
        entity_type NVARCHAR(50) NOT NULL, -- 'workspace', 'folder', 'note', 'file'
        entity_id INT NOT NULL,
        tagged_by INT NOT NULL,
        created_at DATETIME2 DEFAULT GETUTCDATE(),
        updated_at DATETIME2,
        deleted_at DATETIME2,

        CONSTRAINT FK_entity_tags_tag FOREIGN KEY (tag_id) REFERENCES tags_new(tag_id) ON DELETE CASCADE,
        CONSTRAINT FK_entity_tags_entity_type FOREIGN KEY (entity_type) REFERENCES entity_types(type_name),
        CONSTRAINT FK_entity_tags_user FOREIGN KEY (tagged_by) REFERENCES users(user_id),
        CONSTRAINT UQ_entity_tags_tag_entity UNIQUE (tag_id, entity_type, entity_id),
        INDEX IX_entity_tags_entity (entity_type, entity_id),
        INDEX IX_entity_tags_tag (tag_id),
        INDEX IX_entity_tags_user (tagged_by)
    );
    PRINT '  ✓ entity_tags table created';
END;
GO

-- Table: standard_registries
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'standard_registries')
BEGIN
    CREATE TABLE standard_registries (
        registry_id INT IDENTITY(1,1) PRIMARY KEY,
        category NVARCHAR(100) NOT NULL,
        key NVARCHAR(255) NOT NULL,
        value NVARCHAR(MAX),
        description NVARCHAR(500),
        is_active BIT DEFAULT 1,
        created_at DATETIME2 DEFAULT GETUTCDATE(),
        updated_at DATETIME2,

        CONSTRAINT UQ_standard_registries_category_key UNIQUE (category, key),
        INDEX IX_standard_registries_category (category, is_active)
    );
    PRINT '  ✓ standard_registries table created';
END;
GO

-- =============================================
-- PHASE 2: WORKSPACE TABLES
-- =============================================
PRINT '';
PRINT 'PHASE 2: Creating workspace tables...';

-- Table: workspace_relationship_types
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'workspace_relationship_types')
BEGIN
    CREATE TABLE workspace_relationship_types (
        type_id INT IDENTITY(1,1) PRIMARY KEY,
        type_name NVARCHAR(50) NOT NULL UNIQUE, -- 'parent', 'reference', 'fork'
        description NVARCHAR(255),
        is_active BIT DEFAULT 1,
        created_at DATETIME2 DEFAULT GETUTCDATE()
    );
    PRINT '  ✓ workspace_relationship_types table created';

    -- Insert default relationship types
    INSERT INTO workspace_relationship_types (type_name, description) VALUES
        ('parent', 'Parent-child relationship'),
        ('reference', 'Reference/link to another workspace'),
        ('fork', 'Forked/copied from another workspace');
    PRINT '  ✓ Default relationship types inserted';
END;
GO

-- Table: workspaces
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'workspaces')
BEGIN
    CREATE TABLE workspaces (
        workspace_id INT IDENTITY(1,1) PRIMARY KEY,
        user_id INT NOT NULL,
        name NVARCHAR(255) NOT NULL,
        description NVARCHAR(1000),
        color NVARCHAR(7) DEFAULT '#3B82F6',
        icon NVARCHAR(50),
        type NVARCHAR(50) DEFAULT 'personal', -- 'personal', 'shared', 'template'
        max_depth INT DEFAULT 5,
        is_default BIT DEFAULT 0,
        is_public BIT DEFAULT 0,
        is_template BIT DEFAULT 0,
        is_archived BIT DEFAULT 0,
        settings NVARCHAR(MAX), -- JSON
        created_at DATETIME2 DEFAULT GETUTCDATE(),
        updated_at DATETIME2,
        deleted_at DATETIME2,

        CONSTRAINT FK_workspaces_user FOREIGN KEY (user_id) REFERENCES users(user_id),
        INDEX IX_workspaces_user (user_id) WHERE deleted_at IS NULL,
        INDEX IX_workspaces_type (type, is_archived),
        INDEX IX_workspaces_public (is_public, is_template) WHERE deleted_at IS NULL
    );
    PRINT '  ✓ workspaces table created';
END;
GO

-- Table: workspace_members
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'workspace_members')
BEGIN
    CREATE TABLE workspace_members (
        member_id INT IDENTITY(1,1) PRIMARY KEY,
        workspace_id INT NOT NULL,
        user_id INT NOT NULL,
        role NVARCHAR(50) DEFAULT 'viewer', -- 'owner', 'editor', 'viewer'
        permissions NVARCHAR(MAX), -- JSON
        joined_at DATETIME2 DEFAULT GETUTCDATE(),
        left_at DATETIME2,

        CONSTRAINT FK_workspace_members_workspace FOREIGN KEY (workspace_id) REFERENCES workspaces(workspace_id) ON DELETE CASCADE,
        CONSTRAINT FK_workspace_members_user FOREIGN KEY (user_id) REFERENCES users(user_id),
        CONSTRAINT UQ_workspace_members_workspace_user UNIQUE (workspace_id, user_id),
        INDEX IX_workspace_members_user (user_id)
    );
    PRINT '  ✓ workspace_members table created';
END;
GO

-- Table: workspace_items (polymorphic: folders, notes, files)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'workspace_items')
BEGIN
    CREATE TABLE workspace_items (
        item_id INT IDENTITY(1,1) PRIMARY KEY,
        workspace_id INT NOT NULL,
        parent_tag_id INT, -- FK to folders.folder_id (if item is nested under a folder)
        child_type NVARCHAR(50) NOT NULL, -- 'folder', 'note', 'file'
        child_id INT NOT NULL, -- FK to folders/notes/files
        sort_order INT DEFAULT 0,
        created_at DATETIME2 DEFAULT GETUTCDATE(),
        updated_at DATETIME2,
        deleted_at DATETIME2,

        CONSTRAINT FK_workspace_items_workspace FOREIGN KEY (workspace_id) REFERENCES workspaces(workspace_id) ON DELETE CASCADE,
        CONSTRAINT FK_workspace_items_parent FOREIGN KEY (parent_tag_id) REFERENCES folders(folder_id),
        CONSTRAINT FK_workspace_items_child_type FOREIGN KEY (child_type) REFERENCES entity_types(type_name),
        CONSTRAINT UQ_workspace_items_child UNIQUE (workspace_id, child_type, child_id),
        INDEX IX_workspace_items_workspace (workspace_id, deleted_at) WHERE deleted_at IS NULL,
        INDEX IX_workspace_items_parent (parent_tag_id, sort_order) WHERE deleted_at IS NULL,
        INDEX IX_workspace_items_child (child_type, child_id)
    );
    PRINT '  ✓ workspace_items table created';
END;
GO

-- =============================================
-- PHASE 3: ENTITY TABLES (Notes, Files)
-- =============================================
PRINT '';
PRINT 'PHASE 3: Creating entity tables...';

-- Table: notes
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'notes')
BEGIN
    CREATE TABLE notes (
        note_id INT IDENTITY(1,1) PRIMARY KEY,
        user_id INT NOT NULL,
        name NVARCHAR(255) NOT NULL,
        description NVARCHAR(MAX),
        content NVARCHAR(MAX),
        type NVARCHAR(50) DEFAULT 'markdown', -- 'markdown', 'rich_text', 'code'
        is_archived BIT DEFAULT 0,
        is_pinned BIT DEFAULT 0,
        metadata NVARCHAR(MAX), -- JSON
        created_at DATETIME2 DEFAULT GETUTCDATE(),
        updated_at DATETIME2,
        deleted_at DATETIME2,

        CONSTRAINT FK_notes_user FOREIGN KEY (user_id) REFERENCES users(user_id),
        INDEX IX_notes_user (user_id) WHERE deleted_at IS NULL,
        INDEX IX_notes_type (type, is_archived),
        INDEX IX_notes_created (created_at DESC)
    );
    PRINT '  ✓ notes table created';
END;
GO

-- Table: note_members (sharing)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'note_members')
BEGIN
    CREATE TABLE note_members (
        member_id INT IDENTITY(1,1) PRIMARY KEY,
        note_id INT NOT NULL,
        user_id INT NOT NULL,
        role NVARCHAR(50) DEFAULT 'viewer', -- 'owner', 'editor', 'viewer'
        permissions NVARCHAR(MAX), -- JSON
        joined_at DATETIME2 DEFAULT GETUTCDATE(),
        left_at DATETIME2,

        CONSTRAINT FK_note_members_note FOREIGN KEY (note_id) REFERENCES notes(note_id) ON DELETE CASCADE,
        CONSTRAINT FK_note_members_user FOREIGN KEY (user_id) REFERENCES users(user_id),
        CONSTRAINT UQ_note_members_note_user UNIQUE (note_id, user_id),
        INDEX IX_note_members_user (user_id)
    );
    PRINT '  ✓ note_members table created';
END;
GO

-- Table: note_versions (version history)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'note_versions')
BEGIN
    CREATE TABLE note_versions (
        version_id INT IDENTITY(1,1) PRIMARY KEY,
        note_id INT NOT NULL,
        version_number INT NOT NULL,
        content NVARCHAR(MAX),
        changed_by INT NOT NULL,
        change_summary NVARCHAR(500),
        created_at DATETIME2 DEFAULT GETUTCDATE(),

        CONSTRAINT FK_note_versions_note FOREIGN KEY (note_id) REFERENCES notes(note_id) ON DELETE CASCADE,
        CONSTRAINT FK_note_versions_user FOREIGN KEY (changed_by) REFERENCES users(user_id),
        CONSTRAINT UQ_note_versions_note_version UNIQUE (note_id, version_number),
        INDEX IX_note_versions_created (note_id, created_at DESC)
    );
    PRINT '  ✓ note_versions table created';
END;
GO

-- Table: files
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'files')
BEGIN
    CREATE TABLE files (
        file_id INT IDENTITY(1,1) PRIMARY KEY,
        user_id INT NOT NULL,
        name NVARCHAR(255) NOT NULL,
        original_name NVARCHAR(255),
        file_path NVARCHAR(500),
        file_size BIGINT,
        mime_type NVARCHAR(100),
        extension NVARCHAR(10),
        is_archived BIT DEFAULT 0,
        metadata NVARCHAR(MAX), -- JSON
        created_at DATETIME2 DEFAULT GETUTCDATE(),
        updated_at DATETIME2,
        deleted_at DATETIME2,

        CONSTRAINT FK_files_user FOREIGN KEY (user_id) REFERENCES users(user_id),
        INDEX IX_files_user (user_id) WHERE deleted_at IS NULL,
        INDEX IX_files_type (mime_type, is_archived),
        INDEX IX_files_created (created_at DESC)
    );
    PRINT '  ✓ files table created';
END;
GO

-- =============================================
-- PHASE 4: TRIGGERS (Auto-increment usage_count)
-- =============================================
PRINT '';
PRINT 'PHASE 4: Creating triggers...';

-- Trigger: Update tag usage_count
IF NOT EXISTS (SELECT 1 FROM sys.triggers WHERE name = 'trg_entity_tags_update_usage')
BEGIN
    EXEC('
    CREATE TRIGGER trg_entity_tags_update_usage
    ON entity_tags
    AFTER INSERT, DELETE
    AS
    BEGIN
        SET NOCOUNT ON;

        -- Increment usage_count for newly inserted tags
        UPDATE t
        SET usage_count = usage_count + 1,
            updated_at = GETUTCDATE()
        FROM tags_new t
        INNER JOIN inserted i ON t.tag_id = i.tag_id
        WHERE i.deleted_at IS NULL;

        -- Decrement usage_count for deleted tags
        UPDATE t
        SET usage_count = CASE WHEN usage_count > 0 THEN usage_count - 1 ELSE 0 END,
            updated_at = GETUTCDATE()
        FROM tags_new t
        INNER JOIN deleted d ON t.tag_id = d.tag_id;
    END;
    ');
    PRINT '  ✓ Trigger trg_entity_tags_update_usage created';
END;
GO

-- Trigger: Update folder usage_count
IF NOT EXISTS (SELECT 1 FROM sys.triggers WHERE name = 'trg_workspace_items_update_folder_usage')
BEGIN
    EXEC('
    CREATE TRIGGER trg_workspace_items_update_folder_usage
    ON workspace_items
    AFTER INSERT, DELETE
    AS
    BEGIN
        SET NOCOUNT ON;

        -- Increment usage_count for newly added folders
        UPDATE f
        SET usage_count = usage_count + 1,
            updated_at = GETUTCDATE()
        FROM folders f
        INNER JOIN inserted i ON f.folder_id = i.child_id
        WHERE i.child_type = ''folder'' AND i.deleted_at IS NULL;

        -- Decrement usage_count for removed folders
        UPDATE f
        SET usage_count = CASE WHEN usage_count > 0 THEN usage_count - 1 ELSE 0 END,
            updated_at = GETUTCDATE()
        FROM folders f
        INNER JOIN deleted d ON f.folder_id = d.child_id
        WHERE d.child_type = ''folder'';
    END;
    ');
    PRINT '  ✓ Trigger trg_workspace_items_update_folder_usage created';
END;
GO

-- =============================================
-- SUMMARY
-- =============================================
PRINT '';
PRINT '========================================';
PRINT 'DATABASE SCHEMA CREATED SUCCESSFULLY';
PRINT '========================================';
PRINT '';
PRINT 'Tables created:';
PRINT '  Core: users, user_profiles, folders, tags_new, entity_tags, entity_types, standard_registries';
PRINT '  Workspace: workspaces, workspace_members, workspace_items, workspace_relationship_types';
PRINT '  Entities: notes, note_members, note_versions, files';
PRINT '';
PRINT 'Triggers created:';
PRINT '  - trg_entity_tags_update_usage';
PRINT '  - trg_workspace_items_update_folder_usage';
PRINT '';
PRINT 'Next steps:';
PRINT '  1. Run INSERT_SAMPLE_DATA.sql to populate with test data';
PRINT '  2. Update EF Core connection string';
PRINT '  3. Restart backend API';
PRINT '';
PRINT 'Schema creation completed at: ' + CONVERT(VARCHAR, GETUTCDATE(), 120);
GO
