-- ============================================
-- FILE: materialized_views/workspace-tree-cache-table.sql
-- PURPOSE: Create workspace tree cache table
-- DEPENDENCIES: 02-tables-core.sql, 03-tables-workspace.sql, 07-tables-entities.sql
-- ============================================

USE SuperApp;
GO

SET NOCOUNT ON;

PRINT '🔄 Creating workspace tree cache table...';
GO

-- Drop existing table if exists
IF OBJECT_ID('workspace_tree_cache', 'U') IS NOT NULL
    DROP TABLE workspace_tree_cache;
GO

-- Create materialized view table for workspace tree
CREATE TABLE workspace_tree_cache (
    -- Primary key
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    
    -- Workspace identification
    workspace_id INT NOT NULL,
    
    -- Item identification
    item_id BIGINT NOT NULL,
    child_type NVARCHAR(50) NOT NULL,
    child_id INT NOT NULL,
    
    -- Hierarchy information
    item_path NVARCHAR(4000) NOT NULL,
    depth INT NOT NULL,
    parent_path NVARCHAR(4000) NULL,
    
    -- Item details (denormalized for fast access)
    item_name NVARCHAR(200) NOT NULL,
    item_description NVARCHAR(1000) NULL,
    item_color NVARCHAR(7) NULL,
    
    -- Relationship details
    relationship_type NVARCHAR(50) NULL,
    label NVARCHAR(200) NULL,
    sort_order INT NOT NULL DEFAULT 0,
    
    -- Computed fields for quick filtering
    is_root BIT NOT NULL DEFAULT 0,
    is_leaf BIT NOT NULL DEFAULT 0,
    child_count INT NOT NULL DEFAULT 0,
    descendant_count INT NOT NULL DEFAULT 0,
    
    -- Metadata
    cached_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    -- Indexes for fast queries
    INDEX ix_tree_cache_workspace (workspace_id, depth, sort_order),
    INDEX ix_tree_cache_path (workspace_id, item_path),
    INDEX ix_tree_cache_parent (workspace_id, parent_path, sort_order),
    INDEX ix_tree_cache_child (child_type, child_id),
    INDEX ix_tree_cache_refresh (workspace_id, cached_at)
);

PRINT '   ✅ workspace_tree_cache table created';
PRINT '📊 Next step: Run materialized_views/refresh-procedures.sql';
GO