-- ============================================
-- FILE: tables/core/users.sql
-- PURPOSE: Users table - User accounts
-- SCALE: 1,000+ users
-- DEPENDENCIES: None
-- ============================================

PRINT '📦 Creating users table...';
GO

-- Drop table if exists (for clean reinstall)
-- WARNING: This will delete all data!
/*
IF OBJECT_ID('users', 'U') IS NOT NULL DROP TABLE users;
*/

-- ============================================
-- TABLE: users
-- PURPOSE: User accounts
-- SCALE: 1,000+ users
-- ============================================

CREATE TABLE users (
    id INT IDENTITY(1,1) PRIMARY KEY,
    
    -- Authentication
    email NVARCHAR(255) NOT NULL,
    username NVARCHAR(100) NOT NULL,
    password_hash NVARCHAR(255) NOT NULL, -- bcrypt hash
    
    -- Profile
    display_name NVARCHAR(200),
    avatar_url NVARCHAR(500),
    bio NVARCHAR(1000),
    
    -- Settings
    preferences NVARCHAR(MAX), -- JSON: { theme, language, notifications, ... }
    
    -- Account status
    is_active BIT DEFAULT 1,
    is_verified BIT DEFAULT 0,
    email_verified_at DATETIME2 NULL,
    
    -- Timestamps
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    updated_at DATETIME2 DEFAULT GETUTCDATE(),
    last_login_at DATETIME2 NULL,
    deleted_at DATETIME2 NULL, -- Soft delete
    
    -- Constraints
    CONSTRAINT uq_users_email UNIQUE (email, deleted_at),
    CONSTRAINT uq_users_username UNIQUE (username, deleted_at),
    CONSTRAINT ck_users_email_format CHECK (email LIKE '%@%.%')
);

-- Indexes for users table
CREATE INDEX ix_users_email ON users(email, deleted_at) 
    WHERE deleted_at IS NULL;

CREATE INDEX ix_users_username ON users(username, deleted_at) 
    WHERE deleted_at IS NULL;

CREATE INDEX ix_users_active ON users(is_active, deleted_at) 
    WHERE is_active = 1 AND deleted_at IS NULL;

-- Comments
EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description',
    @value = N'User accounts table. Supports 1000+ users with soft delete.',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE', @level1name = N'users';

EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description',
    @value = N'JSON format: {"theme": "dark", "language": "en", "notifications": {"email": true}}',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE', @level1name = N'users',
    @level2type = N'COLUMN', @level2name = N'preferences';

GO

-- ============================================
-- TRIGGER: Update users.updated_at on change
-- ============================================

CREATE OR ALTER TRIGGER tr_users_updated_at
ON users
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE users
    SET updated_at = GETUTCDATE()
    WHERE id IN (SELECT id FROM inserted);
END;
GO

-- ============================================
-- VIEW: Active users only
-- ============================================

CREATE OR ALTER VIEW vw_active_users AS
SELECT 
    id,
    email,
    username,
    display_name,
    avatar_url,
    is_verified,
    created_at,
    last_login_at
FROM users
WHERE deleted_at IS NULL
AND is_active = 1;
GO

PRINT '   ✅ users table created successfully';
GO
