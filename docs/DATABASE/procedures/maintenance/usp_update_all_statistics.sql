CREATE OR ALTER PROCEDURE usp_update_all_statistics
AS
BEGIN
    SET NOCOUNT ON;
    
    PRINT 'Updating statistics...';
    
    UPDATE STATISTICS users WITH FULLSCAN;
    UPDATE STATISTICS tags WITH FULLSCAN;
    UPDATE STATISTICS workspaces WITH FULLSCAN;
    UPDATE STATISTICS workspace_members WITH FULLSCAN;
    UPDATE STATISTICS workspace_relationship_types WITH FULLSCAN;
    UPDATE STATISTICS workspace_items WITH FULLSCAN;
    UPDATE STATISTICS entity_types WITH FULLSCAN;
    UPDATE STATISTICS notes WITH FULLSCAN;
    UPDATE STATISTICS note_members WITH FULLSCAN;
    UPDATE STATISTICS note_versions WITH FULLSCAN;
    
    PRINT 'Statistics update completed';
END;
GO

PRINT '   ✅ usp_update_all_statistics created';

-- Procedure: Get index usage statistics
GO

PRINT 'âœ… usp_update_all_statistics created successfully';
GO

