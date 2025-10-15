CREATE OR ALTER PROCEDURE usp_invalidate_workspace_cache
    @workspace_id INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DELETE FROM workspace_tree_cache
    WHERE workspace_id = @workspace_id;
    
    PRINT '✅ Cache invalidated for workspace: ' + CAST(@workspace_id AS VARCHAR);
END;
GO

PRINT '   ✅ usp_invalidate_workspace_cache created';

-- Procedure: Cleanup old caches
GO

PRINT 'âœ… usp_invalidate_workspace_cache created successfully';
GO

