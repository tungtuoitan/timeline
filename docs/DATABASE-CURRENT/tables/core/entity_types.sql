-- =============================================
-- TABLE: entity_types
-- Description: Registry of entity types supported in workspaces
-- Status: ✅ DEPLOYED (MVP) - Only 'tag' and 'note' enabled
-- =============================================

CREATE TABLE entity_types (
    -- Primary Key
    type_name NVARCHAR(50) PRIMARY KEY,

    -- Display info
    display_name NVARCHAR(100) NOT NULL,
    display_plural NVARCHAR(100) NOT NULL,
    icon NVARCHAR(50),
    color NVARCHAR(7),
    description NVARCHAR(500),

    -- Technical info
    table_name NVARCHAR(128), -- Physical table name
    supports_versioning BIT DEFAULT 0,
    supports_sharing BIT DEFAULT 0,

    -- Status
    is_enabled BIT DEFAULT 1, -- Whether this type is active

    -- Timestamps
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    updated_at DATETIME2 DEFAULT GETUTCDATE()
);

-- =============================================
-- DEFAULT DATA (Seeded with table creation)
-- =============================================
-- INSERT INTO entity_types VALUES
-- ('tag', 'Tag', 'Tags', '🏷️', '#2196F3', 'Hierarchical tags...', 'tags', 0, 0, 1),
-- ('note', 'Note', 'Notes', '📝', '#4CAF50', 'Markdown notes...', 'notes', 1, 1, 1),
-- ('project', 'Project', 'Projects', '📂', '#FF9800', 'Projects...', 'projects', 0, 1, 0),
-- ('document', 'Document', 'Documents', '📄', '#9C27B0', 'Rich documents...', 'documents', 1, 1, 0),
-- ('task', 'Task', 'Tasks', '✅', '#F44336', 'Action items...', 'tasks', 0, 1, 0);

-- =============================================
-- NOTES
-- =============================================
-- - No indexes needed (lookup table, ~5 rows)
-- - Only 'tag' and 'note' are enabled in MVP
-- - Future: 'project', 'document', 'task' can be enabled
