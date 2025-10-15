CREATE OR ALTER PROCEDURE usp_remove_item_from_workspace
    @item_id BIGINT,
    @delete_descendants BIT = 0 -- If 1, delete all child items too
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Get item info
        DECLARE @workspace_id INT;
        DECLARE @item_path NVARCHAR(4000);
        
        SELECT 
            @workspace_id = workspace_id,
            @item_path = item_path
        FROM workspace_items
        WHERE id = @item_id
        AND deleted_at IS NULL;
        
        IF @workspace_id IS NULL
        BEGIN
            RAISERROR('Item not found or already deleted', 16, 1);
            RETURN;
        END;
        
        -- Soft delete item
        UPDATE workspace_items
        SET deleted_at = GETUTCDATE()
        WHERE id = @item_id;
        
        -- If delete_descendants, also delete all children
        IF @delete_descendants = 1
        BEGIN
            UPDATE workspace_items
            SET deleted_at = GETUTCDATE()
            WHERE workspace_id = @workspace_id
            AND item_path LIKE @item_path + '%'
            AND deleted_at IS NULL;
        END;
        
        COMMIT TRANSACTION;
        
        PRINT 'Item removed from workspace';
        
        SELECT @item_id AS deleted_item_id;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
GO

PRINT 'âœ… usp_remove_item_from_workspace created successfully';
GO
