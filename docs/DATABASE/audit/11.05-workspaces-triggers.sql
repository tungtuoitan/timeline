-- ============================================
-- FILE: audit/11.05-workspaces-triggers.sql
-- PURPOSE: Audit trigger for workspace ownership transfers
-- DEPENDENCIES: 03-tables-workspace.sql, constraints/06.13-audit-log.sql
-- ============================================

USE SuperApp;
GO

SET NOCOUNT ON;

PRINT '🔒 Creating workspace ownership transfer audit trigger...';
GO

-- Trigger: Audit workspace owner changes (critical security event)
CREATE OR ALTER TRIGGER tr_audit_workspace_owner_change
ON workspaces
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Only log if user_id (owner) changed
    IF UPDATE(user_id)
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
            'workspaces',
            i.id,
            'OWNERSHIP_TRANSFER',
            i.user_id,
            JSON_OBJECT(
                'workspace_id': d.id,
                'workspace_name': d.name,
                'old_owner_id': d.user_id,
                'transferred_at': CONVERT(VARCHAR(30), GETUTCDATE(), 127)
            ),
            JSON_OBJECT(
                'workspace_id': i.id,
                'workspace_name': i.name,
                'new_owner_id': i.user_id,
                'transferred_at': CONVERT(VARCHAR(30), GETUTCDATE(), 127)
            ),
            APP_NAME()
        FROM inserted i
        INNER JOIN deleted d ON i.id = d.id
        WHERE i.user_id != d.user_id;
    END;
END;
GO

PRINT '   ✅ tr_audit_workspace_owner_change created';
PRINT '📊 Next step: Run audit/11.06-audit-procedures.sql';
GO