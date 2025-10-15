CREATE OR ALTER PROCEDURE usp_cleanup_old_caches
    @hours_old INT = 24
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @cutoff DATETIME2 = DATEADD(HOUR, -@hours_old, GETUTCDATE());
    DECLARE @deleted_count INT;
    
    DELETE FROM workspace_tree_cache
    WHERE cached_at < @cutoff;
    
    SET @deleted_count = @@ROWCOUNT;
    
    PRINT '✅ Cleaned up old caches';
    PRINT '   Deleted entries: ' + CAST(@deleted_count AS VARCHAR);
    PRINT '   Older than: ' + CAST(@hours_old AS VARCHAR) + ' hours';
END;
GO

PRINT '   ✅ usp_cleanup_old_caches created';

-- Procedure: Get cache statistics
GO

PRINT 'âœ… usp_cleanup_old_caches created successfully';
GO

