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
GO

PRINT 'âœ… usp_get_workspace_audit_trail created successfully';
GO

