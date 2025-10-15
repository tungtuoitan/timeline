-- ============================================
-- FILE: audit/11.01-workspace-members-triggers.sql
-- PURPOSE: Audit trigger for workspace member role changes
-- DEPENDENCIES: 03-tables-workspace.sql, constraints/06.13-audit-log.sql
-- ============================================

USE SuperApp;
GO

SET NOCOUNT ON;

PRINT '🔒 Creating workspace member role change audit trigger...';
GO

-- Trigger: Audit workspace member role changes (critical for security)
CREATE OR ALTER TRIGGER tr_audit_workspace_member_role
ON workspace_members
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Only log if role actually changed
    IF UPDATE(role)
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
            'workspace_members',
            i.id,
            'ROLE_CHANGE',
            i.user_id,
            JSON_OBJECT(
                'workspace_id': d.workspace_id,
                'user_id': d.user_id,
                'old_role': d.role,
                'invitation_status': d.invitation_status
            ),
            JSON_OBJECT(
                'workspace_id': i.workspace_id,
                'user_id': i.user_id,
                'new_role': i.role,
                'invitation_status': i.invitation_status
            ),
            APP_NAME()
        FROM inserted i
        INNER JOIN deleted d ON i.id = d.id
        WHERE i.role != d.role;
    END;
END;
GO

PRINT '   ✅ tr_audit_workspace_member_role created';
PRINT '📊 Next step: Run audit/11.02-note-members-triggers.sql';
GO