
-- ============================================
-- FILE: constraints/06.01-users-constraints.sql
-- PURPOSE: Constraints for users table
-- DEPENDENCIES: 02-tables-core.sql
-- ============================================

PRINT '🔒 Creating user table constraints...';
GO

-- 📝 NOTE ON NAMING CONSISTENCY:
-- Some constraint names use different patterns for historical reasons:
-- - ck_users_email_enhanced (should be ck_users_email_format)
-- Keeping existing names for backward compatibility. New constraints use:
-- Pattern: ck_{table}_{column}_{type} where type = format|valid|range|positive

-- Constraint: Email format validation (enhanced)
ALTER TABLE users
ADD CONSTRAINT ck_users_email_enhanced CHECK (
    email LIKE '%_@__%.__%'
    AND email NOT LIKE '%@%@%'
    AND email NOT LIKE '%..%'
    AND LEN(email) >= 6
);

PRINT '   ✅ ck_users_email_enhanced added';

-- Constraint: Username format (alphanumeric, dash, underscore only)
ALTER TABLE users
ADD CONSTRAINT ck_users_username_format CHECK (
    username NOT LIKE '%[^a-zA-Z0-9_-]%'
    AND LEN(username) BETWEEN 3 AND 50
);

PRINT '   ✅ ck_users_username_format added';

-- Constraint: Display name length
ALTER TABLE users
ADD CONSTRAINT ck_users_display_name_length CHECK (
    display_name IS NULL 
    OR LEN(display_name) BETWEEN 1 AND 200
);

PRINT '   ✅ ck_users_display_name_length added';

-- Constraint: Password hash format (bcrypt starts with $2)
ALTER TABLE users
ADD CONSTRAINT ck_users_password_hash_format CHECK (
    password_hash LIKE '$2%'
    AND LEN(password_hash) >= 50
);

PRINT '   ✅ ck_users_password_hash_format added';

GO

PRINT '✅ User table constraints created successfully!';
PRINT '📊 Next step: Run constraints/06.02-tags-constraints.sql';
GO