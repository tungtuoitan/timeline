-- ============================================
-- FILE: relationships/04.03-triggers.sql
-- PURPOSE: ⚠️ DEPRECATED - Triggers for workspace_tag_relationships
-- DEPENDENCIES: relationships/04.01-table.sql (do not use)
-- STATUS: Kept for reference only, replaced by workspace_items in 07-tables-entities.sql
-- ============================================

-- ⚠️⚠️⚠️ WARNING ⚠️⚠️⚠️
-- This file is DEPRECATED and should NOT be executed.
-- Replaced by: workspace_items (07-tables-entities.sql)
-- Migration: See 12-migration-guide.md

CREATE OR ALTER TRIGGER tr_wsrel_update_tag_usage
ON workspace_tag_relationships
AFTER INSERT, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Tags affected by INSERT
    DECLARE @affected_tags TABLE (tag_id INT);
    
    INSERT INTO @affected_tags (tag_id)
    SELECT DISTINCT from_tag_id FROM inserted
    UNION
    SELECT DISTINCT to_tag_id FROM inserted
    UNION
    SELECT DISTINCT from_tag_id FROM deleted
    UNION
    SELECT DISTINCT to_tag_id FROM deleted;
    
    -- Update usage_count for affected tags
    UPDATE t
    SET usage_count = (
        SELECT COUNT(DISTINCT workspace_id)
        FROM workspace_tag_relationships wtr
        WHERE (wtr.from_tag_id = t.id OR wtr.to_tag_id = t.id)
        AND wtr.deleted_at IS NULL
    )
    FROM tags t
    INNER JOIN @affected_tags at ON t.id = at.tag_id;
END;
GO

CREATE OR ALTER TRIGGER tr_wsrel_update_workspace_stats
ON workspace_tag_relationships
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Update tag_count and relationship_count for affected workspaces
    UPDATE w
    SET 
        relationship_count = (
            SELECT COUNT(*)
            FROM workspace_tag_relationships wtr
            WHERE wtr.workspace_id = w.id
            AND wtr.deleted_at IS NULL
        ),
        tag_count = (
            SELECT COUNT(DISTINCT tag_id)
            FROM (
                SELECT from_tag_id AS tag_id 
                FROM workspace_tag_relationships 
                WHERE workspace_id = w.id AND deleted_at IS NULL
                UNION
                SELECT to_tag_id AS tag_id 
                FROM workspace_tag_relationships 
                WHERE workspace_id = w.id AND deleted_at IS NULL
            ) AS unique_tags
        ),
        updated_at = GETUTCDATE()
    FROM workspaces w
    WHERE w.id IN (
        SELECT DISTINCT workspace_id FROM inserted
        UNION
        SELECT DISTINCT workspace_id FROM deleted
    );
END;
GO

CREATE OR ALTER TRIGGER tr_wsrel_validate_type
ON workspace_tag_relationships
INSTEAD OF INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Check if relationship_type exists for workspace
    IF EXISTS (
        SELECT 1
        FROM inserted i
        WHERE NOT EXISTS (
            SELECT 1
            FROM workspace_relationship_types wrt
            WHERE wrt.workspace_id = i.workspace_id
            AND wrt.type_name = i.relationship_type
            AND wrt.deleted_at IS NULL
        )
    )
    BEGIN
        RAISERROR('Relationship type does not exist for this workspace', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END;
    
    -- Proceed with INSERT
    IF EXISTS (SELECT 1 FROM inserted) AND NOT EXISTS (SELECT 1 FROM deleted)
    BEGIN
        INSERT INTO workspace_tag_relationships (
            workspace_id, from_tag_id, to_tag_id, relationship_type,
            from_path, to_path, depth, sort_order, label, color, line_style,
            is_bidirectional, strength, metadata, created_by, updated_by,
            created_at, updated_at
        )
        SELECT 
            workspace_id, from_tag_id, to_tag_id, relationship_type,
            from_path, to_path, depth, sort_order, label, color, line_style,
            is_bidirectional, strength, metadata, created_by, updated_by,
            created_at, updated_at
        FROM inserted;
    END
    
    -- Proceed with UPDATE
    IF EXISTS (SELECT 1 FROM deleted)
    BEGIN
        UPDATE wtr
        SET 
            relationship_type = i.relationship_type,
            from_path = i.from_path,
            to_path = i.to_path,
            depth = i.depth,
            sort_order = i.sort_order,
            label = i.label,
            color = i.color,
            line_style = i.line_style,
            is_bidirectional = i.is_bidirectional,
            strength = i.strength,
            metadata = i.metadata,
            updated_by = i.updated_by,
            updated_at = GETUTCDATE()
        FROM workspace_tag_relationships wtr
        INNER JOIN inserted i ON wtr.id = i.id;
    END
END;
GO

CREATE OR ALTER TRIGGER tr_wsrel_prevent_cycles
ON workspace_tag_relationships
INSTEAD OF INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Check if workspace type allows cycles
    DECLARE @allows_cycles BIT;
    DECLARE @workspace_id INT;
    DECLARE @workspace_type NVARCHAR(20);
    DECLARE @relationship_type NVARCHAR(50);
    
    SELECT TOP 1 
        @workspace_id = i.workspace_id,
        @relationship_type = i.relationship_type
    FROM inserted i;
    
    -- Get workspace type
    SELECT @workspace_type = type
    FROM workspaces
    WHERE id = @workspace_id;
    
    -- Get relationship type settings
    SELECT @allows_cycles = allows_cycles
    FROM workspace_relationship_types
    WHERE workspace_id = @workspace_id
    AND type_name = @relationship_type
    AND deleted_at IS NULL;
    
    -- If cycles not allowed, check for potential cycles
    IF @allows_cycles = 0
    BEGIN
        -- Check if adding this relationship creates a cycle
        -- Cycle exists if: to_tag is an ancestor of from_tag
        IF EXISTS (
            SELECT 1
            FROM inserted i
            INNER JOIN workspace_tag_relationships existing
                ON existing.workspace_id = i.workspace_id
                AND existing.from_tag_id = i.to_tag_id
                AND existing.deleted_at IS NULL
            WHERE existing.to_path LIKE '%.' + CAST(i.from_tag_id AS NVARCHAR) + '.%'
               OR existing.to_path LIKE '%.' + CAST(i.from_tag_id AS NVARCHAR)
               OR existing.to_path = CAST(i.from_tag_id AS NVARCHAR)
        )
        BEGIN
            RAISERROR('This relationship would create a cycle, which is not allowed for this workspace type', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END;
    END;
    
    -- Proceed with operation (delegate to validation trigger)
    -- Note: This is simplified - in production, you'd merge these triggers
    -- or use a different pattern to avoid nested triggers
    
    -- For INSERT
    IF NOT EXISTS (SELECT 1 FROM deleted)
    BEGIN
        INSERT INTO workspace_tag_relationships (
            workspace_id, from_tag_id, to_tag_id, relationship_type,
            from_path, to_path, depth, sort_order, label, color, line_style,
            is_bidirectional, strength, metadata, created_by, updated_by
        )
        SELECT 
            workspace_id, from_tag_id, to_tag_id, relationship_type,
            from_path, to_path, depth, sort_order, label, color, line_style,
            is_bidirectional, strength, metadata, created_by, updated_by
        FROM inserted;
    END
    ELSE
    BEGIN
        -- UPDATE
        UPDATE wtr
        SET 
            from_tag_id = i.from_tag_id,
            to_tag_id = i.to_tag_id,
            relationship_type = i.relationship_type,
            from_path = i.from_path,
            to_path = i.to_path,
            depth = i.depth,
            sort_order = i.sort_order,
            label = i.label,
            color = i.color,
            line_style = i.line_style,
            is_bidirectional = i.is_bidirectional,
            strength = i.strength,
            metadata = i.metadata,
            updated_by = i.updated_by,
            updated_at = GETUTCDATE()
        FROM workspace_tag_relationships wtr
        INNER JOIN inserted i ON wtr.id = i.id;
    END
END;
GO

CREATE OR ALTER TRIGGER tr_wsrel_updated_at
ON workspace_tag_relationships
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE workspace_tag_relationships
    SET updated_at = GETUTCDATE()
    WHERE id IN (SELECT id FROM inserted);
END;
GO

PRINT '⚠️ DEPRECATED: Triggers for workspace_tag_relationships created for reference only!';
PRINT '   Replaced by: workspace_items in 07-tables-entities.sql';
PRINT '   Migration: See 12-migration-guide.md';
PRINT '📊 Next step: Run relationships/04.04-views.sql (for reference only)';
GO