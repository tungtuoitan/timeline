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

-- ============================================
-- SECTION 7: PERFORMANCE CONSIDERATIONS
-- ============================================

PRINT '';
PRINT '📊 Section 7: Audit log performance notes';

-- Note about audit log maintenance
PRINT '';
PRINT '📝 IMPORTANT: Audit Log Maintenance';
PRINT '   - audit_log table will grow over time';
PRINT '   - Run usp_cleanup_old_audit_logs periodically (recommended: monthly)';
PRINT '   - Consider archiving old logs before deletion';
PRINT '   - Monitor table size and index fragmentation';
PRINT '   - Default retention: 90 days (adjust as needed)';
PRINT '';

-- ============================================
-- SECTION 8: TESTING AUDIT TRIGGERS
-- ============================================

PRINT '';
PRINT '📊 Section 8: Audit trigger validation';

-- Procedure: Test audit triggers
GO

PRINT 'âœ… usp_cleanup_old_audit_logs created successfully';
GO

