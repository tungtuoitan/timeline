CREATE OR ALTER PROCEDURE usp_get_item_path_info
    @item_id BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Get item path
    DECLARE @item_path NVARCHAR(4000);
    DECLARE @workspace_id INT;
    
    SELECT 
        @item_path = item_path,
        @workspace_id = workspace_id
    FROM workspace_items
    WHERE id = @item_id
    AND deleted_at IS NULL;
    
    IF @item_path IS NULL
    BEGIN
        RAISERROR('Item not found or deleted', 16, 1);
        RETURN;
    END;
    
    -- Parse path and return each level
    -- Path format: '1.5.tag:10.note:123'
    DECLARE @path_parts TABLE (
        level INT,
        type NVARCHAR(50),
        id INT,
        path_segment NVARCHAR(100)
    );
    
    -- Split path by dots
    DECLARE @level INT = 0;
    DECLARE @start INT = 1;
    DECLARE @end INT;
    DECLARE @segment NVARCHAR(100);
    
    WHILE @start <= LEN(@item_path)
    BEGIN
        SET @end = CHARINDEX('.', @item_path, @start);
        IF @end = 0 SET @end = LEN(@item_path) + 1;
        
        SET @segment = SUBSTRING(@item_path, @start, @end - @start);
        
        -- Parse segment (either 'number' or 'type:number')
        IF CHARINDEX(':', @segment) > 0
        BEGIN
            INSERT INTO @path_parts (level, type, id, path_segment)
            VALUES (
                @level,
                LEFT(@segment, CHARINDEX(':', @segment) - 1),
                CAST(SUBSTRING(@segment, CHARINDEX(':', @segment) + 1, LEN(@segment)) AS INT),
                @segment
            );
        END
        ELSE
        BEGIN
            INSERT INTO @path_parts (level, type, id, path_segment)
            VALUES (
                @level,
                'tag',
                CAST(@segment AS INT),
                @segment
            );
        END;
        
        SET @level = @level + 1;
        SET @start = @end + 1;
    END;
    
    -- Return path with names
    SELECT 
        pp.level,
        pp.type,
        pp.id,
        CASE pp.type
            WHEN 'tag' THEN t.name
            WHEN 'note' THEN n.name
            ELSE NULL
        END AS name,
        pp.path_segment
    FROM @path_parts pp
    LEFT JOIN tags t ON pp.type = 'tag' AND pp.id = t.id
    LEFT JOIN notes n ON pp.type = 'note' AND pp.id = n.id
    ORDER BY pp.level;
END;
GO
GO

PRINT 'âœ… usp_get_item_path_info created successfully';
GO
