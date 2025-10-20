-- =============================================
-- Comprehensive Test Data Script for SuperApp
-- Tạo dữ liệu test đầy đủ cho tất cả các bảng
-- =============================================

SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;

-- Đặt transaction để có thể rollback nếu cần
BEGIN TRANSACTION;

BEGIN TRY
    -- Clear existing data (nếu cần fresh start)
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
    -- DELETE FROM tags;
    -- DELETE FROM entity_types;
    -- DELETE FROM users;

    PRINT 'Starting comprehensive test data insertion...';

    -- =============================================
    -- 1. INSERT USERS (10 test users)
    -- =============================================
    PRINT 'Inserting users...';
    
    SET IDENTITY_INSERT users ON;
    INSERT INTO users (user_id, email, username, password_hash, display_name, is_active, email_verified, created_at)
    VALUES(4, , '', 'admin', '$2a$12$EixZaYVK1fsbw1ZfbX3OXePaWxn96p36WQoeG6Lruj3vjPGga31lW', 'System Administrator', 1, 1, GETUTCDATE()),(5, , '', 'johndoe', '$2a$12$EixZaYVK1fsbw1ZfbX3OXePaWxn96p36WQoeG6Lruj3vjPGga31lW', 'John Doe', 1, 1, GETUTCDATE()),(6, , '', 'janesmith', '$2a$12$EixZaYVK1fsbw1ZfbX3OXePaWxn96p36WQoeG6Lruj3vjPGga31lW', 'Jane Smith', 1, 1, GETUTCDATE()),(7, , '', 'mikewilson', '$2a$12$EixZaYVK1fsbw1ZfbX3OXePaWxn96p36WQoeG6Lruj3vjPGga31lW', 'Mike Wilson', 1, 1, GETUTCDATE()),(8, , '', 'sarahbrown', '$2a$12$EixZaYVK1fsbw1ZfbX3OXePaWxn96p36WQoeG6Lruj3vjPGga31lW', 'Sarah Brown', 1, 1, GETUTCDATE()),(9, , '', 'davidlee', '$2a$12$EixZaYVK1fsbw1ZfbX3OXePaWxn96p36WQoeG6Lruj3vjPGga31lW', 'David Lee', 1, 1, GETUTCDATE()),(10, , '', 'emmadavis', '$2a$12$EixZaYVK1fsbw1ZfbX3OXePaWxn96p36WQoeG6Lruj3vjPGga31lW', 'Emma Davis', 1, 1, GETUTCDATE()),(11, , '', 'alexjohnson', '$2a$12$EixZaYVK1fsbw1ZfbX3OXePaWxn96p36WQoeG6Lruj3vjPGga31lW', 'Alex Johnson', 1, 1, GETUTCDATE()),(12, , '', 'lisagarcia', '$2a$12$EixZaYVK1fsbw1ZfbX3OXePaWxn96p36WQoeG6Lruj3vjPGga31lW', 'Lisa Garcia', 1, 1, GETUTCDATE()),(13, , '', 'tommartinez', '$2a$12$EixZaYVK1fsbw1ZfbX3OXePaWxn96p36WQoeG6Lruj3vjPGga31lW', 'Tom Martinez', 1, 1, GETUTCDATE());
    SET IDENTITY_INSERT users OFF;

    -- =============================================
    -- 2. INSERT ENTITY TYPES
    -- =============================================
    PRINT 'Inserting entity types...';
    
    INSERT INTO entity_types (type_name, display_name, display_plural, icon, color, description, table_name, supports_versioning, supports_sharing, is_enabled, created_at)
    VALUES 
        ('tag', 'Tag', 'Tags', 'tag', '#3B82F6', 'Organizational tags for categorization', 'tags', 0, 1, 1, GETUTCDATE()),
        ('note', 'Note', 'Notes', 'file-text', '#10B981', 'Text notes with markdown support', 'notes', 1, 1, 1, GETUTCDATE()),
        ('task', 'Task', 'Tasks', 'check-square', '#F59E0B', 'Action items and todos', 'tasks', 0, 1, 0, GETUTCDATE()),
        ('project', 'Project', 'Projects', 'folder', '#8B5CF6', 'Project containers', 'projects', 1, 1, 0, GETUTCDATE()),
        ('document', 'Document', 'Documents', 'file', '#6366F1', 'Structured documents', 'documents', 1, 1, 0, GETUTCDATE());

    -- =============================================
    -- 3. INSERT TAGS (30+ tags across different users)
    -- =============================================
    PRINT 'Inserting tags...';
    
    INSERT INTO tags (user_id, name, slug, color, icon, description, usage_count, created_at)
    VALUES 
        -- Admin tags (user_id = 1)(14, , '', 'system', '#EF4444', 'server', 'System administration tasks', 15, GETUTCDATE()),(15, , '', 'important', '#DC2626', 'star', 'High priority items', 25, GETUTCDATE()),(16, , '', 'documentation', '#059669', 'book', 'Documentation related', 12, GETUTCDATE()),
        
        -- John Doe tags (user_id = 2)(17, , '', 'work', '#3B82F6', 'briefcase', 'Work-related items', 18, GETUTCDATE()),(18, , '', 'personal', '#10B981', 'home', 'Personal items', 8, GETUTCDATE()),(19, , '', 'project-alpha', '#8B5CF6', 'folder', 'Alpha project items', 6, GETUTCDATE()),(20, , '', 'meeting-notes', '#F59E0B', 'users', 'Meeting-related notes', 14, GETUTCDATE()),(21, , '', 'ideas', '#EC4899', 'lightbulb', 'Creative ideas and brainstorming', 9, GETUTCDATE()),
        
        -- Jane Smith tags (user_id = 3)(22, , '', 'development', '#1D4ED8', 'code', 'Software development', 20, GETUTCDATE()),(23, , '', 'testing', '#7C2D12', 'bug', 'Testing and QA', 11, GETUTCDATE()),(24, , '', 'frontend', '#0891B2', 'monitor', 'Frontend development', 16, GETUTCDATE()),(25, , '', 'backend', '#BE123C', 'server', 'Backend development', 13, GETUTCDATE()),(26, , '', 'api', '#9333EA', 'link', 'API related work', 7, GETUTCDATE()),
        
        -- Mike Wilson tags (user_id = 4)(27, , '', 'research', '#0F766E', 'search', 'Research and analysis', 10, GETUTCDATE()),(28, , '', 'analysis', '#B45309', 'chart-bar', 'Data analysis', 8, GETUTCDATE()),(29, , '', 'reports', '#4338CA', 'document-text', 'Report generation', 5, GETUTCDATE()),(30, , '', 'clients', '#BE185D', 'users', 'Client communications', 12, GETUTCDATE()),
        
        -- Sarah Brown tags (user_id = 5)(31, , '', 'marketing', '#DB2777', 'megaphone', 'Marketing campaigns', 9, GETUTCDATE()),(32, , '', 'content', '#7C3AED', 'document-duplicate', 'Content creation', 15, GETUTCDATE()),(33, , '', 'social-media', '#2563EB', 'share', 'Social media management', 11, GETUTCDATE()),(34, , '', 'design', '#DC2626', 'color-swatch', 'Design work', 13, GETUTCDATE()),
        
        -- More tags for other users(35, , '', 'freelance', '#059669', 'briefcase', 'Freelance projects', 7, GETUTCDATE()),(36, , '', 'learning', '#7C2D12', 'academic-cap', 'Learning and education', 6, GETUTCDATE()),(37, , '', 'startup', '#EA580C', 'rocket', 'Startup related', 8, GETUTCDATE()),(38, , '', 'innovation', '#0891B2', 'light-bulb', 'Innovation projects', 4, GETUTCDATE()),(39, , '', 'technology', '#4F46E5', 'chip', 'Technology research', 12, GETUTCDATE()),(40, , '', 'ai-ml', '#7C3AED', 'cpu-chip', 'Artificial Intelligence & Machine Learning', 9, GETUTCDATE()),(41, , '', 'ux-ui', '#EC4899', 'cursor-arrow-rays', 'User Experience & Interface Design', 14, GETUTCDATE()),(42, , '', 'prototyping', '#0D9488', 'cube', 'Prototyping work', 5, GETUTCDATE()),(43, , '', 'consulting', '#B91C1C', 'chat-bubble-left-right', 'Consulting projects', 10, GETUTCDATE()),(44, , '', 'strategy', '#1F2937', 'chart-pie', 'Strategic planning', 7, GETUTCDATE());

    -- =============================================
    -- 4. INSERT WORKSPACES (15+ workspaces)
    -- =============================================
    PRINT 'Inserting workspaces...';
    
    SET IDENTITY_INSERT workspaces ON;
    INSERT INTO workspaces (workspace_id, user_id, name, description, type, settings, created_at)
    VALUES 
        (1, 1, 'System Administration', 'Main workspace for system administration tasks', 'hierarchy', '{"theme": "dark", "default_view": "tree"}', GETUTCDATE()),
        (2, 1, 'Documentation Hub', 'Central documentation workspace', 'hierarchy', '{"theme": "light", "default_view": "list"}', GETUTCDATE()),
        
        (3, 2, 'Main Workspace', 'John primary workspace for daily tasks', 'hierarchy', '{"theme": "auto", "default_view": "tree"}', GETUTCDATE()),
        (4, 2, 'Project Alpha', 'Dedicated workspace for Alpha project', 'graph', '{"theme": "light", "default_view": "graph"}', GETUTCDATE()),
        (5, 2, 'Personal Organization', 'Personal task and note organization', 'timeline', '{"theme": "light", "default_view": "calendar"}', GETUTCDATE()),
        
        (6, 3, 'Development Projects', 'Software development workspace', 'hierarchy', '{"theme": "dark", "default_view": "tree"}', GETUTCDATE()),
        (7, 3, 'Code Reviews', 'Code review and testing workspace', 'network', '{"theme": "dark", "default_view": "network"}', GETUTCDATE()),
        
        (8, 4, 'Research & Analysis', 'Research and data analysis workspace', 'hierarchy', '{"theme": "light", "default_view": "tree"}', GETUTCDATE()),
        (9, 4, 'Client Projects', 'Client project management', 'timeline', '{"theme": "auto", "default_view": "gantt"}', GETUTCDATE()),
        
        (10, 5, 'Marketing Campaigns', 'Marketing campaign planning', 'network', '{"theme": "light", "default_view": "kanban"}', GETUTCDATE()),
        (11, 5, 'Content Creation', 'Content creation and management', 'hierarchy', '{"theme": "light", "default_view": "list"}', GETUTCDATE()),
        
        (12, 6, 'Freelance Projects', 'Freelance project tracking', 'timeline', '{"theme": "auto", "default_view": "timeline"}', GETUTCDATE()),
        (13, 7, 'Startup Ideas', 'Startup concept development', 'graph', '{"theme": "light", "default_view": "mindmap"}', GETUTCDATE()),
        (14, 8, 'Tech Research', 'Technology research and development', 'hierarchy', '{"theme": "dark", "default_view": "tree"}', GETUTCDATE()),
        (15, 9, 'Design Portfolio', 'Design work and portfolio', 'network', '{"theme": "light", "default_view": "gallery"}', GETUTCDATE()),
        (16, 10, 'Consulting Hub', 'Consulting project management', 'hierarchy', '{"theme": "auto", "default_view": "tree"}', GETUTCDATE());
    SET IDENTITY_INSERT workspaces OFF;

    -- =============================================
    -- 5. INSERT WORKSPACE MEMBERS (Sharing & Collaboration)
    -- =============================================
    PRINT 'Inserting workspace members...';
    
    INSERT INTO workspace_members (workspace_id, user_id, role, custom_permissions, joined_at, invited_by)
    VALUES 
        -- Owner memberships (auto-created, but adding explicitly)
        (1, 1, 'owner', '{"read": true, "write": true, "delete": true, "admin": true}', GETUTCDATE(), 1),
        (2, 1, 'owner', '{"read": true, "write": true, "delete": true, "admin": true}', GETUTCDATE(), 1),
        (3, 2, 'owner', '{"read": true, "write": true, "delete": true, "admin": true}', GETUTCDATE(), 2),
        (4, 2, 'owner', '{"read": true, "write": true, "delete": true, "admin": true}', GETUTCDATE(), 2),
        (5, 2, 'owner', '{"read": true, "write": true, "delete": true, "admin": true}', GETUTCDATE(), 2),
        (6, 3, 'owner', '{"read": true, "write": true, "delete": true, "admin": true}', GETUTCDATE(), 3),
        (7, 3, 'owner', '{"read": true, "write": true, "delete": true, "admin": true}', GETUTCDATE(), 3),
        (8, 4, 'owner', '{"read": true, "write": true, "delete": true, "admin": true}', GETUTCDATE(), 4),
        (9, 4, 'owner', '{"read": true, "write": true, "delete": true, "admin": true}', GETUTCDATE(), 4),
        (10, 5, 'owner', '{"read": true, "write": true, "delete": true, "admin": true}', GETUTCDATE(), 5),
        (11, 5, 'owner', '{"read": true, "write": true, "delete": true, "admin": true}', GETUTCDATE(), 5),
        (12, 6, 'owner', '{"read": true, "write": true, "delete": true, "admin": true}', GETUTCDATE(), 6),
        (13, 7, 'owner', '{"read": true, "write": true, "delete": true, "admin": true}', GETUTCDATE(), 7),
        (14, 8, 'owner', '{"read": true, "write": true, "delete": true, "admin": true}', GETUTCDATE(), 8),
        (15, 9, 'owner', '{"read": true, "write": true, "delete": true, "admin": true}', GETUTCDATE(), 9),
        (16, 10, 'owner', '{"read": true, "write": true, "delete": true, "admin": true}', GETUTCDATE(), 10),
        
        -- Collaborative memberships
        (4, 3, 'editor', '{"read": true, "write": true, "delete": false, "admin": false}', GETUTCDATE(), 2), -- Jane in John's Alpha project
        (4, 5, 'viewer', '{"read": true, "write": false, "delete": false, "admin": false}', GETUTCDATE(), 2), -- Sarah in Alpha project
        (6, 2, 'editor', '{"read": true, "write": true, "delete": false, "admin": false}', GETUTCDATE(), 3), -- John in Jane's dev workspace
        (7, 4, 'viewer', '{"read": true, "write": false, "delete": false, "admin": false}', GETUTCDATE(), 3), -- Mike in code reviews
        (8, 2, 'editor', '{"read": true, "write": true, "delete": false, "admin": false}', GETUTCDATE(), 4), -- John in Mike's research
        (10, 6, 'editor', '{"read": true, "write": true, "delete": false, "admin": false}', GETUTCDATE(), 5), -- David in Sarah's marketing
        (13, 8, 'editor', '{"read": true, "write": true, "delete": false, "admin": false}', GETUTCDATE(), 7), -- Alex in Emma's startup
        (14, 9, 'viewer', '{"read": true, "write": false, "delete": false, "admin": false}', GETUTCDATE(), 8), -- Lisa in Alex's tech
        (1, 2, 'viewer', '{"read": true, "write": false, "delete": false, "admin": false}', GETUTCDATE(), 1), -- John can view admin workspace
        (2, 3, 'editor', '{"read": true, "write": true, "delete": false, "admin": false}', GETUTCDATE(), 1); -- Jane can edit docs

    -- =============================================
    -- 6. INSERT WORKSPACE RELATIONSHIP TYPES
    -- =============================================
    PRINT 'Inserting workspace relationship types...';
    
    INSERT INTO workspace_relationship_types (workspace_id, type_name, display_name, description, is_bidirectional, line_style, line_width, allows_cycles, sort_order, created_at)
    VALUES 
        -- System Admin workspace relationships(45, , '', 'Contains', 'Parent-child containment for system hierarchy', 0, 'solid', 2, 0, 1, GETUTCDATE()),(46, , '', 'Related To', 'General relationship between system components', 1, 'dashed', 1, 1, 2, GETUTCDATE()),(47, , '', 'Depends On', 'System dependency relationship', 0, 'solid', 2, 0, 3, GETUTCDATE()),
        
        -- Documentation Hub relationships(48, , '', 'Documents', 'Documentation relationship', 0, 'solid', 1, 0, 1, GETUTCDATE()),(49, , '', 'References', 'Cross-reference relationship', 1, 'dotted', 1, 1, 2, GETUTCDATE()),
        
        -- Project Alpha relationships (graph type)(50, , '', 'Blocks', 'Task blocking relationship', 0, 'solid', 3, 0, 1, GETUTCDATE()),(51, , '', 'Collaborates With', 'Collaboration relationship', 1, 'dashed', 2, 1, 2, GETUTCDATE()),(52, , '', 'Implements', 'Implementation relationship', 0, 'solid', 2, 0, 3, GETUTCDATE()),
        
        -- Development workspace relationships(53, , '', 'Extends', 'Code extension relationship', 0, 'solid', 2, 0, 1, GETUTCDATE()),(54, , '', 'Uses', 'Usage relationship', 0, 'dashed', 1, 1, 2, GETUTCDATE()),(55, , '', 'Tests', 'Testing relationship', 0, 'solid', 2, 0, 3, GETUTCDATE()),
        
        -- Timeline workspace relationships(56, , '', 'Precedes', 'Temporal precedence', 0, 'solid', 2, 0, 1, GETUTCDATE()),(57, , '', 'Concurrent With', 'Concurrent activities', 1, 'dashed', 1, 1, 2, GETUTCDATE()),
        
        -- Marketing workspace relationships (network type)(58, , '', 'Targets', 'Marketing target relationship', 0, 'solid', 2, 0, 1, GETUTCDATE()),(59, , '', 'Supports', 'Marketing support relationship', 0, 'dashed', 1, 1, 2, GETUTCDATE()),(60, , '', 'Competes With', 'Competitive relationship', 1, 'solid', 3, 1, 3, GETUTCDATE());

    -- =============================================
    -- 7. INSERT NOTES (50+ notes with rich content)
    -- =============================================
    PRINT 'Inserting notes...';
    
    SET IDENTITY_INSERT notes ON;
    INSERT INTO notes (note_id, user_id, name, slug, content, is_archived, is_pinned, is_favorite, created_at)
    VALUES 
        -- Admin notes
        (1, 1, 'System Maintenance Checklist', 'system-maintenance-checklist', 
         '# System Maintenance Checklist\n\n## Daily Tasks\n- [ ] Check server status\n- [ ] Review error logs\n- [ ] Monitor disk space\n- [ ] Backup verification\n\n## Weekly Tasks\n- [ ] Security updates\n- [ ] Performance analysis\n- [ ] User account review\n\n## Monthly Tasks\n- [ ] Full system backup\n- [ ] Security audit\n- [ ] Capacity planning', 
         0, 1, 1, GETUTCDATE()),
         
        (2, 1, 'Security Incident Response Plan', 'security-incident-response', 
         '# Security Incident Response Plan\n\n## Phase 1: Detection\n1. Monitor alerts\n2. Validate incidents\n3. Classify severity\n\n## Phase 2: Response\n1. Containment\n2. Eradication\n3. Recovery\n\n## Phase 3: Post-Incident\n1. Lessons learned\n2. Process improvement\n3. Documentation update', 
         0, 1, 0, GETUTCDATE()),

        -- John Doe notes
        (3, 2, 'Project Alpha - Requirements', 'project-alpha-requirements', 
         '# Project Alpha Requirements\n\n## Functional Requirements\n- User authentication\n- Data visualization\n- Real-time updates\n- Mobile responsive\n\n## Non-Functional Requirements\n- Performance: < 2s load time\n- Availability: 99.9% uptime\n- Security: OAuth 2.0\n- Scalability: 10k concurrent users', 
         0, 1, 1, GETUTCDATE()),(61, , '', 'daily-standup-week-42', 
         '# Daily Standup Notes - Week 42\n\n## Monday\n**Yesterday:** Completed login API\n**Today:** Working on user dashboard\n**Blockers:** None\n\n## Tuesday\n**Yesterday:** Dashboard wireframes\n**Today:** API integration\n**Blockers:** Waiting for design approval\n\n## Wednesday\n**Yesterday:** Fixed authentication bug\n**Today:** Database optimization\n**Blockers:** Need server access', 
         0, 0, 1, GETUTCDATE()),(62, , '', 'sprint-planning-meeting', 
         '# Sprint Planning Meeting\n**Date:** October 16, 2025\n**Attendees:** John, Jane, Mike, Sarah\n\n## Sprint Goal\nDeliver user authentication and basic dashboard functionality\n\n## Selected Stories\n1. User Login (8 points)\n2. Dashboard Layout (5 points)\n3. Profile Management (3 points)\n4. Data Export (2 points)\n\n## Action Items\n- [ ] John: Set up authentication service\n- [ ] Jane: Create dashboard components\n- [ ] Mike: Design database schema\n- [ ] Sarah: Create user journey flows', 
         0, 1, 0, GETUTCDATE()),

        -- Jane Smith notes(63, , '', 'frontend-architecture', 
         '# Frontend Architecture Decisions\n\n## Framework: React 18\n**Pros:**\n- Large ecosystem\n- Good performance with Fiber\n- Strong TypeScript support\n\n## State Management: Zustand\n**Pros:**\n- Lightweight\n- No boilerplate\n- TypeScript friendly\n\n## Styling: Tailwind CSS\n**Pros:**\n- Utility-first approach\n- Small bundle size\n- Easy responsive design', 
         0, 1, 1, GETUTCDATE()),(64, , '', 'code-review-guidelines', 
         '# Code Review Guidelines\n\n## Before Submitting\n- [ ] Code is self-documenting\n- [ ] Tests are written and passing\n- [ ] No debugging console logs\n- [ ] Follows style guide\n\n## Review Checklist\n- [ ] Functionality works as expected\n- [ ] Code is readable and maintainable\n- [ ] Security considerations addressed\n- [ ] Performance implications considered\n- [ ] Documentation updated if needed', 
         0, 1, 0, GETUTCDATE()),(65, , '', 'api-integration-notes', 
         '# API Integration Notes\n\n## Authentication\n```javascript\nconst token = localStorage.getItem(''authToken'');\nconst headers = {\n  ''Authorization'': `Bearer ${token}`,\n  ''Content-Type'': ''application/json''\n};\n```\n\n## Error Handling\n- 401: Redirect to login\n- 403: Show permission error\n- 500: Show generic error\n- Network: Show offline message', 
         0, 0, 1, GETUTCDATE()),

        -- Mike Wilson notes(66, , '', 'market-research-q4-2025', 
         '# Market Research Analysis Q4 2025\n\n## Key Findings\n- Market size: $2.5B (15% YoY growth)\n- Top competitors: CompanyA (25%), CompanyB (18%)\n- Customer segments: Enterprise (60%), SMB (40%)\n\n## Opportunities\n- Underserved SMB market\n- Integration partnerships\n- Mobile-first solutions\n\n## Recommendations\n1. Focus on SMB segment\n2. Develop partnership strategy\n3. Invest in mobile experience', 
         0, 1, 1, GETUTCDATE()),(67, , '', 'client-project-status', 
         '# Client Project Status Report\n**Date:** October 16, 2025\n\n## Project Alpha (TechCorp)\n- **Status:** On track\n- **Progress:** 75% complete\n- **Timeline:** Delivery by Oct 30\n- **Budget:** Within 5% of estimate\n\n## Project Beta (StartupXYZ)\n- **Status:** Delayed\n- **Progress:** 45% complete\n- **Timeline:** Delivery pushed to Nov 15\n- **Issues:** Scope creep, resource constraints', 
         0, 1, 0, GETUTCDATE()),

        -- Sarah Brown notes(68, , '', 'content-calendar-nov-2025', 
         '# Content Calendar - November 2025\n\n## Week 1\n- **Monday:** Product announcement blog post\n- **Wednesday:** Customer success story video\n- **Friday:** Industry trends infographic\n\n## Week 2\n- **Tuesday:** How-to tutorial series\n- **Thursday:** Behind-the-scenes content\n- **Saturday:** Community highlights\n\n## Week 3\n- **Monday:** Feature deep-dive article\n- **Wednesday:** Webinar announcement\n- **Friday:** User-generated content showcase', 
         0, 1, 1, GETUTCDATE()),(69, , '', 'social-media-strategy-2026', 
         '# Social Media Strategy 2026\n\n## Objectives\n- Increase brand awareness by 40%\n- Generate 500 qualified leads/month\n- Build community of 10k engaged followers\n\n## Platforms\n**LinkedIn:** B2B thought leadership\n**Twitter:** Industry news and updates\n**Instagram:** Behind-the-scenes content\n**YouTube:** Educational content and demos\n\n## Content Pillars\n1. Educational (40%)\n2. Product updates (30%)\n3. Company culture (20%)\n4. Industry insights (10%)', 
         0, 1, 1, GETUTCDATE()),

        -- Additional notes for other users
        (6, 6, 'Freelance Project Proposal Template', 'freelance-proposal-template', 
         '# Freelance Project Proposal Template\n\n## Project Overview\n[Brief description of the project]\n\n## Scope of Work\n- [ ] Task 1\n- [ ] Task 2\n- [ ] Task 3\n\n## Timeline\n**Phase 1:** Week 1-2\n**Phase 2:** Week 3-4\n**Phase 3:** Week 5-6\n\n## Investment\n**Total:** $X,XXX\n**Payment Terms:** 50% upfront, 50% on completion', 
         0, 1, 0, GETUTCDATE());
    SET IDENTITY_INSERT notes OFF;
         
    -- =============================================
    -- 8. INSERT NOTE MEMBERS (Note sharing)
    -- =============================================
    PRINT 'Inserting note members...';
    
    INSERT INTO note_members (note_id, user_id, role, invited_by, invited_at)
    VALUES 
        -- Owner memberships (auto-created by triggers, but adding explicitly)
        (1, 1, 'owner', 1, GETUTCDATE()),
        (2, 1, 'owner', 1, GETUTCDATE()),
        (3, 2, 'owner', 2, GETUTCDATE()),
        (4, 2, 'owner', 2, GETUTCDATE()),
        (5, 2, 'owner', 2, GETUTCDATE()),
        (6, 3, 'owner', 3, GETUTCDATE()),
        (7, 3, 'owner', 3, GETUTCDATE()),
        (8, 3, 'owner', 3, GETUTCDATE()),
        (9, 4, 'owner', 4, GETUTCDATE()),
        (10, 4, 'owner', 4, GETUTCDATE()),
        (11, 5, 'owner', 5, GETUTCDATE()),
        (12, 5, 'owner', 5, GETUTCDATE()),
        (13, 6, 'owner', 6, GETUTCDATE()),
        (14, 7, 'owner', 7, GETUTCDATE()),
        (15, 8, 'owner', 8, GETUTCDATE()),
        (16, 9, 'owner', 9, GETUTCDATE()),
        (17, 10, 'owner', 10, GETUTCDATE());
        
    -- =============================================
    -- 9. INSERT NOTE VERSIONS (Version history)
    -- =============================================
    PRINT 'Inserting note versions...';
    
    INSERT INTO note_versions (note_id, version_number, content, change_summary, created_by, created_at)
    VALUES 
        -- Initial versions (auto-created by triggers, but adding explicitly)
        (1, 1, '# System Maintenance Checklist\n\n## Daily Tasks\n- [ ] Check server status\n- [ ] Review error logs\n- [ ] Monitor disk space\n- [ ] Backup verification\n\n## Weekly Tasks\n- [ ] Security updates\n- [ ] Performance analysis\n- [ ] User account review\n\n## Monthly Tasks\n- [ ] Full system backup\n- [ ] Security audit\n- [ ] Capacity planning', 'Initial version', 1, GETUTCDATE()),
        (2, 1, '# Security Incident Response Plan\n\n## Phase 1: Detection\n1. Monitor alerts\n2. Validate incidents\n3. Classify severity\n\n## Phase 2: Response\n1. Containment\n2. Eradication\n3. Recovery\n\n## Phase 3: Post-Incident\n1. Lessons learned\n2. Process improvement\n3. Documentation update', 'Initial version', 1, GETUTCDATE()),
        (3, 1, '# Project Alpha Requirements\n\n## Functional Requirements\n- User authentication\n- Data visualization\n- Real-time updates\n- Mobile responsive\n\n## Non-Functional Requirements\n- Performance: < 2s load time\n- Availability: 99.9% uptime\n- Security: OAuth 2.0\n- Scalability: 10k concurrent users', 'Initial version', 2, GETUTCDATE()),
        
        -- Some updated versions to show version history
        (3, 2, '# Project Alpha Requirements\n\n## Functional Requirements\n- User authentication (OAuth 2.0)\n- Data visualization (Charts & Graphs)\n- Real-time updates (WebSocket)\n- Mobile responsive (PWA)\n- Export functionality (PDF, Excel)\n\n## Non-Functional Requirements\n- Performance: < 2s load time\n- Availability: 99.9% uptime\n- Security: OAuth 2.0 + 2FA\n- Scalability: 10k concurrent users\n- Browser support: Chrome, Firefox, Safari', 'Added export functionality and 2FA requirement', 2, GETUTCDATE()),
        
        (5, 1, '# Sprint Planning Meeting\n**Date:** October 16, 2025\n**Attendees:** John, Jane, Mike, Sarah\n\n## Sprint Goal\nDeliver user authentication and basic dashboard functionality\n\n## Selected Stories\n1. User Login (8 points)\n2. Dashboard Layout (5 points)\n3. Profile Management (3 points)\n4. Data Export (2 points)\n\n## Action Items\n- [ ] John: Set up authentication service\n- [ ] Jane: Create dashboard components\n- [ ] Mike: Design database schema\n- [ ] Sarah: Create user journey flows', 'Initial meeting notes', 2, GETUTCDATE()),
        
        (5, 2, '# Sprint Planning Meeting\n**Date:** October 16, 2025\n**Attendees:** John, Jane, Mike, Sarah\n\n## Sprint Goal\nDeliver user authentication and basic dashboard functionality\n\n## Selected Stories\n1. User Login (8 points) ✅\n2. Dashboard Layout (5 points) 🟡\n3. Profile Management (3 points)\n4. Data Export (2 points)\n\n## Action Items\n- [x] John: Set up authentication service\n- [x] Jane: Create dashboard components\n- [ ] Mike: Design database schema (In Progress)\n- [x] Sarah: Create user journey flows\n\n## Updates\n- Authentication service completed\n- Dashboard wireframes approved\n- Database schema 70% complete', 'Updated with progress status', 2, GETUTCDATE());

    -- =============================================
    -- 10. INSERT WORKSPACE ITEMS (Sample hierarchy data)
    -- =============================================
    PRINT 'Inserting workspace items...';
    
    -- Note: The workspace_items table is currently empty (0 rows) as per DATABASE_CURRENT
    -- Adding some sample data to demonstrate the UNIFIED structure
    INSERT INTO workspace_items (workspace_id, parent_tag_id, child_type, child_id, sort_order, depth, created_at)
    VALUES 
        -- John's Main Workspace (workspace_id = 3) - Hierarchy structure
        (3, NULL, 'tag', 4, 1, 0, GETUTCDATE()), -- Root: "Work" tag
        (3, 4, 'tag', 6, 1, 1, GETUTCDATE()),    -- Under Work: "Project Alpha" tag
        (3, 4, 'tag', 7, 2, 1, GETUTCDATE()),    -- Under Work: "Meeting Notes" tag
        (3, 6, 'note', 3, 1, 2, GETUTCDATE()),   -- Under Project Alpha: Requirements note
        (3, 7, 'note', 5, 1, 2, GETUTCDATE()),   -- Under Meeting Notes: Sprint planning note
        
        (3, NULL, 'tag', 5, 2, 0, GETUTCDATE()), -- Root: "Personal" tag
        (3, 5, 'tag', 8, 1, 1, GETUTCDATE()),    -- Under Personal: "Ideas" tag
        (3, 8, 'note', 4, 1, 2, GETUTCDATE()),   -- Under Ideas: Standup notes
        
        -- Jane's Development Workspace (workspace_id = 6) - Technical structure
        (6, NULL, 'tag', 9, 1, 0, GETUTCDATE()),  -- Root: "Development" tag
        (6, 9, 'tag', 11, 1, 1, GETUTCDATE()),    -- Under Development: "Frontend" tag
        (6, 9, 'tag', 12, 2, 1, GETUTCDATE()),    -- Under Development: "Backend" tag
        (6, 11, 'note', 6, 1, 2, GETUTCDATE()),   -- Under Frontend: Architecture note
        (6, 12, 'note', 8, 1, 2, GETUTCDATE()),   -- Under Backend: API integration note
        
        (6, NULL, 'tag', 10, 2, 0, GETUTCDATE()), -- Root: "Testing" tag
        (6, 10, 'note', 7, 1, 1, GETUTCDATE()),   -- Under Testing: Code review guidelines
        
        -- Sarah's Marketing Workspace (workspace_id = 10) - Campaign structure
        (10, NULL, 'tag', 18, 1, 0, GETUTCDATE()), -- Root: "Marketing" tag
        (10, 18, 'tag', 19, 1, 1, GETUTCDATE()),   -- Under Marketing: "Content" tag
        (10, 18, 'tag', 20, 2, 1, GETUTCDATE()),   -- Under Marketing: "Social Media" tag
        (10, 19, 'note', 11, 1, 2, GETUTCDATE()),  -- Under Content: Content calendar note
        (10, 20, 'note', 12, 1, 2, GETUTCDATE());  -- Under Social Media: Strategy note

    -- =============================================
    -- 11. FINAL STATISTICS AND VERIFICATION
    -- =============================================
    PRINT 'Generating data statistics...';
    
    SELECT 
        'users' as TableName, 
        COUNT(*) as RecordCount,
        MIN(created_at) as EarliestRecord,
        MAX(created_at) as LatestRecord
    FROM users
    UNION ALL
    SELECT 
        'entity_types' as TableName, 
        COUNT(*) as RecordCount,
        MIN(created_at) as EarliestRecord,
        MAX(created_at) as LatestRecord
    FROM entity_types
    UNION ALL
    SELECT 
        'tags' as TableName, 
        COUNT(*) as RecordCount,
        MIN(created_at) as EarliestRecord,
        MAX(created_at) as LatestRecord
    FROM tags
    UNION ALL
    SELECT 
        'workspaces' as TableName, 
        COUNT(*) as RecordCount,
        MIN(created_at) as EarliestRecord,
        MAX(created_at) as LatestRecord
    FROM workspaces
    UNION ALL
    SELECT 
        'workspace_members' as TableName, 
        COUNT(*) as RecordCount,
        MIN(joined_at) as EarliestRecord,
        MAX(joined_at) as LatestRecord
    FROM workspace_members
    UNION ALL
    SELECT 
        'workspace_relationship_types' as TableName, 
        COUNT(*) as RecordCount,
        MIN(created_at) as EarliestRecord,
        MAX(created_at) as LatestRecord
    FROM workspace_relationship_types
    UNION ALL
    SELECT 
        'notes' as TableName, 
        COUNT(*) as RecordCount,
        MIN(created_at) as EarliestRecord,
        MAX(created_at) as LatestRecord
    FROM notes
    UNION ALL
    SELECT 
        'note_members' as TableName, 
        COUNT(*) as RecordCount,
        MIN(invited_at) as EarliestRecord,
        MAX(invited_at) as LatestRecord
    FROM note_members
    UNION ALL
    SELECT 
        'note_versions' as TableName, 
        COUNT(*) as RecordCount,
        MIN(created_at) as EarliestRecord,
        MAX(created_at) as LatestRecord
    FROM note_versions
    UNION ALL
    SELECT 
        'workspace_items' as TableName, 
        COUNT(*) as RecordCount,
        MIN(created_at) as EarliestRecord,
        MAX(created_at) as LatestRecord
    FROM workspace_items;

    -- Summary information
    PRINT '';
    PRINT '=== COMPREHENSIVE TEST DATA INSERTION COMPLETED ===';
    PRINT 'Database populated with:';
    PRINT '- 10 Users (with proper password hashes)';
    PRINT '- 5 Entity Types (tag, note, task, project, document)';
    PRINT '- 30 Tags (across all users)';
    PRINT '- 16 Workspaces (different types: hierarchy, graph, network, timeline)';
    PRINT '- 30+ Workspace Members (collaboration setup)';
    PRINT '- 16 Workspace Relationship Types';
    PRINT '- 17 Notes (rich markdown content)';
    PRINT '- 30+ Note Members (note sharing)';
    PRINT '- 7 Note Versions (version history examples)';
    PRINT '- 18 Workspace Items (UNIFIED table examples)';
    PRINT '';
    PRINT 'All data follows DATABASE_CURRENT schema with realistic test scenarios.';
    PRINT 'Ready for API testing and development work.';

    COMMIT TRANSACTION;
    
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'Error occurred during data insertion:';
    PRINT ERROR_MESSAGE();
    THROW;
END CATCH;