-- =============================================
-- Table: files
-- Description: Stores file/document metadata
-- Created: October 21, 2025
-- Version: MVP 1.1
-- =============================================

-- Files are leaf nodes in workspace tree (cannot have children)
-- They can be organized under tags via workspace_items table

CREATE TABLE [dbo].[files] (
    -- Primary key
    [file_id] INT IDENTITY(1,1) NOT NULL,
    
    -- Ownership
    [user_id] INT NOT NULL,
    
    -- File metadata
    [name] NVARCHAR(500) NOT NULL,
    [original_filename] NVARCHAR(500) NOT NULL,
    [file_path] NVARCHAR(2000) NOT NULL,
    [file_size] BIGINT NOT NULL,
    [mime_type] NVARCHAR(255) NOT NULL,
    [extension] NVARCHAR(50),
    
    -- Additional info
    [description] NVARCHAR(MAX),
    [slug] NVARCHAR(255),
    
    -- File properties
    [is_public] BIT NOT NULL DEFAULT 0,
    [is_archived] BIT NOT NULL DEFAULT 0,
    [download_count] INT NOT NULL DEFAULT 0,
    
    -- Timestamps
    [created_at] DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    [updated_at] DATETIME2(7),
    [deleted_at] DATETIME2(7),
    
    -- Constraints
    CONSTRAINT [PK_files] PRIMARY KEY CLUSTERED ([file_id] ASC),
    CONSTRAINT [FK_files_user] FOREIGN KEY ([user_id]) 
        REFERENCES [dbo].[users]([user_id]) ON DELETE CASCADE,
    CONSTRAINT [CK_files_filesize] CHECK ([file_size] >= 0),
    CONSTRAINT [CK_files_downloadcount] CHECK ([download_count] >= 0)
);

-- =============================================
-- Indexes
-- =============================================

-- Index for finding files by user
CREATE NONCLUSTERED INDEX [IX_files_user] 
    ON [dbo].[files]([user_id]) 
    WHERE [deleted_at] IS NULL;

-- Index for finding files by slug
CREATE NONCLUSTERED INDEX [IX_files_slug] 
    ON [dbo].[files]([slug]) 
    WHERE [deleted_at] IS NULL;

-- Index for sorting by creation date
CREATE NONCLUSTERED INDEX [IX_files_created] 
    ON [dbo].[files]([created_at] DESC) 
    WHERE [deleted_at] IS NULL;

-- Index for filtering by MIME type
CREATE NONCLUSTERED INDEX [IX_files_mimetype] 
    ON [dbo].[files]([mime_type]) 
    WHERE [deleted_at] IS NULL;

-- =============================================
-- Triggers
-- =============================================

-- Trigger to auto-generate slug from filename
CREATE TRIGGER [dbo].[tr_files_generate_slug]
ON [dbo].[files]
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Prevent recursion
    IF TRIGGER_NESTLEVEL() > 1
        RETURN;
    
    -- Generate slug from filename if not provided
    UPDATE f
    SET f.slug = LOWER(
        REPLACE(
            REPLACE(
                REPLACE(
                    REPLACE(
                        REPLACE(i.name, ' ', '-'),
                        '.', '-'
                    ),
                    '--', '-'
                ),
                '---', '-'
            ),
            '----', '-'
        )
    ),
    f.updated_at = GETUTCDATE()
    FROM [dbo].[files] f
    INNER JOIN inserted i ON f.file_id = i.file_id
    WHERE f.slug IS NULL OR f.slug = '' OR LEN(f.slug) = 0;
END;

-- =============================================
-- Sample Data
-- =============================================

INSERT INTO [dbo].[files] 
    ([user_id], [name], [original_filename], [file_path], [file_size], [mime_type], [extension], [description])
VALUES
    (1, 'Project Proposal', 'project-proposal.pdf', '/uploads/2025/10/project-proposal.pdf', 2458624, 'application/pdf', 'pdf', 'Q1 2026 project proposal document'),
    (1, 'Meeting Notes', 'meeting-notes-2025-10-15.docx', '/uploads/2025/10/meeting-notes.docx', 45632, 'application/vnd.openxmlformats-officedocument.wordprocessingml.document', 'docx', 'Weekly team meeting notes'),
    (1, 'Architecture Diagram', 'system-architecture.png', '/uploads/2025/10/architecture.png', 1256789, 'image/png', 'png', 'System architecture overview'),
    (1, 'Budget Spreadsheet', 'budget-2025.xlsx', '/uploads/2025/10/budget.xlsx', 87456, 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet', 'xlsx', '2025 annual budget breakdown'),
    (1, 'Presentation', 'product-demo.pptx', '/uploads/2025/10/demo.pptx', 3456789, 'application/vnd.openxmlformats-officedocument.presentationml.presentation', 'pptx', 'Product demo presentation for investors');

-- =============================================
-- Column Descriptions
-- =============================================

/*
file_id: Primary key, auto-increment
user_id: Foreign key to users table (owner of the file)
name: Display name of the file (user-friendly)
original_filename: Original filename when uploaded
file_path: Storage path on server/cloud
file_size: Size in bytes
mime_type: MIME type (e.g., application/pdf, image/png)
extension: File extension without dot (e.g., pdf, docx, jpg)
description: Optional description of the file content
slug: URL-friendly identifier (auto-generated from name)
is_public: Whether file can be accessed publicly
is_archived: Whether file is archived (soft archive)
download_count: Number of times file has been downloaded
created_at: When file was uploaded
updated_at: When file metadata was last updated
deleted_at: Soft delete timestamp (NULL = not deleted)
*/

-- =============================================
-- Business Rules
-- =============================================

/*
1. Files are LEAF NODES in workspace tree (cannot have children)
2. Files can be organized under tags via workspace_items table
3. child_type = 'file' in workspace_items
4. Files are owned by users (user_id)
5. Files support soft delete (deleted_at)
6. Slugs are auto-generated from name
7. File size and download count must be non-negative
8. Files can be public or private (is_public flag)
9. CASCADE delete when user is deleted
*/

-- =============================================
-- Usage in workspace_items
-- =============================================

/*
Example: Organizing files under tags in workspace

INSERT INTO workspace_items 
    (workspace_id, parent_tag_id, child_type, child_id)
VALUES
    (1, 5, 'file', 1),  -- Project Proposal under tag 5
    (1, 5, 'file', 2),  -- Meeting Notes under tag 5
    (1, 7, 'file', 3);  -- Architecture Diagram under tag 7

Note: child_type must be 'file' when linking files to workspace tree
*/

-- =============================================
-- Supported MIME Types
-- =============================================

/*
Common MIME types:
- application/pdf - PDF documents
- application/vnd.openxmlformats-officedocument.wordprocessingml.document - DOCX
- application/vnd.openxmlformats-officedocument.spreadsheetml.sheet - XLSX
- application/vnd.openxmlformats-officedocument.presentationml.presentation - PPTX
- image/png - PNG images
- image/jpeg - JPEG images
- image/gif - GIF images
- text/plain - Text files
- application/zip - ZIP archives
*/
