-- =============================================
-- REBUILD SIMPLIFIED SCHEMA
-- =============================================
-- Author: SuperApp Team
-- Date: 2025-11-30
-- Description: Xóa hết bảng cũ, tạo lại schema đơn giản
-- =============================================

USE [SuperApp-dev];
GO

PRINT '=============================================';
PRINT 'REBUILD SIMPLIFIED SCHEMA';
PRINT 'Started at: ' + CONVERT(NVARCHAR(30), GETDATE(), 120);
PRINT '=============================================';
PRINT '';

-- =============================================
-- PHASE 0: DROP ALL OLD TABLES
-- =============================================
PRINT 'PHASE 0: Dropping all old tables...';
PRINT '';

-- Drop in reverse dependency order
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'entity_tags')
    DROP TABLE entity_tags;
    PRINT '  ✓ Dropped: entity_tags';

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'note_versions')
    DROP TABLE note_versions;
    PRINT '  ✓ Dropped: note_versions';

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'note_members')
    DROP TABLE note_members;
    PRINT '  ✓ Dropped: note_members';

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'workspace_items')
    DROP TABLE workspace_items;
    PRINT '  ✓ Dropped: workspace_items';

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'workspace_members')
    DROP TABLE workspace_members;
    PRINT '  ✓ Dropped: workspace_members';

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'workspace_relationship_types')
    DROP TABLE workspace_relationship_types;
    PRINT '  ✓ Dropped: workspace_relationship_types';

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'notes')
    DROP TABLE notes;
    PRINT '  ✓ Dropped: notes';

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'files')
    DROP TABLE files;
    PRINT '  ✓ Dropped: files';

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'folders')
    DROP TABLE folders;
    PRINT '  ✓ Dropped: folders';

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'workspaces')
    DROP TABLE workspaces;
    PRINT '  ✓ Dropped: workspaces';

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'tags_new')
    DROP TABLE tags_new;
    PRINT '  ✓ Dropped: tags_new';

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'note_tags')
    DROP TABLE note_tags;
    PRINT '  ✓ Dropped: note_tags (deprecated)';

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'entities')
    DROP TABLE entities;
    PRINT '  ✓ Dropped: entities';

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'entity_types')
    DROP TABLE entity_types;
    PRINT '  ✓ Dropped: entity_types (old)';

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'user_profiles')
    DROP TABLE user_profiles;
    PRINT '  ✓ Dropped: user_profiles';

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'standard_registries')
    DROP TABLE standard_registries;
    PRINT '  ✓ Dropped: standard_registries';

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'users')
    DROP TABLE users;
    PRINT '  ✓ Dropped: users';

PRINT '';
PRINT '✅ All old tables dropped successfully!';
PRINT '';

-- =============================================
-- PHASE 0.5: CREATE SCHEMAS
-- =============================================
PRINT 'PHASE 0.5: Creating schemas...';
PRINT '';

-- Schema: urm (User Related Management)
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'urm')
BEGIN
    EXEC('CREATE SCHEMA urm');
    PRINT '  ✓ Schema created: urm (User Related Management)';
END;

-- Schema: ws (Workspace)
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'ws')
BEGIN
    EXEC('CREATE SCHEMA ws');
    PRINT '  ✓ Schema created: ws (Workspace)';
END;

-- Schema: dbo (default - already exists)
PRINT '  ✓ Schema: dbo (default)';

PRINT '';
PRINT '✅ Schemas created!';
PRINT '';

-- =============================================
-- PHASE 1: CORE TABLES
-- =============================================
PRINT 'PHASE 1: Creating core tables...';
PRINT '';

-- Table: urm.users
CREATE TABLE urm.users (
    id INT IDENTITY(1,1) PRIMARY KEY,
    email NVARCHAR(255) NOT NULL UNIQUE,
    phone NVARCHAR(20),
    password NVARCHAR(255) NOT NULL,
    auth_type NVARCHAR(50) DEFAULT 'local', -- 'local', 'google', 'facebook'
    is_active BIT DEFAULT 1,
    last_login_at DATETIME2,
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    updated_at DATETIME2,
    deleted_at DATETIME2,
    
    INDEX IX_users_email (email) WHERE deleted_at IS NULL,
    INDEX IX_users_phone (phone) WHERE deleted_at IS NULL
);
PRINT '  ✓ urm.users table created';

-- Table: urm.user_profiles
CREATE TABLE urm.user_profiles (
    id INT IDENTITY(1,1) PRIMARY KEY,
    user_id INT NOT NULL UNIQUE,
    first_name NVARCHAR(100),
    last_name NVARCHAR(100),
    avatar_url NVARCHAR(500),
    bio NVARCHAR(1000),
    date_of_birth DATE,
    gender NVARCHAR(10),
    country NVARCHAR(100),
    city NVARCHAR(100),
    timezone NVARCHAR(50) DEFAULT 'UTC',
    language NVARCHAR(10) DEFAULT 'en',
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    updated_at DATETIME2,
    
    CONSTRAINT FK_user_profiles_user FOREIGN KEY (user_id) REFERENCES urm.users(id) ON DELETE CASCADE
);
PRINT '  ✓ urm.user_profiles table created';

-- Table: dbo.hashtags
CREATE TABLE dbo.hashtags (
    id INT IDENTITY(1,1) PRIMARY KEY,
    user_id INT NOT NULL,
    name NVARCHAR(100) NOT NULL,
    usage_count INT DEFAULT 0,
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    updated_at DATETIME2,
    deleted_at DATETIME2,
    
    CONSTRAINT FK_hashtags_user FOREIGN KEY (user_id) REFERENCES urm.users(id),
    CONSTRAINT UQ_hashtags_user_name UNIQUE (user_id, name),
    INDEX IX_hashtags_user (user_id) WHERE deleted_at IS NULL,
    INDEX IX_hashtags_name (name)
);
PRINT '  ✓ dbo.hashtags table created';

-- Table: dbo.entities (lookup)
CREATE TABLE dbo.entities (
    id TINYINT PRIMARY KEY, -- 1=workspace, 2=folder, 3=note, 4=file
    name NVARCHAR(50) NOT NULL UNIQUE,
    description NVARCHAR(255),
    created_at DATETIME2 DEFAULT GETUTCDATE()
);
PRINT '  ✓ dbo.entities table created';

-- Insert default entities
INSERT INTO dbo.entities (id, name, description) VALUES
    (1, 'workspace', 'Workspace/Project container'),
    (2, 'folder', 'Folder for organizing items'),
    (3, 'note', 'Note/Document'),
    (4, 'file', 'File attachment');
PRINT '  ✓ Default entities inserted';

-- Table: dbo.standard_registries
CREATE TABLE dbo.standard_registries (
    id INT IDENTITY(1,1) PRIMARY KEY,
    type_code NVARCHAR(100) NOT NULL UNIQUE,
    description NVARCHAR(500),
    is_active BIT DEFAULT 1,
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    updated_at DATETIME2
);
PRINT '  ✓ dbo.standard_registries table created';

-- Insert default configs
INSERT INTO dbo.standard_registries (type_code, description) VALUES
    ('max_file_size', 'Max file upload size (10MB in bytes)'),
    ('allowed_file_types', 'Allowed file extensions'),
    ('max_workspace_depth', 'Maximum folder depth in workspace'),
    ('default_theme', 'Default UI theme');
PRINT '  ✓ Default configs inserted';

PRINT '';
PRINT '✅ PHASE 1 completed!';
PRINT '';

-- =============================================
-- PHASE 2: WORKSPACE TABLES
-- =============================================
PRINT 'PHASE 2: Creating workspace tables...';
PRINT '';

-- Table: ws.workspaces
CREATE TABLE ws.workspaces (
    id INT IDENTITY(1,1) PRIMARY KEY,
    user_id INT NOT NULL,
    name NVARCHAR(255) NOT NULL,
    description NVARCHAR(1000),
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    updated_at DATETIME2,
    deleted_at DATETIME2,
    
    CONSTRAINT FK_workspaces_user FOREIGN KEY (user_id) REFERENCES urm.users(id),
    INDEX IX_workspaces_user (user_id) WHERE deleted_at IS NULL
);
PRINT '  ✓ ws.workspaces table created';

PRINT '';
PRINT '✅ PHASE 2 completed!';
PRINT '';

-- =============================================
-- PHASE 3: ENTITY TABLES (SEPARATED)
-- =============================================
PRINT 'PHASE 3: Creating entity tables (folders, notes, files)...';
PRINT '';

-- Table: ws.folders
CREATE TABLE ws.folders (
    id INT IDENTITY(1,1) PRIMARY KEY,
    user_id INT NOT NULL,
    name NVARCHAR(255) NOT NULL,
    description NVARCHAR(MAX),
    color NVARCHAR(7) DEFAULT '#F59E0B',
    icon NVARCHAR(50) DEFAULT '📁',
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    updated_at DATETIME2,
    deleted_at DATETIME2,
    
    CONSTRAINT FK_folders_user FOREIGN KEY (user_id) REFERENCES urm.users(id),
    INDEX IX_folders_user (user_id) WHERE deleted_at IS NULL,
    INDEX IX_folders_name (name),
    INDEX IX_folders_created (created_at DESC)
);
PRINT '  ✓ ws.folders table created';

-- Table: dbo.notes
CREATE TABLE dbo.notes (
    id INT IDENTITY(1,1) PRIMARY KEY,
    user_id INT NOT NULL,
    name NVARCHAR(255) NOT NULL,
    description NVARCHAR(MAX),
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    updated_at DATETIME2,
    deleted_at DATETIME2,
    
    CONSTRAINT FK_notes_user FOREIGN KEY (user_id) REFERENCES urm.users(id),
    INDEX IX_notes_user (user_id) WHERE deleted_at IS NULL,
    INDEX IX_notes_created (created_at DESC)
);
PRINT '  ✓ dbo.notes table created';

-- Table: ws.files
CREATE TABLE ws.files (
    id INT IDENTITY(1,1) PRIMARY KEY,
    user_id INT NOT NULL,
    name NVARCHAR(255) NOT NULL,
    url NVARCHAR(1000),
    file_size BIGINT,
    mime_type NVARCHAR(100),
    extension NVARCHAR(20),
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    updated_at DATETIME2,
    deleted_at DATETIME2,
    
    CONSTRAINT FK_files_user FOREIGN KEY (user_id) REFERENCES urm.users(id),
    INDEX IX_files_user (user_id) WHERE deleted_at IS NULL,
    INDEX IX_files_type (mime_type),
    INDEX IX_files_created (created_at DESC)
);
PRINT '  ✓ ws.files table created';

PRINT '';
PRINT '✅ PHASE 3 completed!';
PRINT '';

-- =============================================
-- PHASE 4: WORKSPACE_ITEMS (POLYMORPHIC JUNCTION)
-- =============================================
PRINT 'PHASE 4: Creating workspace_items table...';
PRINT '';

-- Table: ws.workspace_items (polymorphic junction - link workspace với folder/note/file)
CREATE TABLE ws.workspace_items (
    id INT IDENTITY(1,1) PRIMARY KEY,
    workspace_id INT NOT NULL,
    folder_id INT, -- FK to ws.folders.id (for hierarchy)
    item_type TINYINT NOT NULL, -- 2=folder, 3=note, 4=file (from dbo.entities)
    item_id INT NOT NULL, -- ID của folder/note/file
    is_original BIT DEFAULT 1, -- TRUE = workspace tạo item, FALSE = item được share/copy vào
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    updated_at DATETIME2,
    deleted_at DATETIME2,
    
    CONSTRAINT FK_workspace_items_workspace FOREIGN KEY (workspace_id) REFERENCES ws.workspaces(id) ON DELETE CASCADE,
    CONSTRAINT FK_workspace_items_folder FOREIGN KEY (folder_id) REFERENCES ws.folders(id),
    CONSTRAINT FK_workspace_items_type FOREIGN KEY (item_type) REFERENCES dbo.entities(id),
    CONSTRAINT UQ_workspace_items_unique UNIQUE (workspace_id, item_type, item_id),
    INDEX IX_workspace_items_workspace (workspace_id, deleted_at) WHERE deleted_at IS NULL,
    INDEX IX_workspace_items_folder (folder_id) WHERE deleted_at IS NULL,
    INDEX IX_workspace_items_item (item_type, item_id),
    INDEX IX_workspace_items_original (item_type, item_id, is_original) WHERE is_original = 1
);
PRINT '  ✓ ws.workspace_items table created';

PRINT '';
PRINT '✅ PHASE 4 completed!';
PRINT '';

-- =============================================
-- PHASE 5: ENTITY_HASHTAGS (POLYMORPHIC TAGGING)
-- =============================================
PRINT 'PHASE 5: Creating entity_hashtags table...';
PRINT '';

-- Table: dbo.entity_hashtags (polymorphic tagging - link hashtags với workspace/folder/note/file)
CREATE TABLE dbo.entity_hashtags (
    id INT IDENTITY(1,1) PRIMARY KEY,
    entity_type TINYINT NOT NULL, -- 1=workspace, 2=folder, 3=note, 4=file (from dbo.entities)
    entity_id INT NOT NULL,
    hashtag_id INT NOT NULL,
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    
    CONSTRAINT FK_entity_hashtags_hashtag FOREIGN KEY (hashtag_id) REFERENCES dbo.hashtags(id) ON DELETE CASCADE,
    CONSTRAINT FK_entity_hashtags_type FOREIGN KEY (entity_type) REFERENCES dbo.entities(id),
    CONSTRAINT UQ_entity_hashtags_unique UNIQUE (hashtag_id, entity_type, entity_id),
    INDEX IX_entity_hashtags_entity (entity_type, entity_id)
);
PRINT '  ✓ dbo.entity_hashtags table created';

PRINT '';
PRINT '✅ PHASE 5 completed!';
PRINT '';

-- =============================================
-- SUMMARY
-- =============================================
PRINT '';
PRINT '=============================================';
PRINT 'REBUILD COMPLETED SUCCESSFULLY!';
PRINT '=============================================';
PRINT '';
PRINT 'Tables created:';
PRINT '';
PRINT 'Schema: urm (User Related Management)';
PRINT '  1. urm.users';
PRINT '  2. urm.user_profiles';
PRINT '';
PRINT 'Schema: ws (Workspace)';
PRINT '  3. ws.workspaces';
PRINT '  4. ws.folders';
PRINT '  5. ws.files';
PRINT '  6. ws.workspace_items (polymorphic junction)';
PRINT '';
PRINT 'Schema: dbo (Default)';
PRINT '  7. dbo.hashtags';
PRINT '  8. dbo.entities (TINYINT lookup)';
PRINT '  9. dbo.standard_registries';
PRINT ' 10. dbo.notes';
PRINT ' 11. dbo.entity_hashtags (polymorphic tagging)';
PRINT '';
PRINT 'Total: 11 tables across 3 schemas';
PRINT '';
PRINT 'Completed at: ' + CONVERT(NVARCHAR(30), GETDATE(), 120);
PRINT '=============================================';
GO

-- =============================================
-- VERIFY TABLES
-- =============================================
PRINT '';
PRINT 'Verifying tables...';
SELECT 
    TABLE_NAME,
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = t.TABLE_NAME) AS column_count
FROM INFORMATION_SCHEMA.TABLES t
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME;
GO
