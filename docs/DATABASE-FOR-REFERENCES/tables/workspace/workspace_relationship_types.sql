-- ============================================
-- FILE: workspace_relationship_types.sql
-- PURPOSE: Define available relationship types per workspace
-- CATEGORY: Workspace Tables
-- SCALE: ~5 types per workspace, 100+ total
-- DEPENDENCIES: workspace/workspaces.sql
-- ============================================

PRINT '📦 Creating table: workspace_relationship_types...';
GO

-- ============================================
-- TABLE: workspace_relationship_types
-- ============================================

CREATE TABLE workspace_relationship_types (
    id INT IDENTITY(1,1) PRIMARY KEY,
    
    -- Workspace
    workspace_id INT NOT NULL,
    
    -- Type definition
    type_name NVARCHAR(50) NOT NULL,
    display_name NVARCHAR(100) NOT NULL,
    description NVARCHAR(500),
    
    -- Visual properties
    icon NVARCHAR(50),
    color NVARCHAR(7),
    line_style NVARCHAR(20) DEFAULT 'solid', -- 'solid', 'dashed', 'dotted'
    line_width INT DEFAULT 2, -- Pixels
    
    -- Relationship behavior
    is_bidirectional BIT DEFAULT 0, -- If true, create reverse edge automatically
    allows_cycles BIT DEFAULT 0, -- Allow loops in relationships
    max_depth INT NULL, -- Limit depth for this type (NULL = no limit)
    
    -- Validation rules (future use)
    validation_rules NVARCHAR(MAX), -- JSON: { min_connections, max_connections, ... }
    
    -- Display order
    sort_order INT DEFAULT 0,
    
    -- Timestamps
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    updated_at DATETIME2 DEFAULT GETUTCDATE(),
    deleted_at DATETIME2 NULL,
    
    -- Constraints
    CONSTRAINT fk_wsreltype_workspace FOREIGN KEY (workspace_id) 
        REFERENCES workspaces(id) ON DELETE CASCADE,
    
    CONSTRAINT ck_wsreltype_line_style CHECK (
        line_style IN ('solid', 'dashed', 'dotted')
    ),
    
    -- Unique: type_name per workspace
    CONSTRAINT uq_wsreltype_name UNIQUE (workspace_id, type_name, deleted_at)
);
GO

-- ============================================
-- INDEXES
-- ============================================

-- Get all types for a workspace
CREATE INDEX ix_wsreltype_workspace ON workspace_relationship_types(
    workspace_id, sort_order, deleted_at
) WHERE deleted_at IS NULL;

-- Lookup by type name
CREATE INDEX ix_wsreltype_name ON workspace_relationship_types(
    workspace_id, type_name, deleted_at
) WHERE deleted_at IS NULL;

GO

-- ============================================
-- EXTENDED PROPERTIES (Comments)
-- ============================================

EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description',
    @value = N'Defines available relationship types for each workspace. Example: parent_child, reports_to, depends_on',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE', @level1name = N'workspace_relationship_types';

GO

PRINT '✅ Table created: workspace_relationship_types';
PRINT '   - 2 indexes created';
GO
