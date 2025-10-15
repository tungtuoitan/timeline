CREATE OR ALTER PROCEDURE usp_update_note
    @note_id INT,
    @user_id INT,
    @name NVARCHAR(200) = NULL,
    @description NVARCHAR(MAX) = NULL,
    @content NVARCHAR(MAX) = NULL,
    @type NVARCHAR(100) = NULL,
    @color NVARCHAR(7) = NULL,
    @icon NVARCHAR(50) = NULL,
    @is_archived BIT = NULL,
    @is_pinned BIT = NULL,
    @is_favorite BIT = NULL,
    @metadata NVARCHAR(MAX) = NULL,
    @change_summary NVARCHAR(500) = NULL -- Version change description
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
        
        IF @user_role IS NULL
        BEGIN
            RAISERROR('Note not found or access denied', 16, 1);
            RETURN;
        END;
        
        IF @user_role NOT IN ('owner', 'editor')
        BEGIN
            RAISERROR('You do not have permission to edit this note', 16, 1);
            RETURN;
        END;
        
        -- Calculate word count if content provided
        DECLARE @word_count INT;
        IF @content IS NOT NULL
        BEGIN
            SET @word_count = LEN(@content) - LEN(REPLACE(@content, ' ', '')) + 1;
        END;
        
        -- Update note (only fields that are provided)
        UPDATE notes
        SET 
            name = ISNULL(@name, name),
            description = ISNULL(@description, description),
            content = ISNULL(@content, content),
            type = ISNULL(@type, type),
            color = ISNULL(@color, color),
            icon = ISNULL(@icon, icon),
            is_archived = ISNULL(@is_archived, is_archived),
            is_pinned = ISNULL(@is_pinned, is_pinned),
            is_favorite = ISNULL(@is_favorite, is_favorite),
            word_count = ISNULL(@word_count, word_count),
            metadata = ISNULL(@metadata, metadata),
            updated_at = GETUTCDATE()
        WHERE id = @note_id;
        
        -- Version is automatically created by trigger if content/name/description changed
        
        -- Return updated note
        SELECT * FROM notes WHERE id = @note_id;
        
        COMMIT TRANSACTION;
        
        PRINT 'Note updated';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
GO

PRINT 'âœ… usp_update_note created successfully';
GO
