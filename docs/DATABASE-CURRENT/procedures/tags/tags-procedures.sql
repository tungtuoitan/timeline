-- =============================================
-- TAG PROCEDURES (4 procedures)
-- Status: ✅ DEPLOYED (MVP)
-- =============================================

-- =============================================
-- 1. usp_i_tag - Create tag
-- =============================================
CREATE PROCEDURE usp_i_tag
    @user_id INT,
    @name NVARCHAR(255),
    @color NVARCHAR(7) = '#3B82F6',
    @icon NVARCHAR(50) = NULL,
    @description NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- Insert tag
        INSERT INTO tags (user_id, name, color, icon, description)
        VALUES (@user_id, @name, @color, @icon, @description);

        DECLARE @tag_id INT = SCOPE_IDENTITY();

        -- Return created tag
        SELECT * FROM tags WHERE tag_id = @tag_id;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH;
END;
GO

-- =============================================
-- 2. usp_s_tag - Get tag by ID
-- =============================================
CREATE PROCEDURE usp_s_tag
    @tag_id INT,
    @user_id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT * FROM tags
    WHERE tag_id = @tag_id
    AND user_id = @user_id
    AND deleted_at IS NULL;
END;
GO

-- =============================================
-- 3. usp_u_tag - Update tag
-- =============================================
CREATE PROCEDURE usp_u_tag
    @tag_id INT,
    @user_id INT,
    @name NVARCHAR(255) = NULL,
    @color NVARCHAR(7) = NULL,
    @icon NVARCHAR(50) = NULL,
    @description NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Check ownership
    IF NOT EXISTS (
        SELECT 1 FROM tags
        WHERE tag_id = @tag_id
        AND user_id = @user_id
        AND deleted_at IS NULL
    )
    BEGIN
        RAISERROR('Tag not found or access denied', 16, 1);
        RETURN;
    END;

    -- Update tag
    UPDATE tags
    SET
        name = COALESCE(@name, name),
        color = COALESCE(@color, color),
        icon = COALESCE(@icon, icon),
        description = COALESCE(@description, description),
        updated_at = GETUTCDATE()
    WHERE tag_id = @tag_id;

    -- Return updated tag
    SELECT * FROM tags WHERE tag_id = @tag_id;
END;
GO

-- =============================================
-- 4. usp_s_user_tags - List user's tags
-- =============================================
CREATE PROCEDURE usp_s_user_tags
    @user_id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        tag_id,
        name,
        slug,
        color,
        icon,
        description,
        usage_count,
        created_at,
        updated_at
    FROM tags
    WHERE user_id = @user_id
    AND deleted_at IS NULL
    ORDER BY usage_count DESC, name ASC;
END;
GO
