-- ============================================
-- FILE: tables/entities/entity_types.sql
-- PURPOSE: Registry of supported entity types
-- DEPENDENCIES: None (lookup table)
-- ============================================

PRINT '';
PRINT '📦 Creating table: entity_types';
PRINT '   Purpose: Registry of supported entity types';
PRINT '   Scale: ~10 types (static registry)';
PRINT '';

-- ============================================
-- TABLE: entity_types
-- PURPOSE: Registry of all entity types that can be added to workspaces
-- SCALE: ~10 types (grows slowly as new features added)
-- ============================================

CREATE TABLE entity_types (
    type_name NVARCHAR(50) PRIMARY KEY, -- 'tag', 'note', 'project', etc.
    
    -- Display info
    display_name NVARCHAR(100) NOT NULL, -- 'Tag', 'Note', 'Project'
    display_plural NVARCHAR(100) NOT NULL, -- 'Tags', 'Notes', 'Projects'
    icon NVARCHAR(50), -- Icon identifier (emoji or icon library key)
    color NVARCHAR(7), -- Hex color (#RRGGBB)
    description NVARCHAR(500),
    
    -- Technical info
    table_name NVARCHAR(128), -- Physical table name (for validation)
    supports_versioning BIT DEFAULT 0, -- Does this entity support version history?
    supports_sharing BIT DEFAULT 0, -- Can this entity be shared with users?
    
    -- Status
    is_enabled BIT DEFAULT 1,
    
    -- Metadata
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    updated_at DATETIME2 DEFAULT GETUTCDATE()
);

-- Comments
EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description',
    @value = N'Registry of entity types that can be added to workspaces. Extensible design.',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE', @level1name = N'entity_types';

PRINT '   ✅ entity_types table created';
GO

-- ============================================
-- DEFAULT DATA: Insert standard entity types
-- ============================================

PRINT '';
PRINT '📝 Inserting default entity types...';

INSERT INTO entity_types (
    type_name, display_name, display_plural, icon, color, 
    description, table_name, supports_versioning, supports_sharing, is_enabled
)
VALUES
    ('tag', 'Tag', 'Tags', '🏷️', '#2196F3', 
     'Hierarchical tags for organizing content', 'tags', 0, 0, 1),
    
    ('note', 'Note', 'Notes', '📝', '#4CAF50', 
     'Markdown notes with version history', 'notes', 1, 1, 1),
    
    ('project', 'Project', 'Projects', '📂', '#FF9800', 
     'Projects with tasks and milestones', 'projects', 0, 1, 0), -- Not implemented yet
    
    ('document', 'Document', 'Documents', '📄', '#9C27B0', 
     'Rich documents with attachments', 'documents', 1, 1, 0), -- Not implemented yet
    
    ('task', 'Task', 'Tasks', '✅', '#F44336', 
     'Action items with due dates', 'tasks', 0, 1, 0); -- Not implemented yet

PRINT '   ✅ 5 default entity types inserted';
PRINT '   ✅ Enabled: tag, note';
PRINT '   ⏸️  Disabled (not implemented): project, document, task';
GO

-- ============================================
-- VALIDATION
-- ============================================

PRINT '';
PRINT '📊 Verifying entity_types...';

SELECT 
    type_name,
    display_name,
    is_enabled,
    supports_versioning,
    supports_sharing
FROM entity_types
ORDER BY 
    CASE type_name 
        WHEN 'tag' THEN 1 
        WHEN 'note' THEN 2 
        ELSE 3 
    END;

GO

-- ============================================
-- SUCCESS MESSAGE
-- ============================================

PRINT '';
PRINT '✅ entity_types table created successfully!';
PRINT '   - 5 entity types registered';
PRINT '   - 2 types enabled (tag, note)';
PRINT '   - 3 types reserved for future (project, document, task)';
PRINT '';
GO
