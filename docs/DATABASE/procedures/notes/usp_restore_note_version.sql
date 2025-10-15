CREATE OR ALTER PROCEDURE usp_restore_note_version
    @note_id INT,
    @version_number INT,
    @user_id INT
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Check if user has edit permission
        DECLARE @user_role NVARCHAR(20);
        
        SELECT @user_role = role
        FROM note_members
        WHERE note_id = @note_id
        AND user_id = @user_id
        AND deleted_at IS NULL
        AND invitation_status = 'active';
        
        IF @user_role NOT IN ('owner', 'editor')
        BEGIN
            RAISERROR('You do not have permission to edit this note', 16, 1);
            RETURN;
        END;
        
        -- Get version content
        DECLARE @version_name NVARCHAR(200);
        DECLARE @version_description NVARCHAR(MAX);
        DECLARE @version_content NVARCHAR(MAX);
        DECLARE @version_word_count INT;
        
        SELECT 
            @version_name = name,
            @version_description = description,
            @version_content = content,
            @version_word_count = word_count
        FROM note_versions
        WHERE note_id = @note_id
        AND version_number = @version_number;
        
        IF @version_name IS NULL
        BEGIN
            RAISERROR('Version not found', 16, 1);
            RETURN;
        END;
        
        -- Update note with version content
        UPDATE notes
        SET 
            name = @version_name,
            description = @version_description,
            content = @version_content,
            word_count = @version_word_count,
            updated_at = GETUTCDATE()
        WHERE id = @note_id;
        
        -- New version will be created automatically by trigger
        
        -- Return updated note
        SELECT * FROM notes WHERE id = @note_id;
        
        COMMIT TRANSACTION;
        
        PRINT 'Note restored to version ' + CAST(@version_number AS NVARCHAR);
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
GO

PRINT 'âœ… usp_restore_note_version created successfully';
GO
