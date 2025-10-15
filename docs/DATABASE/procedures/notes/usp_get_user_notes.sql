CREATE OR ALTER PROCEDURE usp_get_user_notes
    @user_id INT,
    @include_archived BIT = 0,
    @filter_role NVARCHAR(20) = NULL, -- Filter by role: 'owner', 'editor', 'viewer'
    @search_text NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        n.id,
        n.name,
        n.description,
        n.type,
        n.color,
        n.icon,
        n.is_archived,
        n.is_pinned,
        n.is_favorite,
        n.word_count,
        n.version_count,
        n.created_at,
        n.updated_at,
        nm.role AS user_role,
        CASE WHEN n.user_id = @user_id THEN 1 ELSE 0 END AS is_owner,
        n.user_id AS owner_id,
        u.username AS owner_username
    FROM notes n
    INNER JOIN note_members nm ON n.id = nm.note_id
    LEFT JOIN users u ON n.user_id = u.id
    WHERE nm.user_id = @user_id
    AND nm.deleted_at IS NULL
    AND nm.invitation_status = 'active'
    AND n.deleted_at IS NULL
    AND (@include_archived = 1 OR n.is_archived = 0)
    AND (@filter_role IS NULL OR nm.role = @filter_role)
    AND (
        @search_text IS NULL 
        OR n.name LIKE '%' + @search_text + '%'
        OR n.description LIKE '%' + @search_text + '%'
    )
    ORDER BY 
        n.is_pinned DESC,
        n.is_favorite DESC,
        n.updated_at DESC;
END;
GO
GO

PRINT 'âœ… usp_get_user_notes created successfully';
GO
