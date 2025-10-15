CREATE OR ALTER PROCEDURE usp_get_note_versions
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
    
    -- Return all versions
    SELECT 
        nv.*,
        u.username AS created_by_username,
        u.display_name AS created_by_display_name
    FROM note_versions nv
    INNER JOIN users u ON nv.created_by = u.id
    WHERE nv.note_id = @note_id
    ORDER BY nv.version_number DESC;
END;
GO
GO

PRINT 'âœ… usp_get_note_version created successfully';
GO
