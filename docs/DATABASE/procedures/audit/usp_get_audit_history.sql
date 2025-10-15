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
GO

PRINT 'âœ… usp_get_audit_history created successfully';
GO

