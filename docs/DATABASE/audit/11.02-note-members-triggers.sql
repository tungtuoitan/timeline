-- ============================================
-- FILE: audit/11.02-note-members-triggers.sql
-- PURPOSE: Audit triggers for note sharing changes
-- DEPENDENCIES: 07-tables-entities.sql, constraints/06.13-audit-log.sql
-- ============================================

USE SuperApp;
GO

SET NOCOUNT ON;

PRINT '🔒 Creating note sharing audit triggers...';
GO

-- Trigger: Audit note member role changes
CREATE OR ALTER TRIGGER tr_audit_note_member_role
ON note_members
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
            'note_members',
            i.id,
            'PERMISSION_CHANGE',
            i.user_id,
            JSON_OBJECT(
                'note_id': d.note_id,
                'user_id': d.user_id,
                'old_role': d.role,
                'invitation_status': d.invitation_status
            ),
            JSON_OBJECT(
                'note_id': i.note_id,
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

PRINT '   ✅ tr_audit_note_member_role created';

-- Trigger: Audit note member additions (new sharing)
CREATE OR ALTER TRIGGER tr_audit_note_member_add
ON note_members
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO audit_log (
        table_name, 
        record_id, 
        operation, 
        user_id, 
        new_values,
        application_name
    )
    SELECT 
        'note_members',
        i.id,
        'SHARE_NOTE',
        i.user_id,
        JSON_OBJECT(
            'note_id': i.note_id,
            'user_id': i.user_id,
            'role': i.role,
            'invitation_status': i.invitation_status,
            'invited_at': CONVERT(VARCHAR(30), i.invited_at, 127)
        ),
        APP_NAME()
    FROM inserted i;
END;
GO

PRINT '   ✅ tr_audit_note_member_add created';

-- Trigger: Audit note member removal (unsharing)
CREATE OR ALTER TRIGGER tr_audit_note_member_remove
ON note_members
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Only log if invitation_status changed to 'removed' or deleted_at set
    IF UPDATE(invitation_status) OR UPDATE(deleted_at)
    BEGIN
        INSERT INTO audit_log (
            table_name, 
            record_id, 
            operation, 
            user_id, 
            old_values,
            application_name
        )
        SELECT 
            'note_members',
            i.id,
            'UNSHARE_NOTE',
            i.user_id,
            JSON_OBJECT(
                'note_id': d.note_id,
                'user_id': d.user_id,
                'role': d.role,
                'invitation_status': d.invitation_status,
                'reason': CASE 
                    WHEN i.deleted_at IS NOT NULL AND d.deleted_at IS NULL THEN 'soft_deleted'
                    WHEN i.invitation_status = 'removed' AND d.invitation_status != 'removed' THEN 'access_removed'
                    ELSE 'status_change'
                END
            ),
            APP_NAME()
        FROM inserted i
        INNER JOIN deleted d ON i.id = d.id
        WHERE (i.invitation_status = 'removed' AND d.invitation_status != 'removed')
           OR (i.deleted_at IS NOT NULL AND d.deleted_at IS NULL);
    END;
END;
GO

PRINT '   ✅ tr_audit_note_member_remove created';
PRINT '📊 Next step: Run audit/11.03-workspace-items-triggers.sql';
GO