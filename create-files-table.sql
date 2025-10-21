-- =============================================
-- Create Files Table
-- Date: October 21, 2025
-- Description: Table to store file/document metadata
-- Files are leaf nodes in workspace tree (cannot have children)
-- =============================================

-- Drop existing objects if they exist (for development)
IF OBJECT_ID('dbo.tr_files_generate_slug', 'TR') IS NOT NULL
    DROP TRIGGER dbo.tr_files_generate_slug;

IF OBJECT_ID('dbo.files', 'U') IS NOT NULL
    DROP TABLE dbo.files;

-- Create files table
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
        REFERENCES [dbo].[users]([user_id]) ON DELETE CASCADE
);
GO

-- Create indexes
CREATE NONCLUSTERED INDEX [IX_files_user] 
    ON [dbo].[files]([user_id]) 
    WHERE [deleted_at] IS NULL;
GO

CREATE NONCLUSTERED INDEX [IX_files_slug] 
    ON [dbo].[files]([slug]) 
    WHERE [deleted_at] IS NULL;
GO

CREATE NONCLUSTERED INDEX [IX_files_created] 
    ON [dbo].[files]([created_at] DESC) 
    WHERE [deleted_at] IS NULL;
GO

CREATE NONCLUSTERED INDEX [IX_files_mimetype] 
    ON [dbo].[files]([mime_type]) 
    WHERE [deleted_at] IS NULL;
GO

-- Create trigger for automatic slug generation
CREATE TRIGGER [dbo].[tr_files_generate_slug]
ON [dbo].[files]
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Check trigger nesting level to prevent recursion
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
GO

-- Add check constraints
ALTER TABLE [dbo].[files]
    ADD CONSTRAINT [CK_files_filesize] CHECK ([file_size] >= 0);
GO

ALTER TABLE [dbo].[files]
    ADD CONSTRAINT [CK_files_downloadcount] CHECK ([download_count] >= 0);
GO

-- Insert sample data for testing
INSERT INTO [dbo].[files] 
    ([user_id], [name], [original_filename], [file_path], [file_size], [mime_type], [extension], [description])
VALUES
    (1, 'Project Proposal', 'project-proposal.pdf', '/uploads/2025/10/project-proposal.pdf', 2458624, 'application/pdf', 'pdf', 'Q1 2026 project proposal document'),
    (1, 'Meeting Notes', 'meeting-notes-2025-10-15.docx', '/uploads/2025/10/meeting-notes.docx', 45632, 'application/vnd.openxmlformats-officedocument.wordprocessingml.document', 'docx', 'Weekly team meeting notes'),
    (1, 'Architecture Diagram', 'system-architecture.png', '/uploads/2025/10/architecture.png', 1256789, 'image/png', 'png', 'System architecture overview'),
    (1, 'Budget Spreadsheet', 'budget-2025.xlsx', '/uploads/2025/10/budget.xlsx', 87456, 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet', 'xlsx', '2025 annual budget breakdown'),
    (1, 'Presentation', 'product-demo.pptx', '/uploads/2025/10/demo.pptx', 3456789, 'application/vnd.openxmlformats-officedocument.presentationml.presentation', 'pptx', 'Product demo presentation for investors');
GO

-- Verification queries
PRINT '=== Files Table Created Successfully ===';
PRINT '';

SELECT 
    COUNT(*) AS [TotalFiles],
    SUM(file_size) AS [TotalSizeBytes],
    SUM(file_size) / 1024.0 / 1024.0 AS [TotalSizeMB]
FROM [dbo].[files]
WHERE deleted_at IS NULL;

PRINT '';
PRINT '=== Sample Files ===';
SELECT 
    file_id,
    name,
    extension,
    CAST(file_size / 1024.0 / 1024.0 AS DECIMAL(10, 2)) AS [SizeMB],
    mime_type,
    slug,
    created_at
FROM [dbo].[files]
WHERE deleted_at IS NULL
ORDER BY created_at DESC;

PRINT '';
PRINT '=== Indexes Created ===';
SELECT 
    i.name AS IndexName,
    c.name AS ColumnName,
    i.type_desc AS IndexType
FROM sys.indexes i
INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
WHERE i.object_id = OBJECT_ID('dbo.files')
ORDER BY i.name, ic.index_column_id;

PRINT '';
PRINT '=== Script Completed ===';
