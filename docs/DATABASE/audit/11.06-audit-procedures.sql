-- ============================================
-- FILE: audit/11.06-audit-procedures.sql
-- PURPOSE: Audit query helper procedures
-- DEPENDENCIES: constraints/06.13-audit-log.sql
-- ============================================

USE SuperApp;
GO

SET NOCOUNT ON;

PRINT '🔒 Creating audit query helper procedures...';
GO

-- Procedure: Get audit history for a specific record
CREATE OR ALTER PROCEDURE usp_get_audit_history
    @table_name NVARCHAR(100),
    @record_id BIGINT,
    @limit INT = 50
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT TOP (@limit)
        id,
        table_name,
        record_id,
        operation,
        user_id,
        changed_at,
        old_values,
        new_values,
        application_name,
        client_ip
    FROM audit_log
    WHERE table_name = @table_name
      AND record_id = @record_id
    ORDER BY changed_at DESC, id DESC;
END;
GO

PRINT '   ✅ usp_get_audit_history created';

-- Procedure: Get recent security events
CREATE OR ALTER PROCEDURE usp_get_security_events
    @hours INT = 24,
    @limit INT = 100
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Security-relevant operations
    DECLARE @security_ops TABLE (operation NVARCHAR(50));
    INSERT INTO @security_ops VALUES 
        ('ROLE_CHANGE'),
        ('PERMISSION_CHANGE'),
        ('SHARE_NOTE'),
        ('UNSHARE_NOTE'),
        ('OWNERSHIP_TRANSFER');
    
    SELECT TOP (@limit)
        al.id,
        al.table_name,
        al.record_id,
        al.operation,
        al.user_id,
        al.changed_at,
        al.old_values,
        al.new_values,
        al.application_name,
        CASE al.operation
            WHEN 'ROLE_CHANGE' THEN 'Workspace role changed'
            WHEN 'PERMISSION_CHANGE' THEN 'Note permission changed'
            WHEN 'SHARE_NOTE' THEN 'Note shared with user'
            WHEN 'UNSHARE_NOTE' THEN 'Note access removed'
            WHEN 'OWNERSHIP_TRANSFER' THEN 'Workspace ownership transferred'
            ELSE 'Other security event'
        END AS event_description
    FROM audit_log al
    WHERE al.operation IN (SELECT operation FROM @security_ops)
      AND al.changed_at >= DATEADD(HOUR, -@hours, GETUTCDATE())
    ORDER BY al.changed_at DESC, al.id DESC;
END;
GO

PRINT '   ✅ usp_get_security_events created';

-- Procedure: Get user activity summary
CREATE OR ALTER PROCEDURE usp_get_user_activity
    @user_id INT,
    @days INT = 30
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        operation,
        table_name,
        COUNT(*) AS action_count,
        MIN(changed_at) AS first_action,
        MAX(changed_at) AS last_action
    FROM audit_log
    WHERE user_id = @user_id
      AND changed_at >= DATEADD(DAY, -@days, GETUTCDATE())
    GROUP BY operation, table_name
    ORDER BY action_count DESC;
END;
GO

PRINT '   ✅ usp_get_user_activity created';

-- Procedure: Get workspace audit trail
CREATE OR ALTER PROCEDURE usp_get_workspace_audit_trail
    @workspace_id INT,
    @days INT = 30,
    @limit INT = 100
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Get all audit events related to a workspace
    SELECT TOP (@limit)
        al.id,
        al.table_name,
        al.operation,
        al.user_id,
        al.changed_at,
        al.old_values,
        al.new_values,
        al.application_name
    FROM audit_log al
    WHERE al.changed_at >= DATEADD(DAY, -@days, GETUTCDATE())
      AND (
          -- Direct workspace changes
          (al.table_name = 'workspaces' AND al.record_id = @workspace_id)
          
          -- Workspace member changes
          OR (al.table_name = 'workspace_members' AND 
              JSON_VALUE(al.new_values, '$.workspace_id') = CAST(@workspace_id AS NVARCHAR))
          
          -- Workspace item changes
          OR (al.table_name = 'workspace_items' AND 
              JSON_VALUE(al.new_values, '$.workspace_id') = CAST(@workspace_id AS NVARCHAR))
      )
    ORDER BY al.changed_at DESC, al.id DESC;
END;
GO

PRINT '   ✅ usp_get_workspace_audit_trail created';

-- Procedure: Cleanup old audit logs (for maintenance)
CREATE OR ALTER PROCEDURE usp_cleanup_old_audit_logs
    @days_to_keep INT = 90
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @cutoff_date DATETIME2 = DATEADD(DAY, -@days_to_keep, GETUTCDATE());
    DECLARE @deleted_count INT;
    
    -- Archive old logs before deletion (optional step)
    -- You could insert into audit_log_archive table here
    
    -- Delete old audit logs
    DELETE FROM audit_log
    WHERE changed_at < @cutoff_date;
    
    SET @deleted_count = @@ROWCOUNT;
    
    PRINT 'Deleted ' + CAST(@deleted_count AS VARCHAR) + ' audit log entries older than ' + 
          CAST(@days_to_keep AS VARCHAR) + ' days';
    
    RETURN @deleted_count;
END;
GO

PRINT '   ✅ usp_cleanup_old_audit_logs created';
PRINT '📊 Next step: Run audit/11.07-performance-notes.sql';
GO