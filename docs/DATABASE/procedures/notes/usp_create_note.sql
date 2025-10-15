CREATE OR ALTER PROCEDURE usp_create_note
    @user_id INT,
    @name NVARCHAR(200),
    @description NVARCHAR(MAX) = NULL,
    @content NVARCHAR(MAX) = NULL,
    @type NVARCHAR(100) = NULL,
    @color NVARCHAR(7) = NULL,
    @icon NVARCHAR(50) = NULL,
    @metadata NVARCHAR(MAX) = NULL -- JSON
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Validate user exists
        IF NOT EXISTS (SELECT 1 FROM users WHERE id = @user_id AND deleted_at IS NULL)
        BEGIN
            RAISERROR('User not found or deleted', 16, 1);
            RETURN;
        END;
        
        -- Calculate word count
        DECLARE @word_count INT;
        SET @word_count = CASE 
            WHEN @content IS NULL THEN 0
            ELSE LEN(@content) - LEN(REPLACE(@content, ' ', '')) + 1
        END;
        
        -- Create note
        INSERT INTO notes (
            user_id, name, description, content, type,
            color, icon, word_count, metadata
        )
        VALUES (
            @user_id, @name, @description, @content, @type,
            @color, @icon, @word_count, @metadata
        );
        
        DECLARE @note_id INT = SCOPE_IDENTITY();
        
        -- Owner is automatically added by trigger tr_notes_add_owner
        -- Initial version is automatically created by trigger tr_notes_create_version
        
        -- Return created note
        SELECT * FROM notes WHERE id = @note_id;
        
        COMMIT TRANSACTION;
        
        PRINT 'Note created: ' + @name;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
GO

PRINT 'âœ… usp_create_note created successfully';
GO
