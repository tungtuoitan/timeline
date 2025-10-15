CREATE OR ALTER PROCEDURE usp_share_note
    @note_id INT,
    @owner_user_id INT, -- Current user sharing the note
    @new_user_id INT, -- User to share with
    @role NVARCHAR(20) = 'viewer' -- 'editor' or 'viewer'
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Validate role
        IF @role NOT IN ('editor', 'viewer')
        BEGIN
            RAISERROR('Invalid role. Must be editor or viewer', 16, 1);
            RETURN;
        END;
        
        -- Check if owner has permission to share
        IF NOT EXISTS (
            SELECT 1 FROM note_members
            WHERE note_id = @note_id
            AND user_id = @owner_user_id
            AND role = 'owner'
            AND deleted_at IS NULL
        )
        BEGIN
            RAISERROR('Only note owner can share the note', 16, 1);
            RETURN;
        END;
        
        -- Check if new user exists
        IF NOT EXISTS (SELECT 1 FROM users WHERE id = @new_user_id AND deleted_at IS NULL)
        BEGIN
            RAISERROR('User not found', 16, 1);
            RETURN;
        END;
        
        -- Check if already shared
        IF EXISTS (
            SELECT 1 FROM note_members
            WHERE note_id = @note_id
            AND user_id = @new_user_id
            AND deleted_at IS NULL
        )
        BEGIN
            RAISERROR('Note already shared with this user', 16, 1);
            RETURN;
        END;
        
        -- Add member
        INSERT INTO note_members (
            note_id, user_id, role, invited_by, invitation_status, joined_at
        )
        VALUES (
            @note_id, @new_user_id, @role, @owner_user_id, 'active', GETUTCDATE()
        );
        
        DECLARE @member_id BIGINT = SCOPE_IDENTITY();
        
        -- Return created member
        SELECT * FROM note_members WHERE id = @member_id;
        
        COMMIT TRANSACTION;
        
        PRINT 'Note shared successfully';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
GO

PRINT 'âœ… usp_share_note created successfully';
GO
