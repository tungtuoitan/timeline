-- =============================================
-- INSERT SAMPLE DATA - Test Data Setup
-- Description: Insert sample data for testing
-- Author: Claude Code
-- Date: 2025-01-30
-- =============================================

USE SuperApp-dev;
GO

PRINT '========================================';
PRINT 'INSERTING SAMPLE DATA';
PRINT 'Time: ' + CONVERT(VARCHAR, GETUTCDATE(), 120);
PRINT '========================================';
GO

-- =============================================
-- PHASE 1: USERS
-- =============================================
PRINT '';
PRINT 'PHASE 1: Inserting users...';

-- User 1
IF NOT EXISTS (SELECT 1 FROM users WHERE email = 'hoanhtungle@gmail.com')
BEGIN
    INSERT INTO users (email, name, auth_provider, email_verified, is_active)
    VALUES ('hoanhtungle@gmail.com', 'Tung Le', 'google', 1, 1);
    PRINT '  ✓ User 1 created: hoanhtungle@gmail.com';
END;

-- User 2
IF NOT EXISTS (SELECT 1 FROM users WHERE email = 'testuser@example.com')
BEGIN
    INSERT INTO users (email, name, auth_provider, email_verified, is_active)
    VALUES ('testuser@example.com', 'Test User', 'email', 1, 1);
    PRINT '  ✓ User 2 created: testuser@example.com';
END;

DECLARE @user1_id INT = (SELECT user_id FROM users WHERE email = 'hoanhtungle@gmail.com');
DECLARE @user2_id INT = (SELECT user_id FROM users WHERE email = 'testuser@example.com');

-- User profiles
IF NOT EXISTS (SELECT 1 FROM user_profiles WHERE user_id = @user1_id)
BEGIN
    INSERT INTO user_profiles (user_id, bio, timezone, language)
    VALUES (@user1_id, 'Software Developer', 'Asia/Ho_Chi_Minh', 'vi');
    PRINT '  ✓ Profile created for User 1';
END;

-- =============================================
-- PHASE 2: WORKSPACES
-- =============================================
PRINT '';
PRINT 'PHASE 2: Inserting workspaces...';

-- Workspace 1 (Default)
IF NOT EXISTS (SELECT 1 FROM workspaces WHERE user_id = @user1_id AND name = 'My Workspace')
BEGIN
    INSERT INTO workspaces (user_id, name, description, color, icon, is_default)
    VALUES (@user1_id, 'My Workspace', 'Default workspace', '#3B82F6', '🏠', 1);
    PRINT '  ✓ Workspace 1 created: My Workspace';
END;

-- Workspace 2 (Project)
IF NOT EXISTS (SELECT 1 FROM workspaces WHERE user_id = @user1_id AND name = 'SuperApp Project')
BEGIN
    INSERT INTO workspaces (user_id, name, description, color, icon, type)
    VALUES (@user1_id, 'SuperApp Project', 'Development workspace', '#10B981', '💻', 'personal');
    PRINT '  ✓ Workspace 2 created: SuperApp Project';
END;

DECLARE @workspace1_id INT = (SELECT workspace_id FROM workspaces WHERE user_id = @user1_id AND name = 'My Workspace');
DECLARE @workspace2_id INT = (SELECT workspace_id FROM workspaces WHERE user_id = @user1_id AND name = 'SuperApp Project');

-- Workspace members (owner)
IF NOT EXISTS (SELECT 1 FROM workspace_members WHERE workspace_id = @workspace1_id AND user_id = @user1_id)
BEGIN
    INSERT INTO workspace_members (workspace_id, user_id, role)
    VALUES (@workspace1_id, @user1_id, 'owner');
    PRINT '  ✓ Workspace 1 member added';
END;

IF NOT EXISTS (SELECT 1 FROM workspace_members WHERE workspace_id = @workspace2_id AND user_id = @user1_id)
BEGIN
    INSERT INTO workspace_members (workspace_id, user_id, role)
    VALUES (@workspace2_id, @user1_id, 'owner');
    PRINT '  ✓ Workspace 2 member added';
END;

-- =============================================
-- PHASE 3: FOLDERS
-- =============================================
PRINT '';
PRINT 'PHASE 3: Inserting folders...';

-- Folder 1: Work
IF NOT EXISTS (SELECT 1 FROM folders WHERE user_id = @user1_id AND slug = 'work')
BEGIN
    INSERT INTO folders (user_id, name, slug, color, icon, description)
    VALUES (@user1_id, 'Work', 'work', '#EF4444', '💼', 'Work-related items');
    PRINT '  ✓ Folder created: Work';
END;

-- Folder 2: Personal
IF NOT EXISTS (SELECT 1 FROM folders WHERE user_id = @user1_id AND slug = 'personal')
BEGIN
    INSERT INTO folders (user_id, name, slug, color, icon, description)
    VALUES (@user1_id, 'Personal', 'personal', '#8B5CF6', '🏡', 'Personal items');
    PRINT '  ✓ Folder created: Personal';
END;

-- Folder 3: Archive
IF NOT EXISTS (SELECT 1 FROM folders WHERE user_id = @user1_id AND slug = 'archive')
BEGIN
    INSERT INTO folders (user_id, name, slug, color, icon, description)
    VALUES (@user1_id, 'Archive', 'archive', '#6B7280', '📦', 'Archived items');
    PRINT '  ✓ Folder created: Archive';
END;

DECLARE @folder_work_id INT = (SELECT folder_id FROM folders WHERE user_id = @user1_id AND slug = 'work');
DECLARE @folder_personal_id INT = (SELECT folder_id FROM folders WHERE user_id = @user1_id AND slug = 'personal');
DECLARE @folder_archive_id INT = (SELECT folder_id FROM folders WHERE user_id = @user1_id AND slug = 'archive');

-- =============================================
-- PHASE 4: HASHTAGS (tags_new)
-- =============================================
PRINT '';
PRINT 'PHASE 4: Inserting hashtags...';

-- Hashtag 1: important
IF NOT EXISTS (SELECT 1 FROM tags_new WHERE user_id = @user1_id AND slug = 'important')
BEGIN
    INSERT INTO tags_new (user_id, name, slug, color)
    VALUES (@user1_id, 'Important', 'important', '#EF4444');
    PRINT '  ✓ Hashtag created: #important';
END;

-- Hashtag 2: urgent
IF NOT EXISTS (SELECT 1 FROM tags_new WHERE user_id = @user1_id AND slug = 'urgent')
BEGIN
    INSERT INTO tags_new (user_id, name, slug, color)
    VALUES (@user1_id, 'Urgent', 'urgent', '#F59E0B');
    PRINT '  ✓ Hashtag created: #urgent';
END;

-- Hashtag 3: todo
IF NOT EXISTS (SELECT 1 FROM tags_new WHERE user_id = @user1_id AND slug = 'todo')
BEGIN
    INSERT INTO tags_new (user_id, name, slug, color)
    VALUES (@user1_id, 'TODO', 'todo', '#3B82F6');
    PRINT '  ✓ Hashtag created: #todo';
END;

-- Hashtag 4: done
IF NOT EXISTS (SELECT 1 FROM tags_new WHERE user_id = @user1_id AND slug = 'done')
BEGIN
    INSERT INTO tags_new (user_id, name, slug, color)
    VALUES (@user1_id, 'Done', 'done', '#10B981');
    PRINT '  ✓ Hashtag created: #done';
END;

DECLARE @tag_important_id INT = (SELECT tag_id FROM tags_new WHERE user_id = @user1_id AND slug = 'important');
DECLARE @tag_urgent_id INT = (SELECT tag_id FROM tags_new WHERE user_id = @user1_id AND slug = 'urgent');
DECLARE @tag_todo_id INT = (SELECT tag_id FROM tags_new WHERE user_id = @user1_id AND slug = 'todo');
DECLARE @tag_done_id INT = (SELECT tag_id FROM tags_new WHERE user_id = @user1_id AND slug = 'done');

-- =============================================
-- PHASE 5: NOTES
-- =============================================
PRINT '';
PRINT 'PHASE 5: Inserting notes...';

-- Note 1: Meeting notes
IF NOT EXISTS (SELECT 1 FROM notes WHERE user_id = @user1_id AND name = 'Meeting Notes')
BEGIN
    INSERT INTO notes (user_id, name, description, content, type)
    VALUES (@user1_id, 'Meeting Notes', 'Weekly team meeting',
            '# Meeting Notes\n\n- Discussed project timeline\n- Reviewed sprint goals\n- Assigned tasks',
            'markdown');
    PRINT '  ✓ Note created: Meeting Notes';
END;

-- Note 2: Project ideas
IF NOT EXISTS (SELECT 1 FROM notes WHERE user_id = @user1_id AND name = 'Project Ideas')
BEGIN
    INSERT INTO notes (user_id, name, description, content, type, is_pinned)
    VALUES (@user1_id, 'Project Ideas', 'Brainstorming new features',
            '# Project Ideas\n\n1. Add dark mode\n2. Implement tags\n3. Add sharing feature',
            'markdown', 1);
    PRINT '  ✓ Note created: Project Ideas';
END;

-- Note 3: Shopping list
IF NOT EXISTS (SELECT 1 FROM notes WHERE user_id = @user1_id AND name = 'Shopping List')
BEGIN
    INSERT INTO notes (user_id, name, description, content, type)
    VALUES (@user1_id, 'Shopping List', 'Grocery shopping',
            '- Milk\n- Bread\n- Eggs\n- Coffee',
            'markdown');
    PRINT '  ✓ Note created: Shopping List';
END;

DECLARE @note1_id INT = (SELECT note_id FROM notes WHERE user_id = @user1_id AND name = 'Meeting Notes');
DECLARE @note2_id INT = (SELECT note_id FROM notes WHERE user_id = @user1_id AND name = 'Project Ideas');
DECLARE @note3_id INT = (SELECT note_id FROM notes WHERE user_id = @user1_id AND name = 'Shopping List');

-- =============================================
-- PHASE 6: WORKSPACE ITEMS (folders + notes in workspace)
-- =============================================
PRINT '';
PRINT 'PHASE 6: Linking items to workspaces...';

-- Add folders to Workspace 1
IF NOT EXISTS (SELECT 1 FROM workspace_items WHERE workspace_id = @workspace1_id AND child_type = 'folder' AND child_id = @folder_work_id)
BEGIN
    INSERT INTO workspace_items (workspace_id, child_type, child_id, sort_order)
    VALUES (@workspace1_id, 'folder', @folder_work_id, 1);
    PRINT '  ✓ Added folder "Work" to Workspace 1';
END;

IF NOT EXISTS (SELECT 1 FROM workspace_items WHERE workspace_id = @workspace1_id AND child_type = 'folder' AND child_id = @folder_personal_id)
BEGIN
    INSERT INTO workspace_items (workspace_id, child_type, child_id, sort_order)
    VALUES (@workspace1_id, 'folder', @folder_personal_id, 2);
    PRINT '  ✓ Added folder "Personal" to Workspace 1';
END;

-- Add notes to Workspace 1 (under Work folder)
IF NOT EXISTS (SELECT 1 FROM workspace_items WHERE workspace_id = @workspace1_id AND child_type = 'note' AND child_id = @note1_id)
BEGIN
    INSERT INTO workspace_items (workspace_id, parent_tag_id, child_type, child_id, sort_order)
    VALUES (@workspace1_id, @folder_work_id, 'note', @note1_id, 1);
    PRINT '  ✓ Added note "Meeting Notes" to Work folder';
END;

IF NOT EXISTS (SELECT 1 FROM workspace_items WHERE workspace_id = @workspace1_id AND child_type = 'note' AND child_id = @note2_id)
BEGIN
    INSERT INTO workspace_items (workspace_id, parent_tag_id, child_type, child_id, sort_order)
    VALUES (@workspace1_id, @folder_work_id, 'note', @note2_id, 2);
    PRINT '  ✓ Added note "Project Ideas" to Work folder';
END;

-- Add note to Personal folder
IF NOT EXISTS (SELECT 1 FROM workspace_items WHERE workspace_id = @workspace1_id AND child_type = 'note' AND child_id = @note3_id)
BEGIN
    INSERT INTO workspace_items (workspace_id, parent_tag_id, child_type, child_id, sort_order)
    VALUES (@workspace1_id, @folder_personal_id, 'note', @note3_id, 1);
    PRINT '  ✓ Added note "Shopping List" to Personal folder';
END;

-- =============================================
-- PHASE 7: ENTITY TAGS (tag notes with hashtags)
-- =============================================
PRINT '';
PRINT 'PHASE 7: Tagging notes with hashtags...';

-- Tag note 1 with #important and #todo
IF NOT EXISTS (SELECT 1 FROM entity_tags WHERE tag_id = @tag_important_id AND entity_type = 'note' AND entity_id = @note1_id)
BEGIN
    INSERT INTO entity_tags (tag_id, entity_type, entity_id, tagged_by)
    VALUES (@tag_important_id, 'note', @note1_id, @user1_id);
    PRINT '  ✓ Tagged "Meeting Notes" with #important';
END;

IF NOT EXISTS (SELECT 1 FROM entity_tags WHERE tag_id = @tag_todo_id AND entity_type = 'note' AND entity_id = @note1_id)
BEGIN
    INSERT INTO entity_tags (tag_id, entity_type, entity_id, tagged_by)
    VALUES (@tag_todo_id, 'note', @note1_id, @user1_id);
    PRINT '  ✓ Tagged "Meeting Notes" with #todo';
END;

-- Tag note 2 with #important and #urgent
IF NOT EXISTS (SELECT 1 FROM entity_tags WHERE tag_id = @tag_important_id AND entity_type = 'note' AND entity_id = @note2_id)
BEGIN
    INSERT INTO entity_tags (tag_id, entity_type, entity_id, tagged_by)
    VALUES (@tag_important_id, 'note', @note2_id, @user1_id);
    PRINT '  ✓ Tagged "Project Ideas" with #important';
END;

IF NOT EXISTS (SELECT 1 FROM entity_tags WHERE tag_id = @tag_urgent_id AND entity_type = 'note' AND entity_id = @note2_id)
BEGIN
    INSERT INTO entity_tags (tag_id, entity_type, entity_id, tagged_by)
    VALUES (@tag_urgent_id, 'note', @note2_id, @user1_id);
    PRINT '  ✓ Tagged "Project Ideas" with #urgent';
END;

-- Tag note 3 with #todo
IF NOT EXISTS (SELECT 1 FROM entity_tags WHERE tag_id = @tag_todo_id AND entity_type = 'note' AND entity_id = @note3_id)
BEGIN
    INSERT INTO entity_tags (tag_id, entity_type, entity_id, tagged_by)
    VALUES (@tag_todo_id, 'note', @note3_id, @user1_id);
    PRINT '  ✓ Tagged "Shopping List" with #todo';
END;

-- =============================================
-- SUMMARY
-- =============================================
PRINT '';
PRINT '========================================';
PRINT 'SAMPLE DATA INSERTED SUCCESSFULLY';
PRINT '========================================';
PRINT '';
PRINT 'Data inserted:';
PRINT '  Users: 2';
PRINT '  Workspaces: 2';
PRINT '  Folders: 3 (Work, Personal, Archive)';
PRINT '  Hashtags: 4 (#important, #urgent, #todo, #done)';
PRINT '  Notes: 3';
PRINT '  Workspace Items: 5 (folders + notes)';
PRINT '  Entity Tags: 5 (note-hashtag links)';
PRINT '';
PRINT 'You can now:';
PRINT '  1. Login as: hoanhtungle@gmail.com';
PRINT '  2. View "My Workspace" with Work and Personal folders';
PRINT '  3. See notes tagged with hashtags';
PRINT '';
PRINT 'Sample data insertion completed at: ' + CONVERT(VARCHAR, GETUTCDATE(), 120);
GO
