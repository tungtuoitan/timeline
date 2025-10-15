CREATE OR ALTER PROCEDURE usp_restore_note
    @note_id INT,
    @user_id INT
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Check if user was owner
        IF NOT EXISTS (
            SELECT 1 FROM note_members
            WHERE note_id = @note_id
            AND user_id = @user_id
            AND role = 'owner'
        )
        BEGIN
            RAISERROR('Only note owner can restore the note', 16, 1);
            RETURN;
        END;
        
        -- Restore note
        UPDATE notes
        SET deleted_at = NULL
        WHERE id = @note_id;
        
        -- Restore members
        UPDATE note_members
        SET deleted_at = NULL
        WHERE note_id = @note_id;
        
        -- Note: workspace_items are NOT automatically restored
        -- User needs to re-add note to workspaces manually
        
        COMMIT TRANSACTION;
        
        PRINT 'Note restored';
        
        SELECT * FROM notes WHERE id = @note_id;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
GO

PRINT 'âœ… usp_restore_note created successfully';
GO
