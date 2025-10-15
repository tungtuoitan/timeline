CREATE OR ALTER PROCEDURE usp_get_note_members
    @note_id INT,
    @user_id INT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Check access
    IF NOT EXISTS (
        SELECT 1 FROM note_members
        WHERE note_id = @note_id
        AND user_id = @user_id
        AND deleted_at IS NULL
        AND invitation_status = 'active'
    )
    BEGIN
        RAISERROR('Access denied', 16, 1);
        RETURN;
    END;
    
    -- Return all members
    SELECT 
        nm.*,
        u.username,
        u.display_name,
        u.email
    FROM note_members nm
    INNER JOIN users u ON nm.user_id = u.id
    WHERE nm.note_id = @note_id
    AND nm.deleted_at IS NULL
    ORDER BY 
        CASE nm.role 
            WHEN 'owner' THEN 1
            WHEN 'editor' THEN 2
            WHEN 'viewer' THEN 3
        END,
        nm.joined_at;
END;
GO
GO

PRINT 'âœ… usp_get_note_members created successfully';
GO
