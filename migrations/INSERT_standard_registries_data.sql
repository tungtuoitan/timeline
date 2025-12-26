-- =============================================
-- Script: Insert initial data into standard_registries
-- Description: Insert entity types, workspace statuses, and note statuses
-- Based on: C:\Users\Admin\source\SuperApp\data.md
-- =============================================

USE [SuperApp-dev]; -- Replace with your database name
GO

-- Clear existing data (optional - comment out if you want to keep existing data)
-- DELETE FROM dbo.standard_registries;
-- GO

-- Insert Entity Types
INSERT INTO dbo.standard_registries (code, description, type, is_active, json_detail, created_date)
VALUES
    ('workspace', 'Workspace/Project container', 'entity', 1, NULL, GETUTCDATE()),
    ('folder', 'Folder for organizing items', 'entity', 1, NULL, GETUTCDATE()),
    ('note', 'Note/Document', 'entity', 1, NULL, GETUTCDATE()),
    ('file', 'File attachment', 'entity', 1, NULL, GETUTCDATE());

-- Insert Workspace Status Types
INSERT INTO dbo.standard_registries (code, description, type, is_active, json_detail, created_date)
VALUES
    ('active', 'Active', 'workspaceStatus', 1, NULL, GETUTCDATE()),
    ('inactive', 'Inactive', 'workspaceStatus', 1, NULL, GETUTCDATE());

-- Insert Note Status Types
INSERT INTO dbo.standard_registries (code, description, type, is_active, json_detail, created_date)
VALUES
    ('active', 'Active', 'noteStatus', 1, NULL, GETUTCDATE()),
    ('inactive', 'Inactive', 'noteStatus', 1, NULL, GETUTCDATE());

-- Insert HashTag Options (migrating from frontend hardcoded data)
INSERT INTO dbo.standard_registries (code, description, type, is_active, json_detail, created_date)
VALUES
    ('work', 'Work', 'hashtag', 1, NULL, GETUTCDATE()),
    ('personal', 'Personal', 'hashtag', 1, NULL, GETUTCDATE()),
    ('important', 'Important', 'hashtag', 1, NULL, GETUTCDATE()),
    ('urgent', 'Urgent', 'hashtag', 1, NULL, GETUTCDATE());

GO


PRINT 'Standard registries data inserted successfully!';
GO


select * from dbo.standard_registries
