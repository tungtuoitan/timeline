CREATE OR ALTER PROCEDURE usp_get_breadcrumb_cached
    @workspace_id INT,
    @item_path NVARCHAR(4000)
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Parse path segments
    DECLARE @segments TABLE (
        segment_path NVARCHAR(4000),
        level INT
    );
    
    DECLARE @current_path NVARCHAR(4000) = '';
    DECLARE @pos INT = 1;
    DECLARE @next_pos INT;
    DECLARE @segment NVARCHAR(100);
    DECLARE @level INT = 0;
    
    -- Split path by dots
    WHILE @pos <= LEN(@item_path)
    BEGIN
        SET @next_pos = CHARINDEX('.', @item_path, @pos);
        
        IF @next_pos = 0
            SET @next_pos = LEN(@item_path) + 1;
        
        SET @segment = SUBSTRING(@item_path, @pos, @next_pos - @pos);
        
        IF LEN(@current_path) = 0
            SET @current_path = @segment;
        ELSE
            SET @current_path = @current_path + '.' + @segment;
        
        INSERT INTO @segments (segment_path, level)
        VALUES (@current_path, @level);
        
        SET @level = @level + 1;
        SET @pos = @next_pos + 1;
    END;
    
    -- Return breadcrumb items
    SELECT 
        wtc.item_id,
        wtc.child_type,
        wtc.child_id,
        wtc.item_path,
        wtc.depth,
        wtc.item_name,
        wtc.item_color,
        s.level AS breadcrumb_level
    FROM @segments s
    INNER JOIN workspace_tree_cache wtc ON wtc.item_path = s.segment_path
    WHERE wtc.workspace_id = @workspace_id
    ORDER BY s.level;
END;
GO

PRINT '   ✅ usp_get_breadcrumb_cached created';

-- ============================================
-- SECTION 4: CACHE MAINTENANCE
-- ============================================

PRINT '';
PRINT '📊 Section 4: Cache maintenance procedures';

-- Procedure: Invalidate cache for a workspace
GO

PRINT 'âœ… usp_get_breadcrumb_cached created successfully';
GO

