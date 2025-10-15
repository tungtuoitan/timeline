CREATE OR ALTER PROCEDURE usp_reorder_items
    @workspace_id INT,
    @parent_tag_id INT,
    @item_orders NVARCHAR(MAX) -- JSON: [{"item_id":1,"sort_order":0},{"item_id":2,"sort_order":1}]
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Parse JSON
        DECLARE @orders_table TABLE (
            item_id BIGINT,
            sort_order INT
        );
        
        INSERT INTO @orders_table (item_id, sort_order)
        SELECT 
            JSON_VALUE(value, '$.item_id'),
            JSON_VALUE(value, '$.sort_order')
        FROM OPENJSON(@item_orders);
        
        -- Update sort orders
        UPDATE wi
        SET 
            sort_order = ot.sort_order,
            updated_at = GETUTCDATE()
        FROM workspace_items wi
        INNER JOIN @orders_table ot ON wi.id = ot.item_id
        WHERE wi.workspace_id = @workspace_id
        AND wi.parent_tag_id = @parent_tag_id
        AND wi.deleted_at IS NULL;
        
        COMMIT TRANSACTION;
        
        PRINT 'Items reordered successfully';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
GO

PRINT 'âœ… usp_reorder_items created successfully';
GO
