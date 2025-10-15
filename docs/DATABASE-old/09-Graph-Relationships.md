# 09 - Tag Graph Relationships System

Hệ thống graph relationships cho phép tạo semantic connections giữa các tags, tương tự như Obsidian graph view hoặc knowledge graphs.

---

## 📋 Table of Contents

1. [Overview](#overview)
2. [Architecture Analysis](#architecture-analysis)
3. [Schema Design](#schema-design)
4. [Stored Procedures](#stored-procedures)
5. [Graph Algorithms](#graph-algorithms)
6. [Use Cases & Examples](#use-cases--examples)
7. [Performance Optimization](#performance-optimization)
8. [UI/UX Integration](#uiux-integration)
9. [Migration Guide](#migration-guide)
10. [Advanced Features](#advanced-features)

---

## 🎯 Overview

### What is Tag Graph Relationships?

Graph relationships bổ sung **semantic connections** cho hierarchical tag structure hiện có. Trong khi closure table quản lý parent-child relationships (taxonomy), graph relationships quản lý cross-cutting relationships (ontology).

### Comparison: Hierarchy vs Graph

```
HIERARCHY (Closure Table):
Programming Languages
├─ JavaScript
│  ├─ React
│  └─ Vue
└─ Python
   └─ Django

GRAPH (Relationships):
JavaScript ─related_to→ TypeScript
JavaScript ─prerequisite_for→ React
React ─similar_to→ Vue
React ─used_with→ Redux
Python ─opposite_of→ JavaScript (dynamically typed)
```

### Key Features

- ✅ **Multiple relationship types** (related_to, prerequisite_for, similar_to, etc.)
- ✅ **Bidirectional & directional** relationships
- ✅ **Weighted edges** (strength 0.0 - 1.0)
- ✅ **Graph traversal** (BFS/DFS)
- ✅ **Path finding** between tags
- ✅ **Smart recommendations** based on connections
- ✅ **Cycle detection**
- ✅ **Compatible** with existing closure table

### Inspired By

- **Obsidian** - Note linking and graph view
- **Roam Research** - Bidirectional links
- **Knowledge Graphs** - Semantic web, RDF
- **Neo4j** - Graph database patterns

---

## 🏗️ Architecture Analysis

### Design Decision: Hybrid Approach

**✅ RECOMMENDED: Closure Table + Graph Relationships**

```
┌─────────────────────────────────────────────┐
│  HIERARCHICAL STRUCTURE (Existing)          │
│  ├─ tags table (adjacency list)             │
│  └─ tag_paths (closure table)               │
│     → Fast subtree queries                  │
│     → Parent-child relationships            │
└─────────────────────────────────────────────┘
              ↓
┌─────────────────────────────────────────────┐
│  SEMANTIC RELATIONSHIPS (New)               │
│  ├─ tag_relationships (graph edges)         │
│  └─ tag_relationship_types (edge types)     │
│     → Semantic connections                  │
│     → Cross-taxonomy links                  │
└─────────────────────────────────────────────┘
```

**Why not pure graph database?**

| Approach | Pros | Cons | Verdict |
|----------|------|------|---------|
| **Hybrid (closure + graph)** | ✅ Best of both worlds<br>✅ No migration needed<br>✅ Flexible | ⚠️ Two systems to maintain | ✅ **RECOMMENDED** |
| **Pure SQL Server Graph** | ✅ Native MATCH syntax<br>✅ Optimized queries | ❌ Requires full migration<br>❌ Less flexible<br>❌ Breaking changes | ❌ Not suitable |
| **External graph DB (Neo4j)** | ✅ Best graph performance | ❌ Extra infrastructure<br>❌ Data sync complexity<br>❌ Additional cost | ❌ Overkill |

### Compatibility with Existing System

```sql
-- Existing closure table queries: UNAFFECTED
SELECT t.* FROM tags t
JOIN tag_paths p ON t.id = p.descendant_id
WHERE p.ancestor_id = 5;

-- New graph queries: ADDITIVE
SELECT t.* FROM tags t
JOIN tag_relationships r ON t.id = r.to_tag_id
WHERE r.from_tag_id = 5 AND r.relationship_type = 'related_to';

-- Combined query: POWERFUL
SELECT DISTINCT t.*
FROM tags t
WHERE t.id IN (
    -- Hierarchical descendants
    SELECT descendant_id FROM tag_paths WHERE ancestor_id = 5
    UNION
    -- Semantic relationships
    SELECT to_tag_id FROM tag_relationships WHERE from_tag_id = 5
);
```

---

## 🗄️ Schema Design

### Core Tables

```sql
-- ============================================
-- TAG RELATIONSHIPS: Semantic graph edges
-- ============================================
CREATE TABLE tag_relationships (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    
    -- Edge definition
    from_tag_id INT NOT NULL,
    to_tag_id INT NOT NULL,
    
    -- Relationship metadata
    relationship_type NVARCHAR(50) NOT NULL,
    -- Examples: 'related_to', 'similar_to', 'prerequisite_for', 
    --           'opposite_of', 'part_of', 'used_with', 'replaces',
    --           'conflicts_with', 'alternative_to'
    
    -- Edge properties
    is_bidirectional BIT DEFAULT 1,
    -- TRUE: A ↔ B (symmetric, e.g., "similar_to")
    -- FALSE: A → B (directional, e.g., "prerequisite_for")
    
    strength DECIMAL(3,2) DEFAULT 1.0,
    -- Weight/confidence: 0.0 (weak) to 1.0 (strong)
    -- Used for: recommendations, graph algorithms, filtering
    
    -- User context
    user_id INT NOT NULL,  -- Owner of the relationship
    -- Note: Both tags must be accessible to this user
    
    -- Descriptive metadata
    description NVARCHAR(500),
    notes NVARCHAR(MAX),  -- Optional detailed explanation
    
    -- Lifecycle
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    created_by INT NOT NULL,
    updated_at DATETIME2 DEFAULT GETUTCDATE(),
    updated_by INT NULL,
    deleted_at DATETIME2 NULL,
    
    -- Foreign keys
    CONSTRAINT fk_tagrel_from FOREIGN KEY (from_tag_id) 
        REFERENCES tags(id) ON DELETE CASCADE,
    CONSTRAINT fk_tagrel_to FOREIGN KEY (to_tag_id) 
        REFERENCES tags(id),
    CONSTRAINT fk_tagrel_user FOREIGN KEY (user_id) 
        REFERENCES users(id) ON DELETE CASCADE,
    CONSTRAINT fk_tagrel_creator FOREIGN KEY (created_by) 
        REFERENCES users(id),
    CONSTRAINT fk_tagrel_updater FOREIGN KEY (updated_by) 
        REFERENCES users(id),
    
    -- Constraints
    CONSTRAINT ck_tagrel_no_self CHECK (from_tag_id != to_tag_id),
    CONSTRAINT ck_tagrel_strength CHECK (strength BETWEEN 0.0 AND 1.0),
    
    -- Unique: Can't have duplicate relationship
    CONSTRAINT uq_tag_relationship UNIQUE (
        from_tag_id, to_tag_id, relationship_type, user_id, deleted_at
    )
);

-- Indexes for graph traversal performance
CREATE INDEX ix_tagrel_from 
    ON tag_relationships(from_tag_id, relationship_type, strength DESC, deleted_at)
    WHERE deleted_at IS NULL;

CREATE INDEX ix_tagrel_to 
    ON tag_relationships(to_tag_id, relationship_type, strength DESC, deleted_at)
    WHERE deleted_at IS NULL;

CREATE INDEX ix_tagrel_user 
    ON tag_relationships(user_id, created_at DESC)
    WHERE deleted_at IS NULL;

CREATE INDEX ix_tagrel_type 
    ON tag_relationships(relationship_type, strength DESC)
    WHERE deleted_at IS NULL;

CREATE INDEX ix_tagrel_bidirectional 
    ON tag_relationships(is_bidirectional, relationship_type)
    WHERE is_bidirectional = 1 AND deleted_at IS NULL;

-- Composite index for common graph queries
CREATE INDEX ix_tagrel_graph_traversal
    ON tag_relationships(from_tag_id, to_tag_id, relationship_type, strength)
    INCLUDE (is_bidirectional, description)
    WHERE deleted_at IS NULL;

-- ============================================
-- TAG RELATIONSHIP TYPES: Define edge types
-- ============================================
CREATE TABLE tag_relationship_types (
    type_name NVARCHAR(50) PRIMARY KEY,
    
    display_name NVARCHAR(100) NOT NULL,
    description NVARCHAR(500),
    
    -- Default properties
    is_bidirectional_default BIT DEFAULT 1,
    default_strength DECIMAL(3,2) DEFAULT 1.0,
    
    -- UI/UX metadata
    icon NVARCHAR(50),           -- Icon identifier (e.g., '🔗', 'link-icon')
    color NVARCHAR(7),            -- Hex color for visualization
    line_style NVARCHAR(20),      -- 'solid', 'dashed', 'dotted'
    
    -- Semantic properties
    inverse_type NVARCHAR(50),    -- e.g., 'prerequisite_for' ↔ 'required_by'
    category NVARCHAR(50),        -- Group related types
    
    -- Admin
    is_system BIT DEFAULT 0,      -- System-defined (can't delete)
    is_active BIT DEFAULT 1,
    sort_order INT DEFAULT 0,
    
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    updated_at DATETIME2 DEFAULT GETUTCDATE()
);

-- Foreign key from relationships to types
ALTER TABLE tag_relationships
ADD CONSTRAINT fk_tagrel_type FOREIGN KEY (relationship_type) 
    REFERENCES tag_relationship_types(type_name);

-- ============================================
-- SEED DATA: Common relationship types
-- ============================================
INSERT INTO tag_relationship_types (
    type_name, display_name, description, 
    is_bidirectional_default, default_strength,
    icon, color, line_style, category, is_system, sort_order
) VALUES
-- Semantic relationships (bidirectional)
('related_to', 'Related To', 'General semantic relationship between concepts', 
    1, 0.7, '🔗', '#3F51B5', 'solid', 'semantic', 1, 1),
    
('similar_to', 'Similar To', 'Tags with similar meaning or use cases', 
    1, 0.8, '≈', '#2196F3', 'solid', 'semantic', 1, 2),
    
('opposite_of', 'Opposite Of', 'Antonyms or contrasting concepts', 
    1, 1.0, '⇄', '#F44336', 'solid', 'semantic', 1, 3),
    
('alternative_to', 'Alternative To', 'Different approaches to same problem', 
    1, 0.7, '↔', '#FF9800', 'dashed', 'semantic', 1, 4),

-- Hierarchical (directional)
('prerequisite_for', 'Prerequisite For', 'Must learn/know A before B', 
    0, 1.0, '→', '#4CAF50', 'solid', 'learning', 1, 5),
    
('part_of', 'Part Of', 'Component or subset relationship', 
    0, 1.0, '⊂', '#9C27B0', 'solid', 'hierarchical', 1, 6),
    
('extends', 'Extends', 'Builds upon or extends functionality', 
    0, 0.8, '↗', '#00BCD4', 'solid', 'hierarchical', 1, 7),

-- Practical (bidirectional)
('used_with', 'Used With', 'Commonly used together or in combination', 
    1, 0.7, '⚡', '#8BC34A', 'solid', 'practical', 1, 8),
    
('conflicts_with', 'Conflicts With', 'Mutually exclusive or incompatible', 
    1, 1.0, '⚠', '#FF5722', 'dotted', 'practical', 1, 9),

-- Temporal (directional)
('replaces', 'Replaces', 'Supersedes or replaces older version', 
    0, 0.9, '⇒', '#795548', 'solid', 'temporal', 1, 10),
    
('evolved_from', 'Evolved From', 'Historical relationship or origin', 
    0, 0.6, '↪', '#607D8B', 'dashed', 'temporal', 1, 11),

-- Cross-reference
('see_also', 'See Also', 'Related topic worth exploring', 
    1, 0.5, '👁', '#9E9E9E', 'dotted', 'reference', 1, 12);

-- ============================================
-- RELATIONSHIP AUDIT LOG
-- ============================================
CREATE TABLE tag_relationship_audit (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    relationship_id BIGINT NOT NULL,
    
    action NVARCHAR(50) NOT NULL,  -- 'CREATED', 'UPDATED', 'DELETED'
    
    -- Snapshot before change (JSON)
    old_values NVARCHAR(MAX),
    new_values NVARCHAR(MAX),
    
    performed_by INT NOT NULL,
    performed_at DATETIME2 DEFAULT GETUTCDATE(),
    
    CONSTRAINT fk_relaudit_rel FOREIGN KEY (relationship_id) 
        REFERENCES tag_relationships(id),
    CONSTRAINT fk_relaudit_user FOREIGN KEY (performed_by) 
        REFERENCES users(id)
);

CREATE INDEX ix_relaudit_rel ON tag_relationship_audit(relationship_id, performed_at DESC);
CREATE INDEX ix_relaudit_user ON tag_relationship_audit(performed_by, performed_at DESC);

-- ============================================
-- MATERIALIZED VIEW: Relationship statistics
-- ============================================
CREATE VIEW vw_tag_relationship_stats AS
SELECT 
    t.id AS tag_id,
    t.name AS tag_name,
    t.user_id,
    
    -- Outgoing relationships
    COUNT(DISTINCT r_out.id) AS outgoing_count,
    AVG(r_out.strength) AS avg_outgoing_strength,
    
    -- Incoming relationships
    COUNT(DISTINCT r_in.id) AS incoming_count,
    AVG(r_in.strength) AS avg_incoming_strength,
    
    -- Total connections
    COUNT(DISTINCT r_out.id) + COUNT(DISTINCT r_in.id) AS total_connections,
    
    -- By type
    COUNT(DISTINCT CASE WHEN r_out.relationship_type = 'related_to' THEN r_out.id END) AS related_count,
    COUNT(DISTINCT CASE WHEN r_out.relationship_type = 'similar_to' THEN r_out.id END) AS similar_count,
    COUNT(DISTINCT CASE WHEN r_out.relationship_type = 'prerequisite_for' THEN r_out.id END) AS prereq_count
    
FROM tags t
LEFT JOIN tag_relationships r_out ON r_out.from_tag_id = t.id AND r_out.deleted_at IS NULL
LEFT JOIN tag_relationships r_in ON r_in.to_tag_id = t.id AND r_in.deleted_at IS NULL
WHERE t.deleted_at IS NULL
GROUP BY t.id, t.name, t.user_id;
GO
```

---

## 🛠️ Stored Procedures

### 1. Create Relationship

```sql
CREATE OR ALTER PROCEDURE usp_i_tag_relationship
    @from_tag_id INT,
    @to_tag_id INT,
    @relationship_type NVARCHAR(50),
    @user_id INT,
    @strength DECIMAL(3,2) = NULL,
    @is_bidirectional BIT = NULL,
    @description NVARCHAR(500) = NULL,
    @notes NVARCHAR(MAX) = NULL,
    @auto_create_reverse BIT = 1  -- Create reverse edge if bidirectional
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- =============================================
        -- Validation: Tags exist and accessible
        -- =============================================
        IF NOT EXISTS (
            SELECT 1 FROM tags 
            WHERE id = @from_tag_id 
            AND user_id = @user_id 
            AND deleted_at IS NULL
        )
        BEGIN
            RAISERROR('Source tag not found or access denied', 16, 1);
            RETURN;
        END;
        
        IF NOT EXISTS (
            SELECT 1 FROM tags 
            WHERE id = @to_tag_id 
            AND user_id = @user_id 
            AND deleted_at IS NULL
        )
        BEGIN
            RAISERROR('Target tag not found or access denied', 16, 1);
            RETURN;
        END;
        
        -- =============================================
        -- Validation: Self-reference
        -- =============================================
        IF @from_tag_id = @to_tag_id
        BEGIN
            RAISERROR('Cannot create relationship from tag to itself', 16, 1);
            RETURN;
        END;
        
        -- =============================================
        -- Validation: Relationship type exists
        -- =============================================
        DECLARE @default_bidirectional BIT, @default_strength DECIMAL(3,2);
        
        SELECT 
            @default_bidirectional = is_bidirectional_default,
            @default_strength = default_strength
        FROM tag_relationship_types
        WHERE type_name = @relationship_type 
        AND is_active = 1;
        
        IF @default_bidirectional IS NULL
        BEGIN
            RAISERROR('Invalid or inactive relationship type', 16, 1);
            RETURN;
        END;
        
        -- Use defaults if not provided
        SET @is_bidirectional = COALESCE(@is_bidirectional, @default_bidirectional);
        SET @strength = COALESCE(@strength, @default_strength);
        
        -- =============================================
        -- Validation: Duplicate relationship
        -- =============================================
        IF EXISTS (
            SELECT 1 FROM tag_relationships
            WHERE from_tag_id = @from_tag_id
            AND to_tag_id = @to_tag_id
            AND relationship_type = @relationship_type
            AND user_id = @user_id
            AND deleted_at IS NULL
        )
        BEGIN
            RAISERROR('Relationship already exists', 16, 1);
            RETURN;
        END;
        
        -- =============================================
        -- Validation: Conflicting directional relationships
        -- =============================================
        IF @is_bidirectional = 0 AND EXISTS (
            SELECT 1 FROM tag_relationships
            WHERE from_tag_id = @to_tag_id
            AND to_tag_id = @from_tag_id
            AND relationship_type = @relationship_type
            AND is_bidirectional = 0
            AND user_id = @user_id
            AND deleted_at IS NULL
        )
        BEGIN
            RAISERROR('Conflicting directional relationship exists (reverse direction)', 16, 1);
            RETURN;
        END;
        
        -- =============================================
        -- Create relationship
        -- =============================================
        INSERT INTO tag_relationships (
            from_tag_id, to_tag_id, relationship_type,
            is_bidirectional, strength, description, notes,
            user_id, created_by
        )
        VALUES (
            @from_tag_id, @to_tag_id, @relationship_type,
            @is_bidirectional, @strength, @description, @notes,
            @user_id, @user_id
        );
        
        DECLARE @relationship_id BIGINT = SCOPE_IDENTITY();
        
        -- =============================================
        -- Create reverse relationship if bidirectional
        -- =============================================
        IF @is_bidirectional = 1 AND @auto_create_reverse = 1
        BEGIN
            -- Check if reverse doesn't already exist
            IF NOT EXISTS (
                SELECT 1 FROM tag_relationships
                WHERE from_tag_id = @to_tag_id
                AND to_tag_id = @from_tag_id
                AND relationship_type = @relationship_type
                AND user_id = @user_id
                AND deleted_at IS NULL
            )
            BEGIN
                INSERT INTO tag_relationships (
                    from_tag_id, to_tag_id, relationship_type,
                    is_bidirectional, strength, description, notes,
                    user_id, created_by
                )
                VALUES (
                    @to_tag_id, @from_tag_id, @relationship_type,
                    1, @strength, @description, @notes,
                    @user_id, @user_id
                );
            END;
        END;
        
        -- =============================================
        -- Audit log
        -- =============================================
        INSERT INTO tag_relationship_audit (
            relationship_id, action, new_values, performed_by
        )
        VALUES (
            @relationship_id,
            'CREATED',
            (SELECT 
                from_tag_id = @from_tag_id,
                to_tag_id = @to_tag_id,
                relationship_type = @relationship_type,
                strength = @strength
             FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
            @user_id
        );
        
        -- =============================================
        -- Return created relationship
        -- =============================================
        SELECT 
            r.id,
            r.from_tag_id,
            t1.name AS from_tag_name,
            r.to_tag_id,
            t2.name AS to_tag_name,
            r.relationship_type,
            rt.display_name AS relationship_display,
            r.is_bidirectional,
            r.strength,
            r.description,
            r.created_at
        FROM tag_relationships r
        JOIN tags t1 ON t1.id = r.from_tag_id
        JOIN tags t2 ON t2.id = r.to_tag_id
        JOIN tag_relationship_types rt ON rt.type_name = r.relationship_type
        WHERE r.id = @relationship_id;
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
```

### 2. Get Tag Relationships (Graph View)

```sql
CREATE OR ALTER PROCEDURE usp_s_tag_relationships
    @tag_id INT,
    @user_id INT,
    @relationship_types NVARCHAR(MAX) = NULL,  -- JSON array: ["related_to", "similar_to"]
    @direction NVARCHAR(10) = 'both',  -- 'outgoing', 'incoming', 'both'
    @max_depth INT = 1,  -- How many hops to traverse
    @min_strength DECIMAL(3,2) = 0.0,
    @include_hierarchy BIT = 0  -- Also include parent-child from closure table
AS
BEGIN
    SET NOCOUNT ON;
    
    -- =============================================
    -- Validate access
    -- =============================================
    IF NOT EXISTS (
        SELECT 1 FROM tags 
        WHERE id = @tag_id 
        AND user_id = @user_id 
        AND deleted_at IS NULL
    )
    BEGIN
        RAISERROR('Tag not found or access denied', 16, 1);
        RETURN;
    END;
    
    -- =============================================
    -- Parse relationship types filter
    -- =============================================
    DECLARE @type_filter TABLE (type_name NVARCHAR(50));
    IF @relationship_types IS NOT NULL
    BEGIN
        INSERT INTO @type_filter
        SELECT value FROM OPENJSON(@relationship_types);
    END;
    
    -- =============================================
    -- Traverse graph with BFS
    -- =============================================
    WITH GraphTraversal AS (
        -- Level 0: Starting tag
        SELECT 
            @tag_id AS tag_id,
            @tag_id AS source_tag_id,
            CAST(NULL AS BIGINT) AS relationship_id,
            CAST(NULL AS NVARCHAR(50)) AS relationship_type,
            CAST(NULL AS BIT) AS is_bidirectional,
            CAST(NULL AS DECIMAL(3,2)) AS strength,
            CAST(NULL AS NVARCHAR(500)) AS description,
            0 AS depth,
            CAST(@tag_id AS NVARCHAR(MAX)) AS path,
            'start' AS direction
        
        UNION ALL
        
        -- Level 1+: Connected tags (outgoing)
        SELECT 
            r.to_tag_id,
            gt.source_tag_id,
            r.id,
            r.relationship_type,
            r.is_bidirectional,
            r.strength,
            r.description,
            gt.depth + 1,
            gt.path + '->' + CAST(r.to_tag_id AS NVARCHAR(MAX)),
            'outgoing'
        FROM GraphTraversal gt
        JOIN tag_relationships r ON r.from_tag_id = gt.tag_id
        WHERE gt.depth < @max_depth
        AND (@direction IN ('outgoing', 'both'))
        AND r.deleted_at IS NULL
        AND r.strength >= @min_strength
        AND r.user_id = @user_id
        AND (@relationship_types IS NULL OR EXISTS (
            SELECT 1 FROM @type_filter WHERE type_name = r.relationship_type
        ))
        -- Prevent cycles
        AND gt.path NOT LIKE '%' + CAST(r.to_tag_id AS NVARCHAR) + '%'
        
        UNION ALL
        
        -- Level 1+: Connected tags (incoming)
        SELECT 
            r.from_tag_id,
            gt.source_tag_id,
            r.id,
            r.relationship_type,
            r.is_bidirectional,
            r.strength,
            r.description,
            gt.depth + 1,
            gt.path + '<-' + CAST(r.from_tag_id AS NVARCHAR(MAX)),
            'incoming'
        FROM GraphTraversal gt
        JOIN tag_relationships r ON r.to_tag_id = gt.tag_id
        WHERE gt.depth < @max_depth
        AND (@direction IN ('incoming', 'both'))
        AND r.deleted_at IS NULL
        AND r.strength >= @min_strength
        AND r.user_id = @user_id
        AND (@relationship_types IS NULL OR EXISTS (
            SELECT 1 FROM @type_filter WHERE type_name = r.relationship_type
        ))
        -- Prevent cycles
        AND gt.path NOT LIKE '%' + CAST(r.from_tag_id AS NVARCHAR) + '%'
    )
    
    -- =============================================
    -- Return nodes and edges
    -- =============================================
    SELECT DISTINCT
        -- Node information
        t.id AS tag_id,
        t.name AS tag_name,
        t.color AS tag_color,
        t.path AS tag_path,
        t.slug AS tag_slug,
        
        -- Graph traversal info
        gt.depth,
        gt.direction,
        
        -- Edge information (NULL for root node)
        gt.relationship_id,
        gt.relationship_type,
        rt.display_name AS relationship_display,
        rt.icon AS relationship_icon,
        rt.color AS relationship_color,
        rt.line_style AS relationship_line_style,
        gt.is_bidirectional,
        gt.strength,
        gt.description AS relationship_description,
        
        -- Statistics
        ISNULL(stats.total_connections, 0) AS node_total_connections
        
    FROM GraphTraversal gt
    JOIN tags t ON t.id = gt.tag_id
    LEFT JOIN tag_relationship_types rt ON rt.type_name = gt.relationship_type
    LEFT JOIN vw_tag_relationship_stats stats ON stats.tag_id = t.id
    WHERE t.deleted_at IS NULL
    ORDER BY gt.depth, t.name;
    
    -- =============================================
    -- Optional: Include hierarchical relationships
    -- =============================================
    IF @include_hierarchy = 1
    BEGIN
        SELECT 
            t.id AS tag_id,
            t.name AS tag_name,
            t.parent_id,
            p_tag.name AS parent_name,
            'hierarchical' AS relationship_source,
            tp.depth AS hierarchy_depth
        FROM tags t
        JOIN tag_paths tp ON tp.descendant_id = t.id
        LEFT JOIN tags p_tag ON p_tag.id = t.parent_id
        WHERE tp.ancestor_id = @tag_id
        AND t.user_id = @user_id
        AND t.deleted_at IS NULL
        ORDER BY tp.depth, t.name;
    END;
END;
GO
```

### 3. Find Shortest Path Between Tags

```sql
CREATE OR ALTER PROCEDURE usp_find_tag_path
    @from_tag_id INT,
    @to_tag_id INT,
    @user_id INT,
    @max_depth INT = 5,
    @relationship_types NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    -- =============================================
    -- Validate tags
    -- =============================================
    IF NOT EXISTS (
        SELECT 1 FROM tags 
        WHERE id IN (@from_tag_id, @to_tag_id)
        AND user_id = @user_id 
        AND deleted_at IS NULL
        HAVING COUNT(*) = 2
    )
    BEGIN
        RAISERROR('One or both tags not found or access denied', 16, 1);
        RETURN;
    END;
    
    -- Parse type filter
    DECLARE @type_filter TABLE (type_name NVARCHAR(50));
    IF @relationship_types IS NOT NULL
    BEGIN
        INSERT INTO @type_filter
        SELECT value FROM OPENJSON(@relationship_types);
    END;
    
    -- =============================================
    -- BFS to find shortest path
    -- =============================================
    WITH PathFinder AS (
        -- Start node
        SELECT 
            @from_tag_id AS current_tag_id,
            CAST(@from_tag_id AS NVARCHAR(MAX)) AS path_ids,
            CAST('' AS NVARCHAR(MAX)) AS path_types,
            0 AS depth,
            CAST(0 AS BIT) AS found
        
        UNION ALL
        
        -- Traverse (bidirectional search)
        SELECT 
            r.to_tag_id,
            pf.path_ids + ',' + CAST(r.to_tag_id AS NVARCHAR(MAX)),
            pf.path_types + '|' + r.relationship_type,
            pf.depth + 1,
            CASE WHEN r.to_tag_id = @to_tag_id THEN 1 ELSE 0 END
        FROM PathFinder pf
        JOIN tag_relationships r ON r.from_tag_id = pf.current_tag_id
        WHERE pf.found = 0  -- Haven't reached destination yet
        AND pf.depth < @max_depth
        AND r.deleted_at IS NULL
        AND r.user_id = @user_id
        AND (@relationship_types IS NULL OR EXISTS (
            SELECT 1 FROM @type_filter WHERE type_name = r.relationship_type
        ))
        -- Prevent cycles
        AND pf.path_ids NOT LIKE '%,' + CAST(r.to_tag_id AS NVARCHAR) + ',%'
        AND pf.path_ids NOT LIKE CAST(r.to_tag_id AS NVARCHAR) + ',%'
    )
    
    -- =============================================
    -- Return shortest path
    -- =============================================
    SELECT TOP 1
        pf.path_ids,
        pf.path_types,
        pf.depth,
        -- Parse path into readable format
        (
            SELECT 
                tag_id = CAST(value AS INT),
                tag_name = t.name,
                step_number = ROW_NUMBER() OVER (ORDER BY (SELECT NULL))
            FROM STRING_SPLIT(pf.path_ids, ',')
            CROSS APPLY (
                SELECT name FROM tags WHERE id = CAST(value AS INT)
            ) t
            FOR JSON PATH
        ) AS path_details,
        'FOUND' AS status
    FROM PathFinder pf
    WHERE pf.found = 1
    ORDER BY pf.depth;
    
    IF @@ROWCOUNT = 0
    BEGIN
        SELECT 
            NULL AS path_ids,
            NULL AS path_types,
            NULL AS depth,
            NULL AS path_details,
            'NOT_FOUND' AS status;
    END;
END;
GO
```

### 4. Get Recommended Tags

```sql
CREATE OR ALTER PROCEDURE usp_s_recommended_tags
    @user_id INT,
    @base_tag_ids NVARCHAR(MAX),  -- JSON array: [1, 5, 10]
    @limit INT = 10,
    @min_strength DECIMAL(3,2) = 0.5,
    @relationship_types NVARCHAR(MAX) = NULL,
    @exclude_tag_ids NVARCHAR(MAX) = NULL  -- Already used/owned tags
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Parse inputs
    DECLARE @base_tags TABLE (tag_id INT);
    INSERT INTO @base_tags
    SELECT CAST(value AS INT) FROM OPENJSON(@base_tag_ids);
    
    DECLARE @exclude_tags TABLE (tag_id INT);
    IF @exclude_tag_ids IS NOT NULL
    BEGIN
        INSERT INTO @exclude_tags
        SELECT CAST(value AS INT) FROM OPENJSON(@exclude_tag_ids);
    END;
    
    DECLARE @type_filter TABLE (type_name NVARCHAR(50));
    IF @relationship_types IS NOT NULL
    BEGIN
        INSERT INTO @type_filter
        SELECT value FROM OPENJSON(@relationship_types);
    END;
    
    -- =============================================
    -- Find related tags with scoring algorithm
    -- =============================================
    WITH RelatedTags AS (
        SELECT 
            r.to_tag_id,
            r.relationship_type,
            r.strength,
            -- Scoring factors
            CASE r.relationship_type
                WHEN 'prerequisite_for' THEN 1.2  -- Boost learning paths
                WHEN 'used_with' THEN 1.1         -- Boost practical combos
                WHEN 'similar_to' THEN 1.0
                WHEN 'related_to' THEN 0.9
                ELSE 0.8
            END AS type_multiplier
        FROM @base_tags bt
        JOIN tag_relationships r ON r.from_tag_id = bt.tag_id
        WHERE r.deleted_at IS NULL
        AND r.user_id = @user_id
        AND r.strength >= @min_strength
        AND (@relationship_types IS NULL OR EXISTS (
            SELECT 1 FROM @type_filter WHERE type_name = r.relationship_type
        ))
    )
    
    SELECT TOP (@limit)
        t.id AS tag_id,
        t.name AS tag_name,
        t.color AS tag_color,
        t.path AS tag_path,
        t.slug AS tag_slug,
        t.description AS tag_description,
        
        -- Recommendation metrics
        SUM(rt.strength * rt.type_multiplier) AS relevance_score,
        COUNT(*) AS connection_count,
        MAX(rt.strength) AS max_strength,
        AVG(rt.strength) AS avg_strength,
        
        -- Relationship breakdown
        STRING_AGG(rtt.display_name, ', ') AS relationship_types,
        
        -- Additional context
        ISNULL(stats.total_connections, 0) AS tag_total_connections
        
    FROM RelatedTags rt
    JOIN tags t ON t.id = rt.to_tag_id
    JOIN tag_relationship_types rtt ON rtt.type_name = rt.relationship_type
    LEFT JOIN vw_tag_relationship_stats stats ON stats.tag_id = t.id
    WHERE t.deleted_at IS NULL
    AND t.user_id = @user_id
    -- Exclude base tags
    AND NOT EXISTS (SELECT 1 FROM @base_tags WHERE tag_id = t.id)
    -- Exclude explicitly excluded tags
    AND NOT EXISTS (SELECT 1 FROM @exclude_tags WHERE tag_id = t.id)
    
    GROUP BY 
        t.id, t.name, t.color, t.path, t.slug, t.description,
        stats.total_connections
    
    ORDER BY 
        relevance_score DESC,
        connection_count DESC,
        tag_total_connections DESC;
END;
GO
```

### 5. Update Relationship

```sql
CREATE OR ALTER PROCEDURE usp_u_tag_relationship
    @relationship_id BIGINT,
    @user_id INT,
    @strength DECIMAL(3,2) = NULL,
    @description NVARCHAR(500) = NULL,
    @notes NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Validate ownership
        IF NOT EXISTS (
            SELECT 1 FROM tag_relationships
            WHERE id = @relationship_id
            AND user_id = @user_id
            AND deleted_at IS NULL
        )
        BEGIN
            RAISERROR('Relationship not found or access denied', 16, 1);
            RETURN;
        END;
        
        -- Capture old values for audit
        DECLARE @old_values NVARCHAR(MAX);
        SELECT @old_values = (
            SELECT strength, description, notes
            FROM tag_relationships
            WHERE id = @relationship_id
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );
        
        -- Update
        UPDATE tag_relationships
        SET 
            strength = COALESCE(@strength, strength),
            description = COALESCE(@description, description),
            notes = COALESCE(@notes, notes),
            updated_at = GETUTCDATE(),
            updated_by = @user_id
        WHERE id = @relationship_id;
        
        -- Audit
        DECLARE @new_values NVARCHAR(MAX);
        SELECT @new_values = (
            SELECT strength, description, notes
            FROM tag_relationships
            WHERE id = @relationship_id
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );
        
        INSERT INTO tag_relationship_audit (
            relationship_id, action, old_values, new_values, performed_by
        )
        VALUES (@relationship_id, 'UPDATED', @old_values, @new_values, @user_id);
        
        -- Return updated relationship
        SELECT 
            r.*,
            t1.name AS from_tag_name,
            t2.name AS to_tag_name,
            rt.display_name AS relationship_display
        FROM tag_relationships r
        JOIN tags t1 ON t1.id = r.from_tag_id
        JOIN tags t2 ON t2.id = r.to_tag_id
        JOIN tag_relationship_types rt ON rt.type_name = r.relationship_type
        WHERE r.id = @relationship_id;
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
```

### 6. Delete Relationship

```sql
CREATE OR ALTER PROCEDURE usp_d_tag_relationship
    @relationship_id BIGINT,
    @user_id INT,
    @delete_reverse BIT = 1  -- Also delete reverse if bidirectional
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Validate ownership
        DECLARE @from_tag_id INT, @to_tag_id INT, @is_bidirectional BIT, @rel_type NVARCHAR(50);
        
        SELECT 
            @from_tag_id = from_tag_id,
            @to_tag_id = to_tag_id,
            @is_bidirectional = is_bidirectional,
            @rel_type = relationship_type
        FROM tag_relationships
        WHERE id = @relationship_id
        AND user_id = @user_id
        AND deleted_at IS NULL;
        
        IF @from_tag_id IS NULL
        BEGIN
            RAISERROR('Relationship not found or access denied', 16, 1);
            RETURN;
        END;
        
        -- Soft delete
        UPDATE tag_relationships
        SET deleted_at = GETUTCDATE(),
            updated_by = @user_id
        WHERE id = @relationship_id;
        
        DECLARE @deleted_count INT = 1;
        
        -- Delete reverse relationship if bidirectional
        IF @is_bidirectional = 1 AND @delete_reverse = 1
        BEGIN
            UPDATE tag_relationships
            SET deleted_at = GETUTCDATE(),
                updated_by = @user_id
            WHERE from_tag_id = @to_tag_id
            AND to_tag_id = @from_tag_id
            AND relationship_type = @rel_type
            AND user_id = @user_id
            AND deleted_at IS NULL;
            
            SET @deleted_count = @deleted_count + @@ROWCOUNT;
        END;
        
        -- Audit
        INSERT INTO tag_relationship_audit (
            relationship_id, action, performed_by
        )
        VALUES (@relationship_id, 'DELETED', @user_id);
        
        SELECT 
            deleted_count = @deleted_count,
            message = CASE 
                WHEN @deleted_count > 1 THEN 'Relationship and reverse deleted'
                ELSE 'Relationship deleted'
            END;
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
```

### 7. Bulk Create Relationships

```sql
CREATE OR ALTER PROCEDURE usp_bulk_i_tag_relationships
    @user_id INT,
    @relationships NVARCHAR(MAX)  -- JSON array of relationships
    -- Example: [
    --   {"from": 1, "to": 2, "type": "related_to", "strength": 0.8},
    --   {"from": 1, "to": 3, "type": "similar_to"}
    -- ]
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        
        DECLARE @results TABLE (
            from_tag_id INT,
            to_tag_id INT,
            relationship_type NVARCHAR(50),
            status NVARCHAR(50),
            relationship_id BIGINT
        );
        
        -- Parse and insert
        INSERT INTO tag_relationships (
            from_tag_id, to_tag_id, relationship_type, strength,
            is_bidirectional, user_id, created_by
        )
        OUTPUT 
            inserted.from_tag_id,
            inserted.to_tag_id,
            inserted.relationship_type,
            'CREATED',
            inserted.id
        INTO @results
        SELECT 
            CAST(JSON_VALUE(value, '$.from') AS INT),
            CAST(JSON_VALUE(value, '$.to') AS INT),
            JSON_VALUE(value, '$.type'),
            CAST(COALESCE(JSON_VALUE(value, '$.strength'), '1.0') AS DECIMAL(3,2)),
            CAST(COALESCE(JSON_VALUE(value, '$.bidirectional'), '1') AS BIT),
            @user_id,
            @user_id
        FROM OPENJSON(@relationships)
        WHERE NOT EXISTS (
            -- Skip if already exists
            SELECT 1 FROM tag_relationships existing
            WHERE existing.from_tag_id = CAST(JSON_VALUE(value, '$.from') AS INT)
            AND existing.to_tag_id = CAST(JSON_VALUE(value, '$.to') AS INT)
            AND existing.relationship_type = JSON_VALUE(value, '$.type')
            AND existing.user_id = @user_id
            AND existing.deleted_at IS NULL
        );
        
        -- Return results
        SELECT 
            COUNT(*) AS total_created,
            (SELECT * FROM @results FOR JSON PATH) AS created_relationships;
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
```

---

## 🧮 Graph Algorithms

### 1. Detect Cycles

```sql
CREATE OR ALTER FUNCTION dbo.fn_has_cycle(
    @from_tag_id INT,
    @to_tag_id INT,
    @relationship_type NVARCHAR(50),
    @user_id INT
)
RETURNS BIT
AS
BEGIN
    DECLARE @has_cycle BIT = 0;
    
    -- Check if adding this relationship would create a cycle
    -- by seeing if there's already a path from @to_tag_id to @from_tag_id
    
    IF EXISTS (
        WITH PathCheck AS (
            SELECT @to_tag_id AS current_tag, 0 AS depth
            
            UNION ALL
            
            SELECT r.to_tag_id, pc.depth + 1
            FROM PathCheck pc
            JOIN tag_relationships r ON r.from_tag_id = pc.current_tag
            WHERE r.relationship_type = @relationship_type
            AND r.user_id = @user_id
            AND r.deleted_at IS NULL
            AND pc.depth < 10  -- Prevent infinite recursion
        )
        SELECT 1 FROM PathCheck WHERE current_tag = @from_tag_id
    )
    BEGIN
        SET @has_cycle = 1;
    END;
    
    RETURN @has_cycle;
END;
GO
```

### 2. Calculate Centrality (Most Connected Tags)

```sql
CREATE OR ALTER PROCEDURE usp_calculate_tag_centrality
    @user_id INT,
    @limit INT = 20
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Degree centrality: number of connections
    WITH DegreeCentrality AS (
        SELECT 
            t.id,
            t.name,
            COUNT(DISTINCT r_out.id) AS outgoing_degree,
            COUNT(DISTINCT r_in.id) AS incoming_degree,
            COUNT(DISTINCT r_out.id) + COUNT(DISTINCT r_in.id) AS total_degree
        FROM tags t
        LEFT JOIN tag_relationships r_out ON r_out.from_tag_id = t.id AND r_out.deleted_at IS NULL
        LEFT JOIN tag_relationships r_in ON r_in.to_tag_id = t.id AND r_in.deleted_at IS NULL
        WHERE t.user_id = @user_id
        AND t.deleted_at IS NULL
        GROUP BY t.id, t.name
    ),
    
    -- Weighted centrality: sum of edge weights
    WeightedCentrality AS (
        SELECT 
            t.id,
            SUM(r_out.strength) AS outgoing_weight,
            SUM(r_in.strength) AS incoming_weight,
            SUM(r_out.strength) + SUM(r_in.strength) AS total_weight
        FROM tags t
        LEFT JOIN tag_relationships r_out ON r_out.from_tag_id = t.id AND r_out.deleted_at IS NULL
        LEFT JOIN tag_relationships r_in ON r_in.to_tag_id = t.id AND r_in.deleted_at IS NULL
        WHERE t.user_id = @user_id
        AND t.deleted_at IS NULL
        GROUP BY t.id
    )
    
    SELECT TOP (@limit)
        dc.id AS tag_id,
        dc.name AS tag_name,
        dc.outgoing_degree,
        dc.incoming_degree,
        dc.total_degree,
        wc.total_weight,
        -- Normalized centrality score (0.0 - 1.0)
        CAST(dc.total_degree AS FLOAT) / NULLIF((SELECT MAX(total_degree) FROM DegreeCentrality), 0) AS normalized_centrality
    FROM DegreeCentrality dc
    JOIN WeightedCentrality wc ON wc.id = dc.id
    ORDER BY dc.total_degree DESC, wc.total_weight DESC;
END;
GO
```

### 3. Find Communities (Clustering)

```sql
CREATE OR ALTER PROCEDURE usp_find_tag_communities
    @user_id INT,
    @min_cluster_size INT = 3,
    @min_connection_strength DECIMAL(3,2) = 0.6
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Simple community detection using connected components
    -- Tags strongly connected to each other form a community
    
    WITH StrongConnections AS (
        SELECT DISTINCT
            r.from_tag_id,
            r.to_tag_id
        FROM tag_relationships r
        WHERE r.user_id = @user_id
        AND r.deleted_at IS NULL
        AND r.strength >= @min_connection_strength
    ),
    
    ConnectedComponents AS (
        SELECT 
            t.id AS tag_id,
            t.id AS component_root,
            0 AS depth
        FROM tags t
        WHERE t.user_id = @user_id
        AND t.deleted_at IS NULL
        
        UNION ALL
        
        SELECT 
            sc.to_tag_id,
            cc.component_root,
            cc.depth + 1
        FROM ConnectedComponents cc
        JOIN StrongConnections sc ON sc.from_tag_id = cc.tag_id
        WHERE cc.depth < 10
    ),
    
    Communities AS (
        SELECT 
            component_root,
            COUNT(DISTINCT tag_id) AS community_size,
            STRING_AGG(t.name, ', ') AS tag_names
        FROM ConnectedComponents cc
        JOIN tags t ON t.id = cc.tag_id
        GROUP BY component_root
        HAVING COUNT(DISTINCT tag_id) >= @min_cluster_size
    )
    
    SELECT 
        ROW_NUMBER() OVER (ORDER BY community_size DESC) AS community_id,
        component_root AS root_tag_id,
        rt.name AS root_tag_name,
        community_size,
        tag_names
    FROM Communities c
    JOIN tags rt ON rt.id = c.component_root
    ORDER BY community_size DESC;
END;
GO
```

---

## 💡 Use Cases & Examples

### Example 1: Programming Language Learning Path

```sql
-- Setup learning path relationships
DECLARE @html INT = 101, @css INT = 102, @js INT = 103, 
        @react INT = 104, @vue INT = 105, @node INT = 106;

-- Prerequisites
EXEC usp_i_tag_relationship @html, @css, 'prerequisite_for', @user_id=1, @strength=1.0;
EXEC usp_i_tag_relationship @css, @js, 'prerequisite_for', @user_id=1, @strength=1.0;
EXEC usp_i_tag_relationship @js, @react, 'prerequisite_for', @user_id=1, @strength=0.9;
EXEC usp_i_tag_relationship @js, @vue, 'prerequisite_for', @user_id=1, @strength=0.9;
EXEC usp_i_tag_relationship @js, @node, 'prerequisite_for', @user_id=1, @strength=0.8;

-- Similar frameworks
EXEC usp_i_tag_relationship @react, @vue, 'similar_to', @user_id=1, @strength=0.7;

-- Query: What should I learn after JavaScript?
EXEC usp_s_tag_relationships 
    @tag_id = @js,
    @user_id = 1,
    @relationship_types = '["prerequisite_for"]',
    @direction = 'outgoing',
    @max_depth = 1;

-- Output: React, Vue, Node.js (all with prerequisite_for relationship)
```

### Example 2: Tool Ecosystems

```sql
-- React ecosystem
DECLARE @react INT = 104, @redux INT = 201, @router INT = 202, 
        @nextjs INT = 203, @styled INT = 204;

-- Tools used together
EXEC usp_i_tag_relationship @react, @redux, 'used_with', @user_id=1, @strength=0.9;
EXEC usp_i_tag_relationship @react, @router, 'used_with', @user_id=1, @strength=0.8;
EXEC usp_i_tag_relationship @react, @nextjs, 'used_with', @user_id=1, @strength=0.7;
EXEC usp_i_tag_relationship @react, @styled, 'used_with', @user_id=1, @strength=0.6;

-- Extends relationships
EXEC usp_i_tag_relationship @react, @nextjs, 'extends', @user_id=1, @strength=0.9;

-- Query: What tools work well with React?
EXEC usp_s_tag_relationships 
    @tag_id = @react,
    @user_id = 1,
    @relationship_types = '["used_with"]',
    @max_depth = 1;

-- Query: Get recommendations based on React usage
EXEC usp_s_recommended_tags 
    @user_id = 1,
    @base_tag_ids = '[104]',  -- React
    @limit = 5;

-- Output: Redux, React Router, Next.js, Styled Components, etc.
```

### Example 3: Concept Mapping

```sql
-- Programming paradigms
DECLARE @oop INT = 301, @functional INT = 302, @reactive INT = 303;

-- Opposite concepts
EXEC usp_i_tag_relationship @oop, @functional, 'opposite_of', @user_id=1, @strength=0.9;

-- Related concepts
EXEC usp_i_tag_relationship @functional, @reactive, 'related_to', @user_id=1, @strength=0.7;

-- Query: Explore concepts around OOP
EXEC usp_s_tag_relationships 
    @tag_id = @oop,
    @user_id = 1,
    @max_depth = 2;

-- Output: Graph showing OOP ↔ Functional ← Reactive
```

### Example 4: Find Learning Path

```sql
-- Find path from HTML to React
EXEC usp_find_tag_path 
    @from_tag_id = 101,  -- HTML
    @to_tag_id = 104,    -- React
    @user_id = 1,
    @max_depth = 5;

-- Output: HTML → CSS → JavaScript → React (depth=3)
```

### Example 5: Smart Tag Suggestions

```sql
-- User is working with: React, TypeScript, Node.js
-- System suggests related technologies

EXEC usp_s_recommended_tags 
    @user_id = 1,
    @base_tag_ids = '[104, 120, 106]',  -- React, TypeScript, Node.js
    @limit = 10,
    @min_strength = 0.5;

-- Output (sorted by relevance):
-- 1. Next.js (used_with React, extends React)
-- 2. GraphQL (used_with Node.js, used_with React)
-- 3. Redux (used_with React)
-- 4. Express (used_with Node.js)
-- 5. MongoDB (used_with Node.js)
-- etc.
```

### Example 6: Detect Most Important Tags

```sql
-- Find hub tags (most connected)
EXEC usp_calculate_tag_centrality 
    @user_id = 1,
    @limit = 10;

-- Output:
-- JavaScript (total_degree=15, centrality=1.0)
-- React (total_degree=12, centrality=0.8)
-- Node.js (total_degree=10, centrality=0.67)
-- etc.
```

### Example 7: Find Tag Communities

```sql
-- Discover clusters of related tags
EXEC usp_find_tag_communities 
    @user_id = 1,
    @min_cluster_size = 3,
    @min_connection_strength = 0.7;

-- Output:
-- Community 1: Frontend Stack (React, Vue, Angular, Redux, TypeScript)
-- Community 2: Backend Stack (Node.js, Express, MongoDB, PostgreSQL)
-- Community 3: DevOps (Docker, Kubernetes, CI/CD, AWS)
```

---

## ⚡ Performance Optimization

### Indexing Strategy

```sql
-- Already created in schema section, but here's why:

-- 1. Graph traversal (most common)
CREATE INDEX ix_tagrel_from ON tag_relationships(from_tag_id, relationship_type, strength DESC);
-- Supports: Finding outgoing edges from a node

-- 2. Reverse traversal
CREATE INDEX ix_tagrel_to ON tag_relationships(to_tag_id, relationship_type, strength DESC);
-- Supports: Finding incoming edges to a node

-- 3. Filtered queries
CREATE INDEX ix_tagrel_type ON tag_relationships(relationship_type, strength DESC);
-- Supports: Finding all relationships of a specific type

-- 4. Composite for complex queries
CREATE INDEX ix_tagrel_graph_traversal
    ON tag_relationships(from_tag_id, to_tag_id, relationship_type, strength)
    INCLUDE (is_bidirectional, description)
    WHERE deleted_at IS NULL;
-- Supports: Multi-hop graph traversal without extra lookups
```

### Query Performance Benchmarks

```sql
-- Test data: 100K tags, 500K relationships

-- Test 1: Get direct relationships (1-hop)
EXEC usp_s_tag_relationships @tag_id=123, @user_id=1, @max_depth=1;
-- Expected: < 10ms (with indexes)

-- Test 2: Graph traversal (2-hop)
EXEC usp_s_tag_relationships @tag_id=123, @user_id=1, @max_depth=2;
-- Expected: 20-50ms

-- Test 3: Find path (max 5 hops)
EXEC usp_find_tag_path @from_tag_id=123, @to_tag_id=456, @user_id=1, @max_depth=5;
-- Expected: 50-200ms (most cases)
-- Worst case (no path): 300-500ms

-- Test 4: Recommendations
EXEC usp_s_recommended_tags @user_id=1, @base_tag_ids='[1,2,3]', @limit=10;
-- Expected: 30-100ms

-- Test 5: Centrality calculation
EXEC usp_calculate_tag_centrality @user_id=1, @limit=20;
-- Expected: 100-300ms (computed on demand)
```

### Caching Strategy

```typescript
// Application-level cache (Redis recommended)

interface CacheConfig {
  tagRelationships: { ttl: 3600 }, // 1 hour
  recommendations: { ttl: 1800 },  // 30 minutes
  graphView: { ttl: 3600 },
  centrality: { ttl: 7200 }        // 2 hours (expensive query)
}

async function getTagRelationships(tagId: number, depth: number, userId: number) {
  const cacheKey = `tag:${tagId}:rel:${depth}:user:${userId}`;
  
  // Try cache first
  const cached = await redis.get(cacheKey);
  if (cached) return JSON.parse(cached);
  
  // Query database
  const result = await db.exec('usp_s_tag_relationships', { tagId, userId, maxDepth: depth });
  
  // Cache for 1 hour
  await redis.setex(cacheKey, 3600, JSON.stringify(result));
  
  return result;
}

// Invalidate cache on relationship changes
async function createRelationship(data: RelationshipData) {
  await db.exec('usp_i_tag_relationship', data);
  
  // Invalidate affected caches
  await redis.del(
    `tag:${data.fromTagId}:rel:*`,
    `tag:${data.toTagId}:rel:*`,
    `recommendations:user:${data.userId}:*`
  );
}
```

### Materialized Views for Analytics

```sql
-- Pre-computed statistics (refresh periodically)
CREATE VIEW vw_tag_graph_analytics AS
SELECT 
    -- Overall stats
    COUNT(DISTINCT r.id) AS total_relationships,
    COUNT(DISTINCT r.from_tag_id) AS tags_with_outgoing,
    COUNT(DISTINCT r.to_tag_id) AS tags_with_incoming,
    
    -- By type
    COUNT(CASE WHEN r.relationship_type = 'related_to' THEN 1 END) AS related_count,
    COUNT(CASE WHEN r.relationship_type = 'prerequisite_for' THEN 1 END) AS prereq_count,
    COUNT(CASE WHEN r.relationship_type = 'used_with' THEN 1 END) AS usedwith_count,
    
    -- Strength distribution
    AVG(r.strength) AS avg_strength,
    MIN(r.strength) AS min_strength,
    MAX(r.strength) AS max_strength,
    
    -- User activity
    COUNT(DISTINCT r.user_id) AS active_users,
    AVG(CAST(DATEDIFF(DAY, r.created_at, GETUTCDATE()) AS FLOAT)) AS avg_age_days
    
FROM tag_relationships r
WHERE r.deleted_at IS NULL;
GO

-- Refresh with SQL Server Agent job (daily)
-- Or use indexed view for automatic updates
```

---

## 🎨 UI/UX Integration

### Frontend Data Models

```typescript
// TypeScript interfaces for frontend

interface TagNode {
  id: number;
  name: string;
  color: string;
  path: string;
  slug: string;
  // Graph position (D3.js)
  x?: number;
  y?: number;
  vx?: number;
  vy?: number;
  // Statistics
  totalConnections: number;
  depth: number;
}

interface TagEdge {
  id: number;
  source: number;  // from_tag_id
  target: number;  // to_tag_id
  type: string;    // relationship_type
  display: string; // relationship_display
  icon: string;
  color: string;
  lineStyle: 'solid' | 'dashed' | 'dotted';
  strength: number;
  isBidirectional: boolean;
  description?: string;
}

interface TagGraph {
  nodes: TagNode[];
  edges: TagEdge[];
  rootNode: TagNode;
  maxDepth: number;
}
```

### Graph Visualization Component (React)

```typescript
import React, { useEffect, useRef } from 'react';
import * as d3 from 'd3';

interface GraphViewProps {
  tagId: number;
  depth?: number;
  onNodeClick?: (node: TagNode) => void;
}

export function TagGraphView({ tagId, depth = 2, onNodeClick }: GraphViewProps) {
  const svgRef = useRef<SVGSVGElement>(null);
  const [graphData, setGraphData] = useState<TagGraph | null>(null);

  useEffect(() => {
    // Fetch graph data
    fetch(`/api/tags/${tagId}/relationships?depth=${depth}`)
      .then(res => res.json())
      .then(data => setGraphData(data));
  }, [tagId, depth]);

  useEffect(() => {
    if (!graphData || !svgRef.current) return;

    const width = 800;
    const height = 600;

    // D3 force simulation
    const simulation = d3.forceSimulation(graphData.nodes)
      .force('link', d3.forceLink(graphData.edges).id(d => d.id).distance(100))
      .force('charge', d3.forceManyBody().strength(-300))
      .force('center', d3.forceCenter(width / 2, height / 2))
      .force('collision', d3.forceCollide().radius(30));

    const svg = d3.select(svgRef.current);
    svg.selectAll('*').remove();

    // Draw edges
    const links = svg.append('g')
      .selectAll('line')
      .data(graphData.edges)
      .join('line')
      .attr('stroke', d => d.color)
      .attr('stroke-width', d => d.strength * 3)
      .attr('stroke-dasharray', d => {
        if (d.lineStyle === 'dashed') return '5,5';
        if (d.lineStyle === 'dotted') return '2,2';
        return '';
      });

    // Draw nodes
    const nodes = svg.append('g')
      .selectAll('circle')
      .data(graphData.nodes)
      .join('circle')
      .attr('r', d => 10 + d.totalConnections * 2)
      .attr('fill', d => d.color)
      .attr('stroke', '#fff')
      .attr('stroke-width', 2)
      .on('click', (event, d) => onNodeClick?.(d))
      .call(drag(simulation));

    // Labels
    const labels = svg.append('g')
      .selectAll('text')
      .data(graphData.nodes)
      .join('text')
      .text(d => d.name)
      .attr('font-size', 12)
      .attr('dx', 15)
      .attr('dy', 5);

    // Update positions on tick
    simulation.on('tick', () => {
      links
        .attr('x1', d => d.source.x)
        .attr('y1', d => d.source.y)
        .attr('x2', d => d.target.x)
        .attr('y2', d => d.target.y);

      nodes
        .attr('cx', d => d.x)
        .attr('cy', d => d.y);

      labels
        .attr('x', d => d.x)
        .attr('y', d => d.y);
    });

    // Drag behavior
    function drag(simulation) {
      function dragstarted(event) {
        if (!event.active) simulation.alphaTarget(0.3).restart();
        event.subject.fx = event.subject.x;
        event.subject.fy = event.subject.y;
      }

      function dragged(event) {
        event.subject.fx = event.x;
        event.subject.fy = event.y;
      }

      function dragended(event) {
        if (!event.active) simulation.alphaTarget(0);
        event.subject.fx = null;
        event.subject.fy = null;
      }

      return d3.drag()
        .on('start', dragstarted)
        .on('drag', dragged)
        .on('end', dragended);
    }

  }, [graphData, onNodeClick]);

  return (
    <div className="graph-view">
      <svg ref={svgRef} width={800} height={600} />
    </div>
  );
}
```

### Relationship Panel Component

```typescript
export function RelationshipPanel({ tagId }: { tagId: number }) {
  const [relationships, setRelationships] = useState([]);

  return (
    <div className="relationship-panel">
      <h3>Tag Relationships</h3>
      
      {/* Group by relationship type */}
      <div className="rel-section">
        <h4>📚 Prerequisites For:</h4>
        {relationships
          .filter(r => r.type === 'prerequisite_for')
          .map(r => (
            <div key={r.id} className="rel-item">
              <span className="tag-name">{r.toTagName}</span>
              <span className="strength">({r.strength.toFixed(1)})</span>
              <button onClick={() => editRelationship(r.id)}>Edit</button>
              <button onClick={() => deleteRelationship(r.id)}>×</button>
            </div>
          ))}
      </div>

      <div className="rel-section">
        <h4>🔗 Related To:</h4>
        {relationships
          .filter(r => r.type === 'related_to')
          .map(r => (
            <div key={r.id} className="rel-item">
              <span className="tag-name">{r.toTagName}</span>
              <span className="strength">({r.strength.toFixed(1)})</span>
            </div>
          ))}
      </div>

      <button className="add-rel-btn" onClick={openAddModal}>
        + Add Relationship
      </button>
    </div>
  );
}
```

### Smart Suggestions Widget

```typescript
export function SmartSuggestions({ currentTags }: { currentTags: number[] }) {
  const [suggestions, setSuggestions] = useState([]);

  useEffect(() => {
    // Fetch recommendations
    fetch('/api/tags/recommendations', {
      method: 'POST',
      body: JSON.stringify({ tagIds: currentTags, limit: 5 })
    })
      .then(res => res.json())
      .then(data => setSuggestions(data));
  }, [currentTags]);

  return (
    <div className="smart-suggestions">
      <h4>💡 You might also like:</h4>
      {suggestions.map(tag => (
        <div key={tag.id} className="suggestion">
          <span className="tag-badge" style={{ background: tag.color }}>
            {tag.name}
          </span>
          <span className="relevance">
            {(tag.relevanceScore * 100).toFixed(0)}% relevant
          </span>
          <span className="reason">
            ({tag.relationshipTypes})
          </span>
          <button onClick={() => addTag(tag.id)}>+ Add</button>
        </div>
      ))}
    </div>
  );
}
```

---

## 🚀 Migration Guide

### Phase 1: Schema Setup (Week 1)

```sql
-- Step 1: Create tables
-- Run all CREATE TABLE statements from Schema Design section

-- Step 2: Create indexes
-- Already included in table creation scripts

-- Step 3: Seed relationship types
-- Already included in INSERT statements

-- Step 4: Verify
SELECT 
    OBJECT_NAME(object_id) AS table_name,
    name AS index_name,
    type_desc
FROM sys.indexes
WHERE OBJECT_NAME(object_id) LIKE 'tag_relationship%'
ORDER BY table_name, index_name;
```

### Phase 2: Migrate Existing Data (Week 2)

```sql
-- Optional: Convert some hierarchy relationships to graph relationships
-- Example: Sibling tags under same parent → similar_to

INSERT INTO tag_relationships (
    from_tag_id, to_tag_id, relationship_type, strength, 
    is_bidirectional, user_id, created_by
)
SELECT 
    t1.id AS from_tag_id,
    t2.id AS to_tag_id,
    'similar_to' AS relationship_type,
    0.6 AS strength,
    1 AS is_bidirectional,
    t1.user_id,
    t1.user_id
FROM tags t1
JOIN tags t2 ON t2.parent_id = t1.parent_id 
    AND t2.id > t1.id  -- Only one direction (will auto-create reverse)
WHERE t1.parent_id IS NOT NULL
AND t1.deleted_at IS NULL
AND t2.deleted_at IS NULL;

-- Verify
SELECT 
    COUNT(*) AS migrated_relationships,
    relationship_type,
    COUNT(DISTINCT user_id) AS affected_users
FROM tag_relationships
WHERE created_at >= DATEADD(MINUTE, -5, GETUTCDATE())
GROUP BY relationship_type;
```

### Phase 3: API Development (Week 3-4)

```typescript
// REST API endpoints

// 1. CRUD operations
POST   /api/tags/relationships
GET    /api/tags/:id/relationships
PUT    /api/tags/relationships/:id
DELETE /api/tags/relationships/:id

// 2. Graph queries
GET    /api/tags/:id/graph?depth=2&types=["related_to"]
GET    /api/tags/path/:fromId/:toId

// 3. Recommendations
POST   /api/tags/recommendations
       Body: { tagIds: [1,2,3], limit: 10 }

// 4. Analytics
GET    /api/tags/centrality?limit=20
GET    /api/tags/communities

// Example implementation (Express + TypeScript)
app.post('/api/tags/relationships', async (req, res) => {
  const { fromTagId, toTagId, type, strength } = req.body;
  const userId = req.user.id;

  const result = await db.exec('usp_i_tag_relationship', {
    from_tag_id: fromTagId,
    to_tag_id: toTagId,
    relationship_type: type,
    user_id: userId,
    strength: strength
  });

  res.json(result);
});

app.get('/api/tags/:id/relationships', async (req, res) => {
  const { id } = req.params;
  const { depth = 1, types, direction = 'both' } = req.query;
  const userId = req.user.id;

  const result = await db.exec('usp_s_tag_relationships', {
    tag_id: id,
    user_id: userId,
    relationship_types: types ? JSON.stringify(types.split(',')) : null,
    direction: direction,
    max_depth: depth
  });

  res.json(result);
});
```

### Phase 4: UI Integration (Week 5-6)

```typescript
// 1. Graph visualization page
/tags/:id/graph

// 2. Relationship editor modal
<RelationshipEditor tagId={tagId} />

// 3. Smart suggestions widget (sidebar)
<SmartSuggestions currentTags={selectedTags} />

// 4. Search with relationship filters
<TagSearch filters={{ relationshipType: 'prerequisite_for' }} />

// 5. Learning path visualizer
<LearningPath startTag={htmlTag} endTag={reactTag} />
```

### Phase 5: Testing & Optimization (Week 7-8)

```sql
-- Load testing
-- Generate test data
DECLARE @i INT = 1;
WHILE @i <= 10000
BEGIN
    -- Random relationships
    INSERT INTO tag_relationships (from_tag_id, to_tag_id, relationship_type, strength, user_id, created_by)
    SELECT TOP 1
        t1.id,
        t2.id,
        'related_to',
        RAND(),
        t1.user_id,
        t1.user_id
    FROM tags t1
    CROSS JOIN tags t2
    WHERE t1.id != t2.id
    AND t1.user_id = 1
    ORDER BY NEWID();
    
    SET @i = @i + 1;
END;

-- Benchmark queries
SET STATISTICS TIME ON;
SET STATISTICS IO ON;

EXEC usp_s_tag_relationships @tag_id=123, @user_id=1, @max_depth=2;
EXEC usp_find_tag_path @from_tag_id=100, @to_tag_id=200, @user_id=1;
EXEC usp_s_recommended_tags @user_id=1, @base_tag_ids='[1,2,3]';

SET STATISTICS TIME OFF;
SET STATISTICS IO OFF;
```

---

## 🎓 Advanced Features

### 1. Relationship Suggestions (ML-based)

```sql
-- Track relationship co-occurrence
CREATE TABLE tag_relationship_patterns (
    pattern_id BIGINT IDENTITY(1,1) PRIMARY KEY,
    
    tag_a_id INT NOT NULL,
    tag_b_id INT NOT NULL,
    relationship_type NVARCHAR(50) NOT NULL,
    
    -- ML features
    co_occurrence_count INT DEFAULT 1,
    confidence_score DECIMAL(4,3),  -- 0.000 to 1.000
    
    -- Context
    user_count INT DEFAULT 1,  -- How many users have this pattern
    last_seen DATETIME2 DEFAULT GETUTCDATE(),
    
    CONSTRAINT fk_pattern_tag_a FOREIGN KEY (tag_a_id) REFERENCES tags(id),
    CONSTRAINT fk_pattern_tag_b FOREIGN KEY (tag_b_id) REFERENCES tags(id)
);

-- Suggest relationships based on patterns
CREATE PROCEDURE usp_suggest_relationships
    @tag_id INT,
    @user_id INT,
    @limit INT = 5
AS
BEGIN
    -- Find tags that frequently have relationships with similar tags
    SELECT TOP (@limit)
        p.tag_b_id AS suggested_tag_id,
        t.name AS suggested_tag_name,
        p.relationship_type AS suggested_type,
        p.confidence_score,
        p.co_occurrence_count,
        'Based on ' + CAST(p.user_count AS NVARCHAR) + ' users' AS reason
    FROM tag_relationship_patterns p
    JOIN tags t ON t.id = p.tag_b_id
    WHERE p.tag_a_id IN (
        -- Tags similar to current tag
        SELECT to_tag_id FROM tag_relationships
        WHERE from_tag_id = @tag_id
        AND relationship_type = 'similar_to'
    )
    AND NOT EXISTS (
        -- Don't suggest existing relationships
        SELECT 1 FROM tag_relationships
        WHERE from_tag_id = @tag_id
        AND to_tag_id = p.tag_b_id
    )
    ORDER BY p.confidence_score DESC, p.co_occurrence_count DESC;
END;
GO
```

### 2. Temporal Relationships

```sql
-- Track relationship evolution over time
ALTER TABLE tag_relationships
ADD 
    valid_from DATETIME2 DEFAULT GETUTCDATE(),
    valid_until DATETIME2 NULL;

-- Example: "Python 2 replaced by Python 3"
INSERT INTO tag_relationships (from_tag_id, to_tag_id, relationship_type, valid_from, valid_until)
VALUES 
    (python2_id, python3_id, 'replaces', '2008-12-03', NULL),
    (python2_id, python3_id, 'similar_to', '2008-12-03', '2020-01-01');  -- End of life

-- Query: What was the state at a specific time?
CREATE PROCEDURE usp_s_relationships_at_time
    @tag_id INT,
    @as_of_date DATETIME2,
    @user_id INT
AS
BEGIN
    SELECT *
    FROM tag_relationships r
    WHERE r.from_tag_id = @tag_id
    AND r.user_id = @user_id
    AND r.valid_from <= @as_of_date
    AND (r.valid_until IS NULL OR r.valid_until > @as_of_date)
    AND r.deleted_at IS NULL;
END;
GO
```

### 3. Weighted Graph Algorithms

```sql
-- PageRank-style importance score
CREATE PROCEDURE usp_calculate_tag_importance
    @user_id INT,
    @iterations INT = 10,
    @damping_factor DECIMAL(3,2) = 0.85
AS
BEGIN
    -- Create temp table for scores
    CREATE TABLE #Scores (
        tag_id INT PRIMARY KEY,
        score FLOAT DEFAULT 1.0,
        new_score FLOAT
    );
    
    -- Initialize all tags with score 1.0
    INSERT INTO #Scores (tag_id, score)
    SELECT id, 1.0
    FROM tags
    WHERE user_id = @user_id AND deleted_at IS NULL;
    
    -- Iterate to converge
    DECLARE @i INT = 0;
    WHILE @i < @iterations
    BEGIN
        -- Calculate new scores
        UPDATE s
        SET s.new_score = (1.0 - @damping_factor) + @damping_factor * ISNULL(incoming_sum, 0.0)
        FROM #Scores s
        LEFT JOIN (
            SELECT 
                r.to_tag_id,
                SUM(s2.score * r.strength / NULLIF(outgoing_count, 0)) AS incoming_sum
            FROM tag_relationships r
            JOIN #Scores s2 ON s2.tag_id = r.from_tag_id
            LEFT JOIN (
                -- Count outgoing edges per node
                SELECT from_tag_id, COUNT(*) AS outgoing_count
                FROM tag_relationships
                WHERE deleted_at IS NULL
                GROUP BY from_tag_id
            ) out_count ON out_count.from_tag_id = r.from_tag_id
            WHERE r.deleted_at IS NULL
            GROUP BY r.to_tag_id
        ) incoming ON incoming.to_tag_id = s.tag_id;
        
        -- Update scores
        UPDATE #Scores SET score = new_score;
        
        SET @i = @i + 1;
    END;
    
    -- Return ranked tags
    SELECT 
        t.id,
        t.name,
        t.path,
        s.score AS importance_score,
        ROW_NUMBER() OVER (ORDER BY s.score DESC) AS rank
    FROM #Scores s
    JOIN tags t ON t.id = s.tag_id
    ORDER BY s.score DESC;
    
    DROP TABLE #Scores;
END;
GO
```

### 4. Graph Diff/Versioning

```sql
-- Track graph state over time
CREATE TABLE tag_graph_snapshots (
    snapshot_id BIGINT IDENTITY(1,1) PRIMARY KEY,
    user_id INT NOT NULL,
    snapshot_date DATETIME2 DEFAULT GETUTCDATE(),
    
    -- Serialized graph state (JSON)
    nodes_snapshot NVARCHAR(MAX),  -- All tags
    edges_snapshot NVARCHAR(MAX),  -- All relationships
    
    -- Metadata
    total_tags INT,
    total_relationships INT,
    
    CONSTRAINT fk_snapshot_user FOREIGN KEY (user_id) REFERENCES users(id)
);

-- Create snapshot
CREATE PROCEDURE usp_create_graph_snapshot
    @user_id INT
AS
BEGIN
    DECLARE @nodes NVARCHAR(MAX), @edges NVARCHAR(MAX);
    
    -- Serialize tags
    SELECT @nodes = (
        SELECT id, name, color, path, parent_id
        FROM tags
        WHERE user_id = @user_id AND deleted_at IS NULL
        FOR JSON PATH
    );
    
    -- Serialize relationships
    SELECT @edges = (
        SELECT id, from_tag_id, to_tag_id, relationship_type, strength
        FROM tag_relationships
        WHERE user_id = @user_id AND deleted_at IS NULL
        FOR JSON PATH
    );
    
    INSERT INTO tag_graph_snapshots (
        user_id, nodes_snapshot, edges_snapshot, 
        total_tags, total_relationships
    )
    VALUES (
        @user_id, @nodes, @edges,
        JSON_VALUE(@nodes, '$.length'),
        JSON_VALUE(@edges, '$.length')
    );
END;
GO

-- Compare two snapshots (diff)
CREATE PROCEDURE usp_compare_graph_snapshots
    @snapshot_id_1 BIGINT,
    @snapshot_id_2 BIGINT
AS
BEGIN
    -- Show added/removed tags and relationships
    -- (Implementation left as exercise - parse JSON and compare)
    SELECT 'Snapshot comparison feature' AS message;
END;
GO
```

---

## 📊 Monitoring & Analytics

### Dashboard Queries

```sql
-- Overall graph health
SELECT 
    'Total Relationships' AS metric,
    COUNT(*) AS value,
    'relationships' AS unit
FROM tag_relationships
WHERE deleted_at IS NULL

UNION ALL

SELECT 
    'Avg Relationships per Tag',
    AVG(rel_count),
    'relationships/tag'
FROM (
    SELECT COUNT(*) AS rel_count
    FROM tag_relationships
    WHERE deleted_at IS NULL
    GROUP BY from_tag_id
) sub

UNION ALL

SELECT 
    'Most Common Type',
    relationship_type,
    'type'
FROM (
    SELECT TOP 1 relationship_type, COUNT(*) AS cnt
    FROM tag_relationships
    WHERE deleted_at IS NULL
    GROUP BY relationship_type
    ORDER BY COUNT(*) DESC
) top_type

UNION ALL

SELECT 
    'Avg Strength',
    AVG(strength),
    'score'
FROM tag_relationships
WHERE deleted_at IS NULL;
```

### User Activity Report

```sql
CREATE PROCEDURE usp_graph_activity_report
    @user_id INT = NULL,  -- NULL = all users
    @date_from DATETIME2 = NULL,
    @date_to DATETIME2 = NULL
AS
BEGIN
    SET @date_from = COALESCE(@date_from, DATEADD(DAY, -30, GETUTCDATE()));
    SET @date_to = COALESCE(@date_to, GETUTCDATE());
    
    SELECT 
        u.id AS user_id,
        u.email,
        COUNT(DISTINCT r.id) AS relationships_created,
        COUNT(DISTINCT r.relationship_type) AS unique_types_used,
        AVG(r.strength) AS avg_strength,
        MIN(r.created_at) AS first_relationship,
        MAX(r.created_at) AS last_relationship
    FROM users u
    LEFT JOIN tag_relationships r ON r.user_id = u.id
        AND r.created_at BETWEEN @date_from AND @date_to
        AND r.deleted_at IS NULL
    WHERE (@user_id IS NULL OR u.id = @user_id)
    GROUP BY u.id, u.email
    ORDER BY relationships_created DESC;
END;
GO
```

---

## ✅ Testing Checklist

### Unit Tests

```sql
-- Test 1: Create basic relationship
BEGIN TRANSACTION;
EXEC usp_i_tag_relationship 
    @from_tag_id=1, @to_tag_id=2, @relationship_type='related_to', @user_id=1;
-- Expected: Success, reverse created (bidirectional)
ROLLBACK;

-- Test 2: Prevent self-relationship
BEGIN TRANSACTION;
BEGIN TRY
    EXEC usp_i_tag_relationship 
        @from_tag_id=1, @to_tag_id=1, @relationship_type='related_to', @user_id=1;
    SELECT 'FAIL: Self-relationship allowed' AS result;
END TRY
BEGIN CATCH
    SELECT 'PASS: Self-relationship blocked' AS result;
END CATCH;
ROLLBACK;

-- Test 3: Cycle detection
-- (Add tests for fn_has_cycle function)

-- Test 4: Path finding
BEGIN TRANSACTION;
-- Setup: A → B → C
EXEC usp_i_tag_relationship @from_tag_id=1, @to_tag_id=2, @relationship_type='prerequisite_for', @user_id=1;
EXEC usp_i_tag_relationship @from_tag_id=2, @to_tag_id=3, @relationship_type='prerequisite_for', @user_id=1;
-- Query: Find path A → C
EXEC usp_find_tag_path @from_tag_id=1, @to_tag_id=3, @user_id=1;
-- Expected: Path found with depth=2
ROLLBACK;

-- Test 5: Recommendations
BEGIN TRANSACTION;
-- Setup: User has tags A, B
-- A related_to C, B related_to C
-- Query recommendations
EXEC usp_s_recommended_tags @user_id=1, @base_tag_ids='[1,2]', @limit=5;
-- Expected: C appears in recommendations
ROLLBACK;
```

### Integration Tests

```typescript
// Test API endpoints
describe('Tag Relationships API', () => {
  it('should create relationship', async () => {
    const res = await request(app)
      .post('/api/tags/relationships')
      .send({
        fromTagId: 1,
        toTagId: 2,
        type: 'related_to',
        strength: 0.8
      });
    
    expect(res.status).toBe(200);
    expect(res.body.id).toBeDefined();
  });

  it('should get graph view', async () => {
    const res = await request(app)
      .get('/api/tags/1/graph?depth=2');
    
    expect(res.status).toBe(200);
    expect(res.body.nodes).toBeInstanceOf(Array);
    expect(res.body.edges).toBeInstanceOf(Array);
  });

  it('should find path', async () => {
    const res = await request(app)
      .get('/api/tags/path/1/5');
    
    expect(res.status).toBe(200);
    expect(res.body.status).toBe('FOUND');
    expect(res.body.depth).toBeGreaterThan(0);
  });
});
```

### Performance Tests

```sql
-- Load test: 100K tags, 500K relationships
-- Run benchmark queries and measure

DECLARE @start_time DATETIME2 = GETUTCDATE();

EXEC usp_s_tag_relationships @tag_id=123, @user_id=1, @max_depth=2;

SELECT DATEDIFF(MILLISECOND, @start_time, GETUTCDATE()) AS duration_ms;
-- Expected: < 50ms

-- Stress test: Multiple concurrent requests
-- (Use application testing tool like JMeter)
```

---

## 📚 Related Documentation

- [01-Architecture.md](01-Architecture.md) - System architecture
- [02-Schema.md](02-Schema.md) - Complete database schema
- [03-Triggers.md](03-Triggers.md) - Trigger implementations
- [04-Procedures.md](04-Procedures.md) - All stored procedures
- [05-Functions.md](05-Functions.md) - User-defined functions
- [06-Sharing.md](06-Sharing.md) - Tag sharing system
- [08-Quick-Reference.md](08-Quick-Reference.md) - Quick reference guide

---

## 🎓 Summary & Conclusion

### ✅ Khả năng triển khai: HIGHLY FEASIBLE

**Lý do:**
1. ✅ **Tương thích hoàn toàn** với closure table hiện có
2. ✅ **Bổ sung semantic value** mà hierarchy không có
3. ✅ **SQL Server performance** đủ tốt với proper indexes
4. ✅ **Flexible schema** dễ mở rộng
5. ✅ **Real-world value** cho users (learning paths, recommendations)

### 🏗️ Recommended Implementation Path

1. **Phase 1 (Week 1):** Schema setup + seed data
2. **Phase 2 (Week 2):** Stored procedures + functions
3. **Phase 3 (Week 3-4):** API endpoints + caching
4. **Phase 4 (Week 5-6):** UI components + graph visualization
5. **Phase 5 (Week 7-8):** Testing + optimization

### 🎯 Key Benefits

- **For users:** Smart recommendations, learning paths, discovery
- **For system:** Rich semantic layer, better search, analytics
- **For business:** Increased engagement, network effects, viral growth

### ⚠️ Important Considerations

1. **Performance:** Monitor query times, use caching aggressively
2. **UI/UX:** Graph visualization must be intuitive, not overwhelming
3. **Data quality:** Encourage high-quality relationships (avoid noise)
4. **Maintenance:** Periodic cleanup of weak/unused relationships

---

**END OF DOCUMENT**

Total implementation time: **6-8 weeks** (1 developer)
Complexity: **Medium to High**
Business value: **Very High** ⭐⭐⭐⭐⭐
