CREATE OR ALTER PROCEDURE usp_delete_note
    @note_id INT,
    @user_id INT
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Check if user is owner
        IF NOT EXISTS (
            SELECT 1 FROM note_members
            WHERE note_id = @note_id
            AND user_id = @user_id
            AND role = 'owner'
            AND deleted_at IS NULL
        )
        BEGIN
            RAISERROR('Only note owner can delete the note', 16, 1);
            RETURN;
        END;
        
        -- Soft delete note
        UPDATE notes
        SET deleted_at = GETUTCDATE()
        WHERE id = @note_id;
        
        -- Soft delete all members
        UPDATE note_members
        SET deleted_at = GETUTCDATE()
        WHERE note_id = @note_id;
        
        -- Soft delete all workspace items containing this note
        UPDATE workspace_items
        SET deleted_at = GETUTCDATE()
        WHERE child_type = 'note'
        AND child_id = @note_id;
        
        COMMIT TRANSACTION;
        
        PRINT 'Note deleted';
        
        SELECT @note_id AS deleted_note_id;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
GO

PRINT 'âœ… usp_delete_note created successfully';
GO
