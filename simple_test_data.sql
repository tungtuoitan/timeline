-- =============================================
-- Simple Test Data Script for SuperApp
-- Tạo dữ liệu test cơ bản cho việc testing
-- =============================================

SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;

-- Đặt transaction để có thể rollback nếu cần
BEGIN TRANSACTION;

BEGIN TRY
    -- Clear existing data 
    DELETE FROM note_versions;
    DELETE FROM note_members;
    DELETE FROM notes;
    DELETE FROM workspace_items;
    DELETE FROM workspace_members;
    DELETE FROM workspace_relationship_types;
    DELETE FROM workspaces;
    DELETE FROM tags;
    DELETE FROM entity_types;
    DELETE FROM users;

    PRINT 'Starting simple test data insertion...';

    -- =============================================
    -- 1. INSERT USERS (5 test users)
    -- =============================================
    PRINT 'Inserting users...';
    
    SET IDENTITY_INSERT users ON;
    INSERT INTO users (user_id, email, username, password_hash, display_name, is_active, email_verified, created_at)
    VALUES 
        (1, 'admin@superapp.com', 'admin', '$2a$12$EixZaYVK1fsbw1ZfbX3OXePaWxn96p36WQoeG6Lruj3vjPGga31lW', 'System Administrator', 1, 1, GETUTCDATE()),
        (2, 'john.doe@company.com', 'johndoe', '$2a$12$EixZaYVK1fsbw1ZfbX3OXePaWxn96p36WQoeG6Lruj3vjPGga31lW', 'John Doe', 1, 1, GETUTCDATE()),
        (3, 'jane.smith@company.com', 'janesmith', '$2a$12$EixZaYVK1fsbw1ZfbX3OXePaWxn96p36WQoeG6Lruj3vjPGga31lW', 'Jane Smith', 1, 1, GETUTCDATE()),
        (4, 'mike.wilson@company.com', 'mikewilson', '$2a$12$EixZaYVK1fsbw1ZfbX3OXePaWxn96p36WQoeG6Lruj3vjPGga31lW', 'Mike Wilson', 1, 1, GETUTCDATE()),
        (5, 'sarah.brown@company.com', 'sarahbrown', '$2a$12$EixZaYVK1fsbw1ZfbX3OXePaWxn96p36WQoeG6Lruj3vjPGga31lW', 'Sarah Brown', 1, 1, GETUTCDATE());
    SET IDENTITY_INSERT users OFF;

    -- =============================================
    -- 2. INSERT ENTITY TYPES
    -- =============================================
    PRINT 'Inserting entity types...';
    
    INSERT INTO entity_types (type_name, display_name, display_plural, icon, color, description, table_name, supports_versioning, supports_sharing, is_enabled, created_at)
    VALUES 
        ('tag', 'Tag', 'Tags', 'tag', '#3B82F6', 'Organizational tags for categorization', 'tags', 0, 1, 1, GETUTCDATE()),
        ('note', 'Note', 'Notes', 'file-text', '#10B981', 'Text notes with markdown support', 'notes', 1, 1, 1, GETUTCDATE());

    -- =============================================
    -- 3. INSERT TAGS (10 tags)
    -- =============================================
    PRINT 'Inserting tags...';
    
    INSERT INTO tags (user_id, name, slug, color, icon, description, usage_count, created_at)
    VALUES 
        (1, 'System', 'system', '#EF4444', 'server', 'System administration tasks', 5, GETUTCDATE()),
        (1, 'Important', 'important', '#DC2626', 'star', 'High priority items', 10, GETUTCDATE()),
        (2, 'Work', 'work', '#3B82F6', 'briefcase', 'Work-related content', 15, GETUTCDATE()),
        (2, 'Project Alpha', 'project-alpha', '#8B5CF6', 'folder', 'Project Alpha related items', 8, GETUTCDATE()),
        (3, 'Development', 'development', '#10B981', 'code', 'Software development', 12, GETUTCDATE()),
        (3, 'Frontend', 'frontend', '#06B6D4', 'monitor', 'Frontend development', 6, GETUTCDATE()),
        (4, 'Research', 'research', '#F59E0B', 'search', 'Research and analysis', 7, GETUTCDATE()),
        (4, 'Clients', 'clients', '#EF4444', 'users', 'Client-related items', 9, GETUTCDATE()),
        (5, 'Marketing', 'marketing', '#EC4899', 'megaphone', 'Marketing activities', 11, GETUTCDATE()),
        (5, 'Content', 'content', '#8B5CF6', 'edit', 'Content creation', 8, GETUTCDATE());

    -- =============================================
    -- 4. INSERT WORKSPACES (5 workspaces)
    -- =============================================
    PRINT 'Inserting workspaces...';
    
    SET IDENTITY_INSERT workspaces ON;
    INSERT INTO workspaces (workspace_id, user_id, name, description, type, settings, created_at)
    VALUES 
        (1, 1, 'System Administration', 'Main workspace for system administration tasks', 'hierarchy', '{"theme": "dark", "default_view": "tree"}', GETUTCDATE()),
        (2, 2, 'Main Workspace', 'John primary workspace for daily tasks', 'hierarchy', '{"theme": "auto", "default_view": "tree"}', GETUTCDATE()),
        (3, 3, 'Development Projects', 'Software development workspace', 'hierarchy', '{"theme": "dark", "default_view": "tree"}', GETUTCDATE()),
        (4, 4, 'Research & Analysis', 'Research and data analysis workspace', 'hierarchy', '{"theme": "light", "default_view": "tree"}', GETUTCDATE()),
        (5, 5, 'Marketing Campaigns', 'Marketing campaign planning', 'network', '{"theme": "light", "default_view": "kanban"}', GETUTCDATE());
    SET IDENTITY_INSERT workspaces OFF;

    -- =============================================
    -- 5. INSERT WORKSPACE MEMBERS
    -- =============================================
    PRINT 'Inserting workspace members...';
    
    INSERT INTO workspace_members (workspace_id, user_id, role, custom_permissions, invited_at, invited_by)
    VALUES 
        -- Owner memberships (auto-created by application, but adding explicitly)
        (1, 1, 'owner', '{"read": true, "write": true, "delete": true, "admin": true}', GETUTCDATE(), 1),
        (2, 2, 'owner', '{"read": true, "write": true, "delete": true, "admin": true}', GETUTCDATE(), 2),
        (3, 3, 'owner', '{"read": true, "write": true, "delete": true, "admin": true}', GETUTCDATE(), 3),
        (4, 4, 'owner', '{"read": true, "write": true, "delete": true, "admin": true}', GETUTCDATE(), 4),
        (5, 5, 'owner', '{"read": true, "write": true, "delete": true, "admin": true}', GETUTCDATE(), 5),
        
        -- Cross-collaboration
        (2, 3, 'editor', '{"read": true, "write": true, "delete": false, "admin": false}', GETUTCDATE(), 2),
        (3, 2, 'viewer', '{"read": true, "write": false, "delete": false, "admin": false}', GETUTCDATE(), 3);

    -- =============================================
    -- 6. INSERT NOTES (10 notes)
    -- =============================================
    PRINT 'Inserting notes...';
    
    SET IDENTITY_INSERT notes ON;
    INSERT INTO notes (note_id, user_id, name, slug, content, is_archived, is_pinned, is_favorite, created_at)
    VALUES 
        (1, 1, 'System Maintenance Checklist', 'system-maintenance-checklist', 
         '# System Maintenance Checklist\n\n## Daily Tasks\n- [ ] Check server status\n- [ ] Review error logs\n\n## Weekly Tasks\n- [ ] Security updates\n- [ ] Performance analysis', 
         0, 1, 1, GETUTCDATE()),
         
        (2, 2, 'Project Alpha Requirements', 'project-alpha-requirements', 
         '# Project Alpha Requirements\n\n## Functional Requirements\n- User authentication\n- Data visualization\n\n## Non-Functional Requirements\n- Performance: < 2s load time\n- Availability: 99.9% uptime', 
         0, 1, 1, GETUTCDATE()),
         
        (3, 2, 'Meeting Notes - Sprint Planning', 'sprint-planning-meeting', 
         '# Sprint Planning Meeting\n**Date:** October 16, 2025\n**Attendees:** John, Jane, Mike\n\n## Sprint Goal\nDeliver user authentication and basic dashboard functionality', 
         0, 1, 0, GETUTCDATE()),
         
        (4, 3, 'Frontend Architecture Decisions', 'frontend-architecture', 
         '# Frontend Architecture Decisions\n\n## Framework: React 18\n**Pros:**\n- Large ecosystem\n- Good performance\n\n## State Management: Zustand\n**Pros:**\n- Lightweight\n- No boilerplate', 
         0, 1, 1, GETUTCDATE()),
         
        (5, 3, 'Code Review Guidelines', 'code-review-guidelines', 
         '# Code Review Guidelines\n\n## Before Submitting\n- [ ] Code is self-documenting\n- [ ] Tests are written and passing\n\n## Review Checklist\n- [ ] Functionality works as expected\n- [ ] Code is readable', 
         0, 1, 0, GETUTCDATE()),
         
        (6, 4, 'Market Research Analysis Q4 2025', 'market-research-q4-2025', 
         '# Market Research Analysis Q4 2025\n\n## Key Findings\n- Market size: $2.5B (15% YoY growth)\n- Top competitors: CompanyA (25%), CompanyB (18%)', 
         0, 1, 1, GETUTCDATE()),
         
        (7, 4, 'Client Project Status Report', 'client-project-status', 
         '# Client Project Status Report\n**Date:** October 16, 2025\n\n## Project Alpha (TechCorp)\n- **Status:** On track\n- **Progress:** 75% complete', 
         0, 1, 0, GETUTCDATE()),
         
        (8, 5, 'Content Calendar - November 2025', 'content-calendar-nov-2025', 
         '# Content Calendar - November 2025\n\n## Week 1\n- **Monday:** Product announcement blog post\n- **Wednesday:** Customer success story video', 
         0, 1, 1, GETUTCDATE()),
         
        (9, 5, 'Social Media Strategy 2026', 'social-media-strategy-2026', 
         '# Social Media Strategy 2026\n\n## Objectives\n- Increase brand awareness by 40%\n- Generate 500 qualified leads/month', 
         0, 1, 1, GETUTCDATE()),
         
        (10, 1, 'Security Incident Response Plan', 'security-incident-response', 
         '# Security Incident Response Plan\n\n## Phase 1: Detection\n1. Monitor alerts\n2. Validate incidents\n\n## Phase 2: Response\n1. Containment\n2. Eradication', 
         0, 1, 0, GETUTCDATE());
    SET IDENTITY_INSERT notes OFF;

    -- =============================================
    -- 7. INSERT NOTE MEMBERS
    -- =============================================
    PRINT 'Inserting note members...';
    
    INSERT INTO note_members (note_id, user_id, role, invited_by, invited_at)
    VALUES 
        -- Owner memberships (auto-created by triggers, but adding explicitly)
        (1, 1, 'owner', 1, GETUTCDATE()),
        (2, 2, 'owner', 2, GETUTCDATE()),
        (3, 2, 'owner', 2, GETUTCDATE()),
        (4, 3, 'owner', 3, GETUTCDATE()),
        (5, 3, 'owner', 3, GETUTCDATE()),
        (6, 4, 'owner', 4, GETUTCDATE()),
        (7, 4, 'owner', 4, GETUTCDATE()),
        (8, 5, 'owner', 5, GETUTCDATE()),
        (9, 5, 'owner', 5, GETUTCDATE()),
        (10, 1, 'owner', 1, GETUTCDATE()),
        
        -- Sharing examples
        (2, 3, 'editor', 2, GETUTCDATE()), -- Jane can edit John's project requirements
        (4, 2, 'viewer', 3, GETUTCDATE()); -- John can view Jane's architecture decisions

    -- =============================================
    -- Statistics
    -- =============================================
    PRINT 'Data insertion completed successfully!';
    PRINT '';
    PRINT '=== INSERTION SUMMARY ===';
    
    DECLARE @UserCount INT = (SELECT COUNT(*) FROM users);
    DECLARE @EntityTypeCount INT = (SELECT COUNT(*) FROM entity_types);
    DECLARE @TagCount INT = (SELECT COUNT(*) FROM tags);
    DECLARE @WorkspaceCount INT = (SELECT COUNT(*) FROM workspaces);
    DECLARE @WorkspaceMemberCount INT = (SELECT COUNT(*) FROM workspace_members);
    DECLARE @NoteCount INT = (SELECT COUNT(*) FROM notes);
    DECLARE @NoteMemberCount INT = (SELECT COUNT(*) FROM note_members);
    
    PRINT 'Users: ' + CAST(@UserCount AS VARCHAR);
    PRINT 'Entity Types: ' + CAST(@EntityTypeCount AS VARCHAR);
    PRINT 'Tags: ' + CAST(@TagCount AS VARCHAR);
    PRINT 'Workspaces: ' + CAST(@WorkspaceCount AS VARCHAR);
    PRINT 'Workspace Members: ' + CAST(@WorkspaceMemberCount AS VARCHAR);
    PRINT 'Notes: ' + CAST(@NoteCount AS VARCHAR);
    PRINT 'Note Members: ' + CAST(@NoteMemberCount AS VARCHAR);
    
    PRINT '';
    PRINT '=== SUCCESS ===';
    PRINT 'Simple test data has been successfully inserted into SuperApp-dev database.';
    PRINT 'You can now test the application with this basic dataset.';

    COMMIT TRANSACTION;
    
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'Error occurred during data insertion:';
    PRINT ERROR_MESSAGE();
    THROW;
END CATCH;