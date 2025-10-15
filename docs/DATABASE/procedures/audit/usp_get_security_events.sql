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
GO

PRINT 'âœ… usp_get_security_events created successfully';
GO

