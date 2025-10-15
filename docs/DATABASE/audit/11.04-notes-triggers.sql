-- ============================================
-- FILE: audit/11.04-notes-triggers.sql
-- PURPOSE: Audit trigger for note metadata changes
-- DEPENDENCIES: 07-tables-entities.sql, constraints/06.13-audit-log.sql
-- ============================================

USE SuperApp;
GO

SET NOCOUNT ON;

PRINT '🔒 Creating note metadata audit trigger...';
GO

-- Trigger: Audit significant note changes (name/description only, not full content)
CREATE OR ALTER TRIGGER tr_audit_note_metadata
ON notes
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Only log if name or description changed (not full content - that's in versions)
    IF UPDATE(name) OR UPDATE(description)
    BEGIN
        INSERT INTO audit_log (
            table_name, 
            record_id, 
            operation, 
            user_id, 
            old_values, 
            new_values,
            application_name
        )
        SELECT 
            'notes',
            i.id,
            'METADATA_CHANGE',
            i.user_id,
            JSON_OBJECT(
                'old_name': d.name,
                'old_description': d.description,
                'updated_at': CONVERT(VARCHAR(30), d.updated_at, 127)
            ),
            JSON_OBJECT(
                'new_name': i.name,
                'new_description': i.description,
                'updated_at': CONVERT(VARCHAR(30), i.updated_at, 127)
            ),
            APP_NAME()
        FROM inserted i
        INNER JOIN deleted d ON i.id = d.id
        WHERE i.name != d.name 
           OR ISNULL(i.description, '') != ISNULL(d.description, '');
    END;
END;
GO

PRINT '   ✅ tr_audit_note_metadata created';
PRINT '📊 Next step: Run audit/11.05-workspaces-triggers.sql';
GO