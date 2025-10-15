-- ============================================
-- FILE: procedures/tags/usp_u_tag.sql
-- PURPOSE: Update existing tag
-- DEPENDENCIES: tags table
-- ============================================

PRINT '';
PRINT '📝 Creating procedure: usp_u_tag';
PRINT '   Purpose: Update tag properties';
PRINT '';

GO
CREATE OR ALTER PROCEDURE usp_u_tag
    @tag_id INT,
    @user_id INT,
    @name NVARCHAR(255) = NULL,
    @color NVARCHAR(7) = NULL,
    @icon NVARCHAR(50) = NULL,
    @description NVARCHAR(MAX) = NULL,
    @metadata NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Validate tag exists and user owns it
        IF NOT EXISTS (
            SELECT 1 FROM tags 
            WHERE id = @tag_id 
            AND user_id = @user_id 
            AND deleted_at IS NULL
        )
        BEGIN
            RAISERROR('Tag not found or not owned by user', 16, 1);
            RETURN;
        END
        
        -- Check duplicate name if name is being changed
        IF @name IS NOT NULL
        BEGIN
            IF EXISTS (
                SELECT 1 FROM tags
                WHERE user_id = @user_id
                AND name = @name
                AND id != @tag_id
                AND deleted_at IS NULL
            )
            BEGIN
                RAISERROR('Tag with same name already exists', 16, 1);
                RETURN;
            END
        END
        
        -- Update tag
        UPDATE tags
        SET 
            name = ISNULL(@name, name),
            color = CASE WHEN @color IS NOT NULL THEN @color ELSE color END,
            icon = CASE WHEN @icon IS NOT NULL THEN @icon ELSE icon END,
            description = CASE WHEN @description IS NOT NULL THEN @description ELSE description END,
            metadata = CASE WHEN @metadata IS NOT NULL THEN @metadata ELSE metadata END,
            updated_at = GETUTCDATE()
        WHERE id = @tag_id;
        
        -- Update slug if name changed
        IF @name IS NOT NULL
        BEGIN
            DECLARE @new_slug NVARCHAR(255);
            SET @new_slug = LOWER(REPLACE(@name, ' ', '-'));
            
            -- Make slug unique
            DECLARE @counter INT = 1;
            DECLARE @original_slug NVARCHAR(255) = @new_slug;
            WHILE EXISTS (
                SELECT 1 FROM tags 
                WHERE user_id = @user_id 
                AND slug = @new_slug
                AND id != @tag_id
                AND deleted_at IS NULL
            )
            BEGIN
                SET @new_slug = @original_slug + '-' + CAST(@counter AS NVARCHAR(10));
                SET @counter = @counter + 1;
            END
            
            UPDATE tags
            SET slug = @new_slug
            WHERE id = @tag_id;
        END
        
        COMMIT TRANSACTION;
        
        -- Return updated tag
        SELECT 
            id,
            user_id,
            name,
            slug,
            color,
            icon,
            description,
            metadata,
            usage_count,
            created_at,
            updated_at
        FROM tags
        WHERE id = @tag_id;
        
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
            
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END;
GO

PRINT '   ✅ usp_u_tag created';
GO
