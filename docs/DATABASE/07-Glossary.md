# 07 - Tag Templates System

Tài liệu này mô tả chi tiết hệ thống tag templates, cho phép users tạo, chia sẻ và áp dụng các tag structures có sẵn.

---

## 📋 Table of Contents

1. [Overview](#overview)
2. [Schema Design](#schema-design)
3. [Template Types](#template-types)
4. [Stored Procedures](#stored-procedures)
5. [Use Cases](#use-cases)
6. [Best Practices](#best-practices)
7. [Examples](#examples)

---

## 🎯 Overview

### What are Tag Templates?

Tag templates là pre-defined tag hierarchies có thể được clone và áp dụng nhanh chóng cho users mới hoặc projects mới.

### Features
- ✅ Create reusable tag structures
- ✅ Public/Private templates
- ✅ Clone entire tag hierarchies
- ✅ Template marketplace
- ✅ Category organization
- ✅ Usage tracking
- ✅ Rating system

### Benefits
1. **Onboarding mới nhanh hơn** - New users có sẵn tag structure
2. **Consistency** - Teams sử dụng cùng tag conventions
3. **Best practices** - Share proven tag structures
4. **Time saving** - Không cần tạo từ đầu

### Use Cases
```
1. "GTD Productivity" template - 20 tags cho Getting Things Done
2. "Project Management" template - Status tags, priority tags
3. "Personal Finance" template - Income, Expenses, Savings categories
4. "Recipe Organization" template - Cuisine types, meal types, dietary
5. "Software Development" template - Bug tracking, feature requests
```

---

## 📐 Schema Design

### Main Tables

```sql
-- ============================================
-- TAG TEMPLATES: Reusable tag structures
-- ============================================
CREATE TABLE tag_templates (
    id INT IDENTITY(1,1) PRIMARY KEY,
    
    -- Metadata
    name NVARCHAR(255) NOT NULL,
    slug NVARCHAR(255) NOT NULL,  -- URL-friendly name
    description NVARCHAR(MAX),
    category NVARCHAR(100),        -- 'Productivity', 'Finance', 'Project'
    
    -- Ownership
    creator_id INT NOT NULL,
    is_public BIT DEFAULT 0,       -- Public in marketplace?
    is_official BIT DEFAULT 0,     -- Official Anthropic template?
    
    -- Template structure (JSON)
    structure NVARCHAR(MAX) NOT NULL,  -- JSON array of tags
    -- Example: [
    --   {"name": "Work", "color": "#FF0000", "children": [
    --     {"name": "ProjectX", "color": "#00FF00"}
    --   ]},
    --   {"name": "Personal", "color": "#0000FF"}
    -- ]
    
    -- Usage stats
    clone_count INT DEFAULT 0,
    view_count INT DEFAULT 0,
    rating_avg DECIMAL(3,2) DEFAULT 0.0,  -- 0.0 to 5.0
    rating_count INT DEFAULT 0,
    
    -- Lifecycle
    version INT DEFAULT 1,
    is_active BIT DEFAULT 1,
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    updated_at DATETIME2 DEFAULT GETUTCDATE(),
    deleted_at DATETIME2 NULL,
    
    CONSTRAINT fk_template_creator FOREIGN KEY (creator_id) REFERENCES users(id),
    CONSTRAINT uq_template_slug UNIQUE (slug),
    CONSTRAINT ck_template_rating CHECK (rating_avg BETWEEN 0 AND 5)
);

CREATE INDEX ix_templates_public ON tag_templates(is_public, is_active)
    WHERE is_public = 1 AND is_active = 1;
CREATE INDEX ix_templates_category ON tag_templates(category, is_public)
    WHERE is_public = 1;
CREATE INDEX ix_templates_creator ON tag_templates(creator_id, deleted_at);
CREATE INDEX ix_templates_rating ON tag_templates(rating_avg DESC, clone_count DESC)
    WHERE is_public = 1 AND is_active = 1;

-- ============================================
-- TEMPLATE APPLICATIONS: Track who cloned what
-- ============================================
CREATE TABLE tag_template_applications (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    template_id INT NOT NULL,
    user_id INT NOT NULL,
    
    -- Mapping: template tags -> user's cloned tags
    tag_mapping NVARCHAR(MAX),  -- JSON: {"template_tag_id": "user_tag_id"}
    
    applied_at DATETIME2 DEFAULT GETUTCDATE(),
    
    CONSTRAINT fk_application_template FOREIGN KEY (template_id) REFERENCES tag_templates(id),
    CONSTRAINT fk_application_user FOREIGN KEY (user_id) REFERENCES users(id)
);

CREATE INDEX ix_applications_user ON tag_template_applications(user_id, applied_at);
CREATE INDEX ix_applications_template ON tag_template_applications(template_id, applied_at);

-- ============================================
-- TEMPLATE RATINGS: User feedback
-- ============================================
CREATE TABLE tag_template_ratings (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    template_id INT NOT NULL,
    user_id INT NOT NULL,
    
    rating TINYINT NOT NULL,  -- 1-5 stars
    review NVARCHAR(1000),
    
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    updated_at DATETIME2 DEFAULT GETUTCDATE(),
    
    CONSTRAINT fk_rating_template FOREIGN KEY (template_id) REFERENCES tag_templates(id) ON DELETE CASCADE,
    CONSTRAINT fk_rating_user FOREIGN KEY (user_id) REFERENCES users(id),
    CONSTRAINT uq_template_rating UNIQUE (template_id, user_id),
    CONSTRAINT ck_rating_value CHECK (rating BETWEEN 1 AND 5)
);

CREATE INDEX ix_ratings_template ON tag_template_ratings(template_id, rating);

-- ============================================
-- TEMPLATE CATEGORIES: Organize templates
-- ============================================
CREATE TABLE template_categories (
    id INT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(100) NOT NULL UNIQUE,
    slug NVARCHAR(100) NOT NULL UNIQUE,
    description NVARCHAR(500),
    icon NVARCHAR(50),  -- Icon name or emoji
    sort_order INT DEFAULT 0,
    is_active BIT DEFAULT 1
);

-- Seed common categories
INSERT INTO template_categories (name, slug, description, icon, sort_order) VALUES
('Productivity', 'productivity', 'GTD, task management, goal tracking', '⚡', 1),
('Project Management', 'project-management', 'Agile, Kanban, status tracking', '📊', 2),
('Personal Finance', 'personal-finance', 'Budgeting, expenses, investments', '💰', 3),
('Education', 'education', 'Study notes, courses, research', '📚', 4),
('Health & Fitness', 'health-fitness', 'Workouts, meal planning, habits', '💪', 5),
('Creative Work', 'creative-work', 'Design, writing, media projects', '🎨', 6),
('Home & Family', 'home-family', 'Chores, recipes, events', '🏠', 7),
('Software Development', 'software-dev', 'Bug tracking, features, releases', '💻', 8);
```

---

## 📑 Template Types

### 1. **Personal Templates**
- Created by individual users
- Private by default
- Can be made public later

```json
{
  "name": "My GTD System",
  "is_public": false,
  "structure": [...]
}
```

### 2. **Public Templates**
- Shared with all users
- Visible in marketplace
- Can be rated and reviewed

```json
{
  "name": "GTD Productivity",
  "is_public": true,
  "category": "Productivity",
  "structure": [...]
}
```

### 3. **Official Templates**
- Curated by platform admins
- Verified and high-quality
- Featured in marketplace

```json
{
  "name": "Project Management",
  "is_public": true,
  "is_official": true,
  "structure": [...]
}
```

---

## 🛠️ Stored Procedures

### 1. Create Tag Template

```sql
CREATE OR ALTER PROCEDURE sp_create_tag_template
    @creator_id INT,
    @name NVARCHAR(255),
    @slug NVARCHAR(255),
    @description NVARCHAR(MAX) = NULL,
    @category NVARCHAR(100) = NULL,
    @structure NVARCHAR(MAX),  -- JSON
    @is_public BIT = 0,
    @is_official BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Validate: User exists
        IF NOT EXISTS (SELECT 1 FROM users WHERE id = @creator_id)
        BEGIN
            RAISERROR('User not found', 16, 1);
            RETURN;
        END;
        
        -- Validate: Slug is unique
        IF EXISTS (SELECT 1 FROM tag_templates WHERE slug = @slug AND deleted_at IS NULL)
        BEGIN
            RAISERROR('Template slug already exists', 16, 1);
            RETURN;
        END;
        
        -- Validate: Structure is valid JSON
        IF ISJSON(@structure) = 0
        BEGIN
            RAISERROR('Invalid JSON structure', 16, 1);
            RETURN;
        END;
        
        -- Validate: Category exists (if provided)
        IF @category IS NOT NULL AND NOT EXISTS (
            SELECT 1 FROM template_categories WHERE slug = @category
        )
        BEGIN
            RAISERROR('Invalid category', 16, 1);
            RETURN;
        END;
        
        -- Create template
        INSERT INTO tag_templates (
            name, slug, description, category, creator_id,
            is_public, is_official, structure
        )
        VALUES (
            @name, @slug, @description, @category, @creator_id,
            @is_public, @is_official, @structure
        );
        
        DECLARE @template_id INT = SCOPE_IDENTITY();
        
        SELECT 
            @template_id AS template_id,
            @slug AS slug,
            'CREATED' AS status;
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
```

### 2. Apply Tag Template

```sql
CREATE OR ALTER PROCEDURE sp_apply_tag_template
    @template_id INT,
    @user_id INT,
    @parent_tag_id INT = NULL  -- Optional: Clone under existing tag
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Validate: Template exists and accessible
        DECLARE @structure NVARCHAR(MAX), @is_public BIT, @creator_id INT;
        
        SELECT @structure = structure, @is_public = is_public, @creator_id = creator_id
        FROM tag_templates
        WHERE id = @template_id
        AND is_active = 1
        AND deleted_at IS NULL;
        
        IF @structure IS NULL
        BEGIN
            RAISERROR('Template not found or inactive', 16, 1);
            RETURN;
        END;
        
        -- Check permission: Public template OR owned by user
        IF @is_public = 0 AND @creator_id != @user_id
        BEGIN
            RAISERROR('You do not have permission to use this template', 16, 1);
            RETURN;
        END;
        
        -- Validate: Parent tag exists (if provided)
        IF @parent_tag_id IS NOT NULL AND NOT EXISTS (
            SELECT 1 FROM tags 
            WHERE id = @parent_tag_id AND user_id = @user_id AND deleted_at IS NULL
        )
        BEGIN
            RAISERROR('Parent tag not found', 16, 1);
            RETURN;
        END;
        
        -- Parse JSON structure and create tags recursively
        DECLARE @tag_mapping TABLE (
            temp_id NVARCHAR(50),  -- Temporary ID from JSON
            real_id INT            -- Actual created tag ID
        );
        
        -- Create tags from structure
        WITH RECURSIVE_CTE AS (
            SELECT 
                JSON_VALUE(value, '$.name') AS tag_name,
                JSON_VALUE(value, '$.color') AS tag_color,
                JSON_VALUE(value, '$.description') AS tag_description,
                JSON_VALUE(value, '$.id') AS temp_id,
                JSON_VALUE(value, '$.parent_id') AS temp_parent_id,
                @parent_tag_id AS parent_id,
                0 AS level
            FROM OPENJSON(@structure)
            
            UNION ALL
            
            SELECT
                JSON_VALUE(child.value, '$.name'),
                JSON_VALUE(child.value, '$.color'),
                JSON_VALUE(child.value, '$.description'),
                JSON_VALUE(child.value, '$.id'),
                JSON_VALUE(child.value, '$.parent_id'),
                tm.real_id,
                rc.level + 1
            FROM RECURSIVE_CTE rc
            CROSS APPLY OPENJSON(JSON_QUERY(rc.tag_name, '$.children')) AS child
            JOIN @tag_mapping tm ON tm.temp_id = rc.temp_id
        )
        
        -- Insert tags level by level
        INSERT INTO tags (user_id, name, color, description, parent_id)
        OUTPUT JSON_VALUE(inserted.name, '$.temp_id'), inserted.id INTO @tag_mapping
        SELECT 
            @user_id,
            tag_name,
            tag_color,
            tag_description,
            parent_id
        FROM RECURSIVE_CTE
        ORDER BY level;
        
        -- Track application
        INSERT INTO tag_template_applications (template_id, user_id, tag_mapping)
        VALUES (
            @template_id,
            @user_id,
            (SELECT * FROM @tag_mapping FOR JSON AUTO)
        );
        
        -- Update template stats
        UPDATE tag_templates
        SET clone_count = clone_count + 1,
            updated_at = GETUTCDATE()
        WHERE id = @template_id;
        
        -- Return created tags
        SELECT 
            t.id,
            t.name,
            t.path,
            t.color
        FROM tags t
        JOIN @tag_mapping tm ON tm.real_id = t.id
        ORDER BY t.path;
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
```

### 3. Get Public Templates

```sql
CREATE OR ALTER PROCEDURE sp_get_public_templates
    @category NVARCHAR(100) = NULL,
    @search NVARCHAR(255) = NULL,
    @sort_by NVARCHAR(20) = 'popular',  -- 'popular', 'recent', 'rating'
    @page INT = 1,
    @page_size INT = 20
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @offset INT = (@page - 1) * @page_size;
    
    -- Main query
    SELECT 
        t.id,
        t.name,
        t.slug,
        t.description,
        t.category,
        t.is_official,
        t.clone_count,
        t.view_count,
        t.rating_avg,
        t.rating_count,
        t.created_at,
        t.updated_at,
        -- Creator info
        u.id AS creator_id,
        u.display_name AS creator_name,
        u.email AS creator_email,
        -- Preview of structure (first level only)
        JSON_QUERY(t.structure, '$[0]') AS structure_preview,
        -- Total count for pagination
        COUNT(*) OVER() AS total_count
    FROM tag_templates t
    JOIN users u ON u.id = t.creator_id
    WHERE t.is_public = 1
    AND t.is_active = 1
    AND t.deleted_at IS NULL
    AND (@category IS NULL OR t.category = @category)
    AND (@search IS NULL OR 
         t.name LIKE '%' + @search + '%' OR 
         t.description LIKE '%' + @search + '%')
    ORDER BY 
        CASE WHEN @sort_by = 'popular' THEN t.clone_count END DESC,
        CASE WHEN @sort_by = 'rating' THEN t.rating_avg END DESC,
        CASE WHEN @sort_by = 'recent' THEN t.created_at END DESC
    OFFSET @offset ROWS
    FETCH NEXT @page_size ROWS ONLY;
END;
GO
```

### 4. Get Template Details

```sql
CREATE OR ALTER PROCEDURE sp_get_template_details
    @template_id INT,
    @user_id INT = NULL  -- Optional: To check if user owns it
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Update view count
    UPDATE tag_templates
    SET view_count = view_count + 1
    WHERE id = @template_id;
    
    -- Main template info
    SELECT 
        t.id,
        t.name,
        t.slug,
        t.description,
        t.category,
        t.structure,
        t.is_public,
        t.is_official,
        t.clone_count,
        t.view_count,
        t.rating_avg,
        t.rating_count,
        t.version,
        t.created_at,
        t.updated_at,
        -- Creator info
        u.id AS creator_id,
        u.display_name AS creator_name,
        u.email AS creator_email,
        u.avatar_url AS creator_avatar,
        -- User status
        CASE WHEN t.creator_id = @user_id THEN 1 ELSE 0 END AS is_owner,
        CASE WHEN EXISTS (
            SELECT 1 FROM tag_template_applications 
            WHERE template_id = @template_id AND user_id = @user_id
        ) THEN 1 ELSE 0 END AS has_applied
    FROM tag_templates t
    JOIN users u ON u.id = t.creator_id
    WHERE t.id = @template_id
    AND t.deleted_at IS NULL;
    
    -- Recent ratings
    SELECT TOP 5
        r.rating,
        r.review,
        r.created_at,
        u.display_name AS reviewer_name,
        u.avatar_url AS reviewer_avatar
    FROM tag_template_ratings r
    JOIN users u ON u.id = r.user_id
    WHERE r.template_id = @template_id
    ORDER BY r.created_at DESC;
END;
GO
```

### 5. Rate Template

```sql
CREATE OR ALTER PROCEDURE sp_rate_tag_template
    @template_id INT,
    @user_id INT,
    @rating TINYINT,
    @review NVARCHAR(1000) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Validate: Template exists
        IF NOT EXISTS (
            SELECT 1 FROM tag_templates 
            WHERE id = @template_id AND is_public = 1 AND deleted_at IS NULL
        )
        BEGIN
            RAISERROR('Template not found or not public', 16, 1);
            RETURN;
        END;
        
        -- Validate: Rating value
        IF @rating NOT BETWEEN 1 AND 5
        BEGIN
            RAISERROR('Rating must be between 1 and 5', 16, 1);
            RETURN;
        END;
        
        -- Validate: User has applied this template
        IF NOT EXISTS (
            SELECT 1 FROM tag_template_applications
            WHERE template_id = @template_id AND user_id = @user_id
        )
        BEGIN
            RAISERROR('You must apply the template before rating it', 16, 1);
            RETURN;
        END;
        
        -- Upsert rating
        MERGE tag_template_ratings AS target
        USING (SELECT @template_id AS template_id, @user_id AS user_id) AS source
        ON target.template_id = source.template_id AND target.user_id = source.user_id
        WHEN MATCHED THEN
            UPDATE SET 
                rating = @rating,
                review = @review,
                updated_at = GETUTCDATE()
        WHEN NOT MATCHED THEN
            INSERT (template_id, user_id, rating, review)
            VALUES (@template_id, @user_id, @rating, @review);
        
        -- Recalculate template rating average
        UPDATE tag_templates
        SET rating_avg = (
                SELECT AVG(CAST(rating AS DECIMAL(3,2)))
                FROM tag_template_ratings
                WHERE template_id = @template_id
            ),
            rating_count = (
                SELECT COUNT(*)
                FROM tag_template_ratings
                WHERE template_id = @template_id
            ),
            updated_at = GETUTCDATE()
        WHERE id = @template_id;
        
        SELECT 
            @template_id AS template_id,
            @rating AS rating,
            'SUCCESS' AS status;
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
```

### 6. Delete Template

```sql
CREATE OR ALTER PROCEDURE sp_delete_tag_template
    @template_id INT,
    @user_id INT  -- Must be creator or admin
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Validate: Template exists and user owns it
        DECLARE @creator_id INT;
        SELECT @creator_id = creator_id
        FROM tag_templates
        WHERE id = @template_id AND deleted_at IS NULL;
        
        IF @creator_id IS NULL
        BEGIN
            RAISERROR('Template not found', 16, 1);
            RETURN;
        END;
        
        IF @creator_id != @user_id
        BEGIN
            RAISERROR('You do not own this template', 16, 1);
            RETURN;
        END;
        
        -- Soft delete
        UPDATE tag_templates
        SET deleted_at = GETUTCDATE(),
            is_active = 0,
            updated_at = GETUTCDATE()
        WHERE id = @template_id;
        
        SELECT 
            @template_id AS template_id,
            'DELETED' AS status;
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
```

### 7. Export User Tags as Template

```sql
CREATE OR ALTER PROCEDURE sp_export_tags_as_template
    @user_id INT,
    @root_tag_id INT = NULL,  -- NULL = export all root tags
    @template_name NVARCHAR(255),
    @template_slug NVARCHAR(255),
    @description NVARCHAR(MAX) = NULL,
    @category NVARCHAR(100) = NULL,
    @is_public BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Build JSON structure from user's tags
        DECLARE @structure NVARCHAR(MAX);
        
        WITH TagTree AS (
            -- Root level
            SELECT 
                t.id,
                t.name,
                t.color,
                t.description,
                t.parent_id,
                0 AS level,
                CAST(t.id AS NVARCHAR(MAX)) AS path_ids
            FROM tags t
            WHERE t.user_id = @user_id
            AND t.deleted_at IS NULL
            AND (
                (@root_tag_id IS NULL AND t.parent_id IS NULL) OR
                (@root_tag_id IS NOT NULL AND t.id = @root_tag_id)
            )
            
            UNION ALL
            
            -- Recursive: Children
            SELECT 
                t.id,
                t.name,
                t.color,
                t.description,
                t.parent_id,
                tt.level + 1,
                tt.path_ids + '.' + CAST(t.id AS NVARCHAR(MAX))
            FROM tags t
            JOIN TagTree tt ON t.parent_id = tt.id
            WHERE t.deleted_at IS NULL
        )
        SELECT @structure = (
            SELECT 
                id,
                name,
                color,
                description,
                parent_id,
                level
            FROM TagTree
            ORDER BY path_ids
            FOR JSON PATH
        );
        
        -- Create template with exported structure
        EXEC sp_create_tag_template
            @creator_id = @user_id,
            @name = @template_name,
            @slug = @template_slug,
            @description = @description,
            @category = @category,
            @structure = @structure,
            @is_public = @is_public,
            @is_official = 0;
        
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

## 💡 Use Cases & Examples

### Example 1: Create Personal Template

```sql
-- User exports their GTD tags as private template
EXEC sp_export_tags_as_template
    @user_id = 1,
    @root_tag_id = NULL,  -- Export all root tags
    @template_name = 'My GTD System',
    @template_slug = 'my-gtd-system',
    @description = 'My personal Getting Things Done tag structure',
    @category = 'productivity',
    @is_public = 0;
```

### Example 2: Create Public Template Manually

```sql
-- Create a public template with custom structure
DECLARE @structure NVARCHAR(MAX) = N'[
    {
        "name": "Inbox",
        "color": "#CCCCCC",
        "description": "Items to process"
    },
    {
        "name": "Next Actions",
        "color": "#4CAF50",
        "description": "Tasks to do next"
    },
    {
        "name": "Projects",
        "color": "#2196F3",
        "description": "Multi-step outcomes",
        "children": [
            {"name": "Active", "color": "#4CAF50"},
            {"name": "Someday", "color": "#FFC107"}
        ]
    },
    {
        "name": "Waiting For",
        "color": "#FF9800",
        "description": "Delegated tasks"
    },
    {
        "name": "Reference",
        "color": "#9E9E9E",
        "description": "Archive and notes"
    }
]';

EXEC sp_create_tag_template
    @creator_id = 1,
    @name = 'GTD Productivity System',
    @slug = 'gtd-productivity',
    @description = 'Complete Getting Things Done tag structure',
    @category = 'productivity',
    @structure = @structure,
    @is_public = 1;
```

### Example 3: Apply Template

```sql
-- New user applies GTD template
EXEC sp_apply_tag_template
    @template_id = 1,
    @user_id = 5,
    @parent_tag_id = NULL;  -- Create at root level

-- Or apply under existing tag
EXEC sp_apply_tag_template
    @template_id = 1,
    @user_id = 5,
    @parent_tag_id = 42;  -- Clone under "Work" tag
```

### Example 4: Browse Templates

```sql
-- Get all productivity templates, sorted by popularity
EXEC sp_get_public_templates
    @category = 'productivity',
    @sort_by = 'popular',
    @page = 1,
    @page_size = 10;

-- Search templates
EXEC sp_get_public_templates
    @search = 'project',
    @sort_by = 'rating',
    @page = 1,
    @page_size = 20;
```

### Example 5: Rate Template

```sql
-- User rates template after using it
EXEC sp_rate_tag_template
    @template_id = 1,
    @user_id = 5,
    @rating = 5,
    @review = 'Perfect GTD setup! Saved me hours of organization.';
```

### Example 6: Get Template Details

```sql
-- View full template details
EXEC sp_get_template_details
    @template_id = 1,
    @user_id = 5;  -- Optional: Check ownership
```

---

## ✅ Best Practices

### 1. **Template Design**
- ✅ Keep structure shallow (max 3-4 levels deep)
- ✅ Use clear, descriptive names
- ✅ Include helpful descriptions
- ✅ Choose meaningful colors
- ✅ Limit to 20-30 tags per template

### 2. **Naming Conventions**
- ✅ Template names: Title Case ("GTD Productivity")
- ✅ Slugs: kebab-case ("gtd-productivity")
- ✅ Categories: lowercase ("productivity")

### 3. **Categories**
- ✅ Use existing categories when possible
- ✅ Create new categories only for distinct use cases
- ✅ Keep category list manageable (< 15 categories)

### 4. **Public Templates**
- ✅ Test thoroughly before making public
- ✅ Write clear descriptions
- ✅ Respond to feedback
- ✅ Update based on user ratings
- ✅ Version templates for major changes

### 5. **Performance**
- ✅ Cache popular templates
- ✅ Pre-compute structure preview
- ✅ Index by category and rating
- ✅ Monitor clone performance

---

## 🚀 Advanced Features

### Template Versioning

```sql
-- Add version tracking
ALTER TABLE tag_templates
ADD previous_version_id INT NULL,
    CONSTRAINT fk_template_version FOREIGN KEY (previous_version_id) 
    REFERENCES tag_templates(id);

-- Create new version
CREATE PROCEDURE sp_create_template_version
    @template_id INT,
    @user_id INT,
    @new_structure NVARCHAR(MAX)
AS
BEGIN
    -- Verify ownership
    -- Create new template with previous_version_id
    -- Increment version number
    -- ...
END;
```

### Template Collections

```sql
-- Group related templates
CREATE TABLE template_collections (
    id INT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(255),
    description NVARCHAR(MAX),
    creator_id INT,
    is_public BIT DEFAULT 0
);

CREATE TABLE template_collection_items (
    collection_id INT,
    template_id INT,
    sort_order INT,
    PRIMARY KEY (collection_id, template_id)
);
```

### Template Analytics

```sql
-- Track detailed usage
CREATE TABLE template_analytics (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    template_id INT,
    event_type NVARCHAR(50),  -- 'VIEW', 'CLONE', 'RATE'
    user_id INT,
    created_at DATETIME2 DEFAULT GETUTCDATE()
);

-- Query analytics
SELECT 
    t.name,
    COUNT(CASE WHEN a.event_type = 'VIEW' THEN 1 END) AS views,
    COUNT(CASE WHEN a.event_type = 'CLONE' THEN 1 END) AS clones,
    COUNT(CASE WHEN a.event_type = 'RATE' THEN 1 END) AS ratings
FROM tag_templates t
LEFT JOIN template_analytics a ON a.template_id = t.id
WHERE a.created_at >= DATEADD(DAY, -30, GETUTCDATE())
GROUP BY t.id, t.name;
```

---

## 🧪 Testing Examples

### Test 1: Template Lifecycle

```sql
-- Create template
DECLARE @template_id INT;
EXEC sp_create_tag_template 
    @creator_id = 1,
    @name = 'Test Template',
    @slug = 'test-template',
    @structure = '[{"name": "Tag1"}, {"name": "Tag2"}]',
    @is_public = 1;

-- Apply template
EXEC sp_apply_tag_template @template_id = @template_id, @user_id = 2;

-- Rate template
EXEC sp_rate_tag_template @template_id = @template_id, @user_id = 2, @rating = 4;

-- Verify
SELECT * FROM tag_templates WHERE id = @template_id;
SELECT * FROM tag_template_applications WHERE template_id = @template_id;
SELECT * FROM tag_template_ratings WHERE template_id = @template_id;
```

### Test 2: Complex Structure

```sql
-- Test nested structure (3 levels)
DECLARE @complex_structure NVARCHAR(MAX) = N'[
    {
        "name": "Work",
        "color": "#FF0000",
        "children": [
            {
                "name": "Project A",
                "children": [
                    {"name": "Phase 1"},
                    {"name": "Phase 2"}
                ]
            },
            {"name": "Project B"}
        ]
    }
]';

EXEC sp_create_tag_template
    @creator_id = 1,
    @name = 'Complex Hierarchy',
    @slug = 'complex-hierarchy',
    @structure = @complex_structure;
```

---

## 📚 Related Documentation

- [02-Schema.md](02-Schema.md) - Database schema
- [04-Procedures.md](04-Procedures.md) - All stored procedures
- [06-Sharing.md](06-Sharing.md) - Sharing system
- [08-Testing.md](08-Testing.md) - Test cases

---

**End of 07-Templates.md**
