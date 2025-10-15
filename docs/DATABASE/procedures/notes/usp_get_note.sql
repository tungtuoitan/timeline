CREATE OR ALTER PROCEDURE usp_get_note
    @note_id INT,
    @user_id INT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Check if user has access to note
    IF NOT EXISTS (
        SELECT 1 FROM note_members
        WHERE note_id = @note_id
        AND user_id = @user_id
        AND deleted_at IS NULL
        AND invitation_status = 'active'
    )
    BEGIN
        RAISERROR('Note not found or access denied', 16, 1);
        RETURN;
    END;
    
    -- Return note with member info
    SELECT 
        n.*,
        nm.role AS user_role,
        CASE WHEN n.user_id = @user_id THEN 1 ELSE 0 END AS is_owner
    FROM notes n
    INNER JOIN note_members nm ON n.id = nm.note_id
    WHERE n.id = @note_id
    AND nm.user_id = @user_id
    AND n.deleted_at IS NULL
    AND nm.deleted_at IS NULL;
    
    -- Update last accessed time
    UPDATE note_members
    SET last_accessed_at = GETUTCDATE()
    WHERE note_id = @note_id
    AND user_id = @user_id;
END;
GO
GO

PRINT 'âœ… usp_get_note created successfully';
GO
