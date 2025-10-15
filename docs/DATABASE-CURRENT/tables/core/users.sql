-- =============================================
-- TABLE: users
-- Description: User accounts and authentication
-- Status: ✅ DEPLOYED (MVP)
-- =============================================

CREATE TABLE users (
    -- Primary Key
    user_id INT IDENTITY(1,1) PRIMARY KEY,

    -- Authentication
    email NVARCHAR(255) NOT NULL UNIQUE,
    username NVARCHAR(100) NOT NULL UNIQUE,
    password_hash NVARCHAR(255) NOT NULL,

    -- Profile
    display_name NVARCHAR(255) NULL,
    avatar_url NVARCHAR(500) NULL,
    bio NVARCHAR(1000) NULL,

    -- Settings
    preferences NVARCHAR(MAX) NULL, -- JSON: user preferences

    -- Status flags
    is_active BIT NOT NULL DEFAULT 1,
    email_verified BIT NOT NULL DEFAULT 0,

    -- Timestamps
    created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    updated_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    last_login_at DATETIME2 NULL,
    deleted_at DATETIME2 NULL -- Soft delete
);

-- =============================================
-- INDEXES
-- =============================================

-- Index for email lookups (authentication)
CREATE NONCLUSTERED INDEX IX_users_email
    ON users(email)
    WHERE deleted_at IS NULL;

-- Index for username lookups
CREATE NONCLUSTERED INDEX IX_users_username
    ON users(username)
    WHERE deleted_at IS NULL;

-- Index for active users
CREATE NONCLUSTERED INDEX IX_users_active
    ON users(is_active, deleted_at)
    WHERE deleted_at IS NULL;

-- =============================================
-- NOTES
-- =============================================
-- - No foreign keys (base table)
-- - Soft delete supported via deleted_at
-- - UNIQUE constraints on email and username
-- - All indexes filtered for deleted_at IS NULL
