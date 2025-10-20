-- =============================================
-- Insert test data according to DATABASE_CURRENT schema
-- =============================================

-- Insert test users
INSERT INTO users (email, username, password_hash, display_name, is_active, email_verified)
VALUES 
    ('user1@test.com', 'user1', 'hashed_password_1', 'Test User 1', 1, 1),
    ('user2@test.com', 'user2', 'hashed_password_2', 'Test User 2', 1, 1),
    ('user3@test.com', 'user3', 'hashed_password_3', 'Test User 3', 1, 1);

-- Insert entity types
INSERT INTO entity_types (type_name, display_name, display_plural, icon, color, description, table_name, supports_versioning, supports_sharing, is_enabled)
VALUES 
    ('tag', 'Tag', 'Tags', 'tag', '#3B82F6', 'Organizational tags', 'tags', 0, 1, 1),
    ('note', 'Note', 'Notes', 'note', '#10B981', 'Text notes with markdown', 'notes', 1, 1, 1),
    ('task', 'Task', 'Tasks', 'task', '#F59E0B', 'Action items and todos', 'tasks', 0, 1, 0);

-- Insert test tags
INSERT INTO tags (user_id, name, slug, color, icon, description, usage_count)
VALUES 
    (1, 'Work', 'work', '#3B82F6', 'briefcase', 'Work-related items', 5),
    (1, 'Personal', 'personal', '#10B981', 'home', 'Personal items', 3),
    (1, 'Important', 'important', '#EF4444', 'star', 'High priority items', 8),
    (2, 'Project Alpha', 'project-alpha', '#8B5CF6', 'folder', 'Alpha project items', 2),
    (2, 'Meeting Notes', 'meeting-notes', '#F59E0B', 'users', 'Meeting-related notes', 4);

-- Insert test workspaces
INSERT INTO workspaces (user_id, name, slug, description, workspace_type, settings)
VALUES 
    (1, 'Main Workspace', 'main-workspace', 'Primary workspace for user 1', 'hierarchy', '{}'),
    (1, 'Side Projects', 'side-projects', 'Side project workspace', 'graph', '{}'),
    (2, 'Team Workspace', 'team-workspace', 'Collaborative workspace', 'hierarchy', '{}'),
    (3, 'Research Hub', 'research-hub', 'Research and documentation', 'timeline', '{}');

-- Insert workspace members (sharing)
INSERT INTO workspace_members (workspace_id, user_id, role, permissions, joined_at)
VALUES 
    (1, 1, 'owner', '{"read": true, "write": true, "delete": true, "admin": true}', GETUTCDATE()),
    (2, 1, 'owner', '{"read": true, "write": true, "delete": true, "admin": true}', GETUTCDATE()),
    (3, 2, 'owner', '{"read": true, "write": true, "delete": true, "admin": true}', GETUTCDATE()),
    (3, 1, 'editor', '{"read": true, "write": true, "delete": false, "admin": false}', GETUTCDATE()),
    (4, 3, 'owner', '{"read": true, "write": true, "delete": true, "admin": true}', GETUTCDATE());

-- Insert workspace relationship types
INSERT INTO workspace_relationship_types (workspace_id, relationship_name, description, is_bidirectional, is_enabled)
VALUES 
    (1, 'contains', 'Parent-child containment', 0, 1),
    (1, 'related_to', 'General relationship', 1, 1),
    (2, 'depends_on', 'Dependency relationship', 0, 1),
    (3, 'collaborates_with', 'Collaboration relationship', 1, 1),
    (4, 'precedes', 'Temporal sequence', 0, 1);

-- Insert test notes
INSERT INTO notes (user_id, name, slug, content, content_type, is_archived, is_pinned, is_favorite)
VALUES 
    (1, 'Meeting Notes - Q4 Planning', 'meeting-notes-q4-planning', '# Q4 Planning Meeting\n\n## Agenda\n- Review Q3 results\n- Plan Q4 objectives\n- Discuss budget', 'markdown', 0, 1, 1),
    (1, 'Project Ideas', 'project-ideas', '# Project Ideas\n\n- Mobile app for productivity\n- Web dashboard for analytics\n- API integration tool', 'markdown', 0, 0, 0),
    (1, 'Daily Standup Notes', 'daily-standup-notes', '# Daily Standup\n\n## Yesterday\n- Fixed login bug\n- Updated documentation\n\n## Today\n- Work on user dashboard', 'markdown', 0, 0, 1),
    (2, 'Alpha Project Spec', 'alpha-project-spec', '# Alpha Project Specification\n\n## Overview\nThis project aims to create a new user experience...', 'markdown', 0, 1, 0),
    (2, 'Team Meeting Minutes', 'team-meeting-minutes', '# Team Meeting - Oct 16, 2025\n\n## Attendees\n- John Doe\n- Jane Smith\n\n## Discussion Points\n- Sprint review', 'markdown', 0, 0, 1);

-- Insert note members (sharing)
INSERT INTO note_members (note_id, user_id, role, permissions, added_at, added_by)
VALUES 
    (1, 1, 'owner', '{"read": true, "write": true, "delete": true}', GETUTCDATE(), 1),
    (2, 1, 'owner', '{"read": true, "write": true, "delete": true}', GETUTCDATE(), 1),
    (3, 1, 'owner', '{"read": true, "write": true, "delete": true}', GETUTCDATE(), 1),
    (4, 2, 'owner', '{"read": true, "write": true, "delete": true}', GETUTCDATE(), 2),
    (4, 1, 'viewer', '{"read": true, "write": false, "delete": false}', GETUTCDATE(), 2),
    (5, 2, 'owner', '{"read": true, "write": true, "delete": true}', GETUTCDATE(), 2);

-- Insert note versions (auto-created by triggers in actual system)
INSERT INTO note_versions (note_id, version_number, content, content_type, change_summary, created_by)
VALUES 
    (1, 1, '# Q4 Planning Meeting\n\n## Agenda\n- Review Q3 results\n- Plan Q4 objectives\n- Discuss budget', 'markdown', 'Initial version', 1),
    (2, 1, '# Project Ideas\n\n- Mobile app for productivity\n- Web dashboard for analytics\n- API integration tool', 'markdown', 'Initial version', 1),
    (3, 1, '# Daily Standup\n\n## Yesterday\n- Fixed login bug\n- Updated documentation\n\n## Today\n- Work on user dashboard', 'markdown', 'Initial version', 1),
    (4, 1, '# Alpha Project Specification\n\n## Overview\nThis project aims to create a new user experience...', 'markdown', 'Initial version', 2),
    (5, 1, '# Team Meeting - Oct 16, 2025\n\n## Attendees\n- John Doe\n- Jane Smith\n\n## Discussion Points\n- Sprint review', 'markdown', 'Initial version', 2);

-- Note: workspace_items table is empty (0 rows) as mentioned in DATABASE_CURRENT
-- It's ready to use but no test data inserted yet

SELECT 'Database populated successfully with test data according to DATABASE_CURRENT schema' AS Result;