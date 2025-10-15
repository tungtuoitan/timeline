CREATE OR ALTER PROCEDURE usp_change_note_member_role
    @note_id INT,
    @owner_user_id INT,
    @target_user_id INT,
    @new_role NVARCHAR(20) -- 'editor' or 'viewer'
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Validate role
        IF @new_role NOT IN ('editor', 'viewer')
        BEGIN
            RAISERROR('Invalid role. Must be editor or viewer', 16, 1);
            RETURN;
        END;
        
        -- Check if owner has permission
        IF NOT EXISTS (
            SELECT 1 FROM note_members
            WHERE note_id = @note_id
            AND user_id = @owner_user_id
            AND role = 'owner'
            AND deleted_at IS NULL
        )
        BEGIN
            RAISERROR('Only note owner can change member roles', 16, 1);
            RETURN;
        END;
        
        -- Cannot change owner's role
        IF EXISTS (
            SELECT 1 FROM note_members
            WHERE note_id = @note_id
            AND user_id = @target_user_id
            AND role = 'owner'
        )
        BEGIN
            RAISERROR('Cannot change owner role', 16, 1);
            RETURN;
        END;
        
        -- Update role
        UPDATE note_members
        SET role = @new_role
        WHERE note_id = @note_id
        AND user_id = @target_user_id
        AND deleted_at IS NULL;
        
        COMMIT TRANSACTION;
        
        PRINT 'Member role updated';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
GO

PRINT 'âœ… usp_change_note_member_role created successfully';
GO
