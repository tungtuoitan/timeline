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
GO

PRINT 'âœ… usp_get_user_activity created successfully';
GO

