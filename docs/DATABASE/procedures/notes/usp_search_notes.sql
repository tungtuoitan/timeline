CREATE OR ALTER PROCEDURE usp_search_notes
    @user_id INT,
    @search_text NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Search in name, description, and content
    SELECT 
        n.id,
        n.name,
        n.description,
        n.word_count,
        n.created_at,
        n.updated_at,
        nm.role AS user_role
    FROM notes n
    INNER JOIN note_members nm ON n.id = nm.note_id
    WHERE nm.user_id = @user_id
    AND nm.deleted_at IS NULL
    AND nm.invitation_status = 'active'
    AND n.deleted_at IS NULL
    AND n.is_archived = 0
    AND (
        n.name LIKE '%' + @search_text + '%'
        OR n.description LIKE '%' + @search_text + '%'
        OR n.content LIKE '%' + @search_text + '%'
    )
    ORDER BY 
        CASE 
            WHEN n.name LIKE '%' + @search_text + '%' THEN 1
            WHEN n.description LIKE '%' + @search_text + '%' THEN 2
            ELSE 3
        END,
        n.updated_at DESC;
END;
GO
GO

PRINT 'âœ… usp_search_notes created successfully';
GO
