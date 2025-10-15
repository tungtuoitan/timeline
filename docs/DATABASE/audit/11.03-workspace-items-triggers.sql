-- ============================================
-- FILE: audit/11.03-workspace-items-triggers.sql
-- PURPOSE: Audit triggers for workspace item movement and deletion
-- DEPENDENCIES: 07-tables-entities.sql, constraints/06.13-audit-log.sql
-- ============================================

USE SuperApp;
GO

SET NOCOUNT ON;

PRINT '🔒 Creating workspace item audit triggers...';
GO

-- Trigger: Audit item moves in workspace hierarchy
CREATE OR ALTER TRIGGER tr_audit_workspace_item_move
ON workspace_items
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Only log if item_path changed (indicates movement in hierarchy)
    IF UPDATE(item_path)
    BEGIN
        INSERT INTO audit_log (
            table_name, 
            record_id, 
            operation, 
            user_id, 
            old_values, 
            new_values,
            application_name
        )
        SELECT 
            'workspace_items',
            i.id,
            'ITEM_MOVE',
            NULL, -- User ID not directly available in workspace_items
            JSON_OBJECT(
                'workspace_id': d.workspace_id,
                'child_type': d.child_type,
                'child_id': d.child_id,
                'old_path': d.item_path,
                'old_depth': d.depth,
                'old_sort_order': d.sort_order
            ),
            JSON_OBJECT(
                'workspace_id': i.workspace_id,
                'child_type': i.child_type,
                'child_id': i.child_id,
                'new_path': i.item_path,
                'new_depth': i.depth,
                'new_sort_order': i.sort_order
            ),
            APP_NAME()
        FROM inserted i
        INNER JOIN deleted d ON i.id = d.id
        WHERE i.item_path != d.item_path;
    END;
END;
GO

PRINT '   ✅ tr_audit_workspace_item_move created';

-- Trigger: Audit item deletion from workspace
CREATE OR ALTER TRIGGER tr_audit_workspace_item_delete
ON workspace_items
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Only log if deleted_at changed from NULL to a value
    IF UPDATE(deleted_at)
    BEGIN
        INSERT INTO audit_log (
            table_name, 
            record_id, 
            operation, 
            user_id, 
            old_values,
            application_name
        )
        SELECT 
            'workspace_items',
            i.id,
            'ITEM_DELETE',
            NULL,
            JSON_OBJECT(
                'workspace_id': d.workspace_id,
                'child_type': d.child_type,
                'child_id': d.child_id,
                'item_path': d.item_path,
                'depth': d.depth,
                'deleted_at': CONVERT(VARCHAR(30), i.deleted_at, 127)
            ),
            APP_NAME()
        FROM inserted i
        INNER JOIN deleted d ON i.id = d.id
        WHERE i.deleted_at IS NOT NULL 
          AND d.deleted_at IS NULL;
    END;
END;
GO

PRINT '   ✅ tr_audit_workspace_item_delete created';
PRINT '📊 Next step: Run audit/11.04-notes-triggers.sql';
GO