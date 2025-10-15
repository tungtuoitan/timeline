CREATE OR ALTER PROCEDURE usp_unshare_note
    @note_id INT,
    @owner_user_id INT,
    @target_user_id INT
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Check if owner has permission
        IF NOT EXISTS (
            SELECT 1 FROM note_members
            WHERE note_id = @note_id
            AND user_id = @owner_user_id
            AND role = 'owner'
            AND deleted_at IS NULL
        )
        BEGIN
            RAISERROR('Only note owner can remove members', 16, 1);
            RETURN;
        END;
        
        -- Cannot remove owner
        IF EXISTS (
            SELECT 1 FROM note_members
            WHERE note_id = @note_id
            AND user_id = @target_user_id
            AND role = 'owner'
        )
        BEGIN
            RAISERROR('Cannot remove note owner', 16, 1);
            RETURN;
        END;
        
        -- Remove member (soft delete)
        UPDATE note_members
        SET 
            deleted_at = GETUTCDATE(),
            invitation_status = 'removed'
        WHERE note_id = @note_id
        AND user_id = @target_user_id;
        
        COMMIT TRANSACTION;
        
        PRINT 'Member removed from note';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
GO

PRINT 'âœ… usp_unshare_note created successfully';
GO
